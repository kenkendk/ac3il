using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccCIL;

namespace SPEJIT
{
    /// <summary>
    /// Class that converts IR to SPE opcodes
    /// </summary>
    public class SPEJITCompiler : IJITCompiler
    {
        /// <summary>
        /// The fixed register number used for special register LINK RETURN
        /// </summary>
        public const uint _LR = 0;
        /// <summary>
        /// The fixed register used for special register STACK POINTER
        /// </summary>
        public const uint _SP = 1;

        /// <summary>
        /// The size of the offset of the LR register in the stack
        /// </summary>
        private const int LR_OFFSET = 16;

        /// <summary>
        /// The size of a register in bytes when placed on stack
        /// </summary>
        public const int REGISTER_SIZE = 16;

        /// <summary>
        /// The size of a single instruction in bytes
        /// </summary>
        public const int INSTRUCTION_SIZE = 4;

        /// <summary>
        /// The first register used for local variables
        /// </summary>
        public const int _LV0 = 80;

        /// <summary>
        /// The size of the object table
        /// </summary>
        public const int OBJECT_TABLE_SIZE = 16;

        /// <summary>
        /// The offset of the type table
        /// </summary>
        public const int TYPE_TABLE_OFFSET = 32 + (OBJECT_TABLE_SIZE * 16);

        /// <summary>
        /// The offset for the first method
        /// </summary>
        public const int FIRST_METHOD_OFFSET = TYPE_TABLE_OFFSET + (OBJECT_TABLE_SIZE * 16);

        //ABI specification states that $75 to $79 are scratch registers
        public const uint _TMP0 = 75;
        public const uint _TMP1 = 76;
        public const uint _TMP2 = 77;
        public const uint _TMP3 = 78;
        public const uint _TMP4 = 79;

        /// <summary>
        /// The register used for the first argument
        /// </summary>
        public const uint _ARG0 = 3;

        /// <summary>
        /// The max number of local variable registers
        /// </summary>
        public const int MAX_LV_REGISTERS = 127 - 80;

        /// <summary>
        /// The list of all mappped CIL to SPE translations
        /// </summary>
        private static readonly Dictionary<Mono.Cecil.Cil.Code, System.Reflection.MethodInfo> _opTranslations;

        /// <summary>
        /// Static initializer for building instruction table based on reflection
        /// </summary>
        static SPEJITCompiler()
        {
            _opTranslations = BuildTranslationTable();
        }

        /// <summary>
        /// Produces an ELF compatible binary output stream with the compiled methods
        /// </summary>
        /// <param name="outstream">The output stream</param>
        /// <param name="assemblyOutput">The assembly text output, can be null</param>
        /// <param name="methods">The compiled methods</param>
        public void EmitELFStream(System.IO.Stream outstream, System.IO.TextWriter assemblyOutput, IEnumerable<ICompiledMethod> methods)
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                uint bootloader_offset;
                EmitInstructionStream(ms, assemblyOutput, methods, out bootloader_offset);

                SPEEmulator.ELFReader.EmitELFHeader((uint)ms.Length, bootloader_offset, outstream);
                
                ms.Position = 0;
                ms.CopyTo(outstream);
            }
        }


        /// <summary>
        /// Emits an instruction stream.
        /// </summary>
        /// <param name="outstream">The output stream</param>
        /// <param name="assemblyOutput">The assembly text output, can be null</param>
        /// <param name="methods">The methods to compile</param>
        public void EmitInstructionStream(System.IO.Stream outstream, System.IO.TextWriter assemblyOutput, IEnumerable<AccCIL.IR.MethodEntry> methods, out uint bootloader_offset)
        {
            List<ICompiledMethod> cmps = new List<ICompiledMethod>();
            foreach (AccCIL.IR.MethodEntry me in methods)
                cmps.Add(JIT(me));

            EmitInstructionStream(outstream, assemblyOutput, cmps, out bootloader_offset);
        }


        private static void InstructionsToBytes(IEnumerable<SPEEmulator.OpCodes.Bases.Instruction> ops, System.IO.Stream stream, System.IO.TextWriter assemblyOutput, Dictionary<long, List<Mono.Cecil.Cil.Instruction>> instructionOffsets)
        {
            foreach (SPEEmulator.OpCodes.Bases.Instruction i in ops) {
                stream.Write(ReverseEndian(BitConverter.GetBytes(i.Value)), 0, 4);

                if (assemblyOutput != null)
                {
                    long offset = stream.Position - 4;
                    if (instructionOffsets != null)
                    {
                        List<Mono.Cecil.Cil.Instruction> ins;
                        instructionOffsets.TryGetValue((int)offset / 4, out ins);
                        if (ins != null)
                            foreach(Mono.Cecil.Cil.Instruction c in ins)
                                assemblyOutput.WriteLine("# " + c.OpCode.Code);
                    }
                    assemblyOutput.WriteLine(string.Format("0x{0:x4}: {1}", offset, i.ToString()));
                }
            }
        }

        /// <summary>
        /// Emits an instruction stream.
        /// </summary>
        /// <param name="outstream">The output stream</param>
        /// <param name="assemblyOutput">The assembly text output, can be null</param>
        /// <param name="methods">The compiled methods</param>
        public void EmitInstructionStream(System.IO.Stream outstream, System.IO.TextWriter assemblyOutput, IEnumerable<ICompiledMethod> _methods, out uint bootloader_offset)
        {
            //The size allocated for bootloader and call handler
            int BOOTLOADER_LENGTH = (BOOT_LOADER.Length + CALL_HANDLER.Length) * 4;

            //Typecast list
            List<CompiledMethod> methods = _methods.Select(x => (CompiledMethod)x).ToList();

            //Get a list of builtins
            Dictionary<string, Mono.Cecil.MethodDefinition> builtins = new Dictionary<string, Mono.Cecil.MethodDefinition>(CompiledMethod.m_builtins);

            //Get list of already compiled builtins
            IDictionary<string, CompiledMethod> includedBuiltins = methods.Where(x => x.Method.Method.DeclaringType.FullName == "SPEJIT.BuiltInMethods").ToDictionary(x => x.Method.Method.Name);

            //Add any built-in methods
            for(int i = 0; i < methods.Count; i++)
                foreach (Mono.Cecil.MethodReference mr in methods[i].CalledMethods)
                    if (mr.DeclaringType.FullName == "SPEJIT.BuiltInMethods" && builtins.ContainsKey(mr.Name) && !includedBuiltins.ContainsKey(mr.Name))
                    {
                        methods.Add((CompiledMethod)AccCIL.AccCIL.JIT(this, builtins[mr.Name]));
                        builtins.Remove(mr.Name);
                    }

            //Start building the binary header
            int items_in_table = 1 + methods.Count;

            //Write argument load area
            outstream.Write(ReverseEndian(BitConverter.GetBytes((int)0)), 0, 4); //Reserved entry
            outstream.Write(ReverseEndian(BitConverter.GetBytes((int)0)), 0, 4); //Argument count
            outstream.Write(ReverseEndian(BitConverter.GetBytes((int)0)), 0, 4); //Argument value offset
            outstream.Write(ReverseEndian(BitConverter.GetBytes((int)0)), 0, 4); //Reserved entry

            //Write object table header
            outstream.Write(ReverseEndian(BitConverter.GetBytes((int)items_in_table)), 0, 4); //Number of elements in table
            outstream.Write(ReverseEndian(BitConverter.GetBytes((int)OBJECT_TABLE_SIZE)), 0, 4); //Size of table
            outstream.Write(ReverseEndian(BitConverter.GetBytes((int)0)), 0, 4); //Reserved
            outstream.Write(ReverseEndian(BitConverter.GetBytes((int)0)), 0, 4); //Reserved

            int codeOffset = 32 + (OBJECT_TABLE_SIZE * 16);

            //First entry, the bootloader
            outstream.Write(ReverseEndian(BitConverter.GetBytes((int)AccCIL.KnownObjectTypes.Bootloader)), 0, 4); //Type
            outstream.Write(ReverseEndian(BitConverter.GetBytes((int)BOOTLOADER_LENGTH)), 0, 4); //Size
            outstream.Write(ReverseEndian(BitConverter.GetBytes((int)codeOffset)), 0, 4); //Pointer
            outstream.Write(ReverseEndian(BitConverter.GetBytes((int)0)), 0, 4); //Typename pointer

            codeOffset += BOOTLOADER_LENGTH;
            codeOffset += (16 - BOOTLOADER_LENGTH % 16) % 16;

            Dictionary<CompiledMethod, int> methodOffsets = new Dictionary<CompiledMethod,int>();
            foreach(CompiledMethod cm in methods)
            {
                cm.Prolouge = GenerateProlouge(cm);
                cm.Epilouge = GenerateEpilouge(cm);

                int codesize = cm.TotalSize;

                outstream.Write(ReverseEndian(BitConverter.GetBytes((int)AccCIL.KnownObjectTypes.Code)), 0, 4); //Type
                outstream.Write(ReverseEndian(BitConverter.GetBytes((int)codesize)), 0, 4); //Size
                outstream.Write(ReverseEndian(BitConverter.GetBytes((int)codeOffset)), 0, 4); //Pointer
                outstream.Write(ReverseEndian(BitConverter.GetBytes((int)codeOffset + cm.CodeSize)), 0, 4); //Typename pointer

                methodOffsets.Add(cm, codeOffset);

                codeOffset += codesize;
            }


            //Fill with zeros for remaining entries in object table
            byte[] zeroentry = new byte[16];
            for (int i = 0; i < OBJECT_TABLE_SIZE - items_in_table; i++)
                outstream.Write(zeroentry, 0, zeroentry.Length);

            bootloader_offset = (uint)outstream.Length;

            List<SPEEmulator.OpCodes.Bases.Instruction> tmp = new List<SPEEmulator.OpCodes.Bases.Instruction>();

            tmp.AddRange(BOOT_LOADER);
            int callhandlerOffset = tmp.Count;
            tmp.AddRange(CALL_HANDLER);

            //Patch the entry point adress
            int entryfunctionOffset = tmp.Count + (4 - tmp.Count % 4) % 4;
            ((SPEEmulator.OpCodes.brsl)tmp[callhandlerOffset - INSTRUCTION_OFFSET_FOR_MAIN_BRSL]).I16 = (uint)(((entryfunctionOffset - callhandlerOffset)) + INSTRUCTION_OFFSET_FOR_MAIN_BRSL);

            if (assemblyOutput != null)
                assemblyOutput.WriteLine("# Bootloader");

            //Flush loader code
            InstructionsToBytes(tmp, outstream, assemblyOutput, null);
            outstream.Write(zeroentry, 0, (16 - BOOTLOADER_LENGTH % 16) % 16);

            //Create a fast lookup table            
            Dictionary<Mono.Cecil.MethodReference, int> methodOffsetLookup = methodOffsets.ToDictionary(x => (Mono.Cecil.MethodReference)x.Key.Method.Method, x => x.Value / 4);

            //We know the layout of each method, we can patch the call instructions
            foreach (CompiledMethod cm in methods)
            {
                List<string> constants = cm.Constants.Distinct().ToList();
                Dictionary<string, int> offsets = new Dictionary<string, int>();

                for(int i = 0; i < constants.Count; i++)
                    offsets.Add(constants[i], i * 4);

                if (assemblyOutput != null)
                {
                    assemblyOutput.WriteLine("###########################################");
                    assemblyOutput.WriteLine("# Begin Function: " + cm.Method.Method.Name);
                    assemblyOutput.WriteLine("###########################################");
                    assemblyOutput.WriteLine();
                }

                int constantOffsets = cm.Instructions.Count + ((4 - (cm.Prolouge.Count + cm.Instructions.Count) % 4) % 4);

                cm.PatchBranches();
                cm.PatchCalls(methodOffsetLookup, callhandlerOffset);
                cm.PatchConstants(constantOffsets, offsets);

                int offset = (cm.Prolouge.Count + cm.Instructions.Count + cm.Epilouge.Count) * 4;


                if (assemblyOutput != null)
                    assemblyOutput.WriteLine("### Prolouge Begin ###");
                InstructionsToBytes(cm.Prolouge, outstream, assemblyOutput, null);
                if (assemblyOutput != null)
                    assemblyOutput.WriteLine("### Prolouge End ###");
#if DEBUG
                long streamOffset = outstream.Position / 4;
                Dictionary<long, List<Mono.Cecil.Cil.Instruction>> instructionLookup = new Dictionary<long, List<Mono.Cecil.Cil.Instruction>>();
                foreach (KeyValuePair<Mono.Cecil.Cil.Instruction, int> x in cm.InstructionOffsets)
                {
                    List<Mono.Cecil.Cil.Instruction> ix;
                    instructionLookup.TryGetValue(x.Value + streamOffset, out ix);
                    if (ix == null)
                        instructionLookup.Add(x.Value + streamOffset, ix = new List<Mono.Cecil.Cil.Instruction>());
                    ix.Add(x.Key);
                }
#else
                Dictionary<int, List<Mono.Cecil.Cil.Instruction>> instructionLookup = null;
#endif

                InstructionsToBytes(cm.Instructions, outstream, assemblyOutput, instructionLookup);
                if (assemblyOutput != null)
                    assemblyOutput.WriteLine("### Epilouge Begin ###");
                InstructionsToBytes(cm.Epilouge, outstream, assemblyOutput, null);
                if (assemblyOutput != null)
                    assemblyOutput.WriteLine("### Epilouge End ###");

                outstream.Write(zeroentry, 0, (16 - offset % 16) % 16);

                foreach (string s in constants)
                {
                    ulong high = ulong.Parse(s.Substring(0, 16), System.Globalization.NumberStyles.HexNumber);
                    ulong low = ulong.Parse(s.Substring(16), System.Globalization.NumberStyles.HexNumber);

                    outstream.Write(ReverseEndian(BitConverter.GetBytes(high)), 0, 8);
                    outstream.Write(ReverseEndian(BitConverter.GetBytes(low)), 0, 8);

                    if (assemblyOutput != null)
                        assemblyOutput.WriteLine("Constant: " + s);
                }

                outstream.Write(ReverseEndian(BitConverter.GetBytes((int)cm.FunctionName.Length)), 0, 4);
                outstream.Write(cm.FunctionName, 0, cm.FunctionName.Length);

                outstream.Write(zeroentry, 0, (16 - (cm.FunctionName.Length + 4) % 16) % 16);

                if (assemblyOutput != null)
                {
                    assemblyOutput.WriteLine();
                    assemblyOutput.WriteLine();
                }
            }

            //If there is an assemblyStream present, write text representation
            /*if (assemblyOutput != null)
            {
                offset = 0;
                foreach (SPEEmulator.OpCodes.Bases.Instruction i in output)
                {
                    if (methodOffsetLookup.ContainsValue(offset))
                    {
                        foreach (KeyValuePair<Mono.Cecil.MethodReference, int> p in methodOffsetLookup)
                            if (p.Value == offset)
                                assemblyOutput.WriteLine("# Function entry: " + p.Key.Name);
                    }
                    assemblyOutput.WriteLine((offset * 4).ToString("x4") + ": " + i.ToString());
                    offset++;
                }
            }*/

        }

        private static byte[] ReverseEndian(byte[] input)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(input);
            return input;
        }

        /// <summary>
        /// This contains a handwritten boot kernel that handles startup and call resolution
        /// </summary>
        private static readonly SPEEmulator.OpCodes.Bases.Instruction[] BOOT_LOADER = new SPEEmulator.OpCodes.Bases.Instruction[] {
            //Start by setting up the the SP
            new SPEEmulator.OpCodes.xor(0, 0, 0), //Clear register $0
            new SPEEmulator.OpCodes.ila(_SP, (uint)(0x40000 - REGISTER_SIZE)), //Set SP to LS_SIZE - 16
            new SPEEmulator.OpCodes.stqd(0, _SP, 0x0), //Set the Back Chain to zero
            new SPEEmulator.OpCodes.stqd(_SP, _SP, (uint)((-2) & 0x3ff)), //Set the Back Chain to zero
            new SPEEmulator.OpCodes.ai(_SP, _SP, (uint)((-(REGISTER_SIZE * 2)) & 0x3ff)), //Increment SP

            //Entry point for the application, start by loading the argument count
            new SPEEmulator.OpCodes.lqd(_TMP0, 0, 0x0), //Load the value at position 0x0

            //Intialize loop by reading count and argument offset
            new SPEEmulator.OpCodes.fsmbi(_TMP1, 0xf000), //Prepare a select mask
            new SPEEmulator.OpCodes.rotqbyi(_TMP2, _TMP0, 0x8), //Move the argument start adress into preferred slot 
            new SPEEmulator.OpCodes.and(_TMP2, _TMP2, _TMP1), //Exclude the unwanted positions for adress
            new SPEEmulator.OpCodes.rotqbyi(_TMP0, _TMP0, 0x4), //Move the argument count into preferred slot 
            new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1), //Exclude the unwanted positions for count

            new SPEEmulator.OpCodes.brz(_TMP0, 20), //Skip the initialization stuff if the start has no arguments

            //TMP0 is the argument counter, TMP1 is the target register increment value, TMP2 is the argument adress
            new SPEEmulator.OpCodes.ila(_TMP1, 0x1), //We need to increment the target register with 1
            new SPEEmulator.OpCodes.shlqbyi(_TMP1, _TMP1, 12), //The target register value is located in byte 3

            //Adjust offset so we get the lqr instruction at the right offset
            new SPEEmulator.OpCodes.nop(),
            new SPEEmulator.OpCodes.nop(),

            //Load the current storage operation
            new SPEEmulator.OpCodes.lqr(_TMP3, 4), //Load current instruction
            new SPEEmulator.OpCodes.ori(_TMP4, _TMP3, 0), //Save an unmodified copy
            new SPEEmulator.OpCodes.nop(), //Adjust offset so we can access the instruction directly

            //Start loop
            new SPEEmulator.OpCodes.brz(_TMP0, 11), //while($75 != 0)
            
                //Load value from list into current register -> NOTE: SELF MODIFYING CODE HERE!
                new SPEEmulator.OpCodes.lqd(_ARG0, _TMP2, 0),

                //Adjust offsets
                new SPEEmulator.OpCodes.ai(_TMP2, _TMP2, REGISTER_SIZE), //Next address
                new SPEEmulator.OpCodes.ai(_TMP0, _TMP0, (uint)(-1 & 0x3ff)), //Decrement counter

                //Modify instruction to use next register
                new SPEEmulator.OpCodes.nop(), //Adjust offset so we can access the instruction directly
                new SPEEmulator.OpCodes.lqr(_TMP3, (uint)((-4) & 0xffff)), //Load current instruction
                new SPEEmulator.OpCodes.a(_TMP3, _TMP3, _TMP1), //Increment the target register
        
                new SPEEmulator.OpCodes.nop(), //Adjust offset so we can access the instruction directly
                new SPEEmulator.OpCodes.nop(), //Adjust offset so we can access the instruction directly
                new SPEEmulator.OpCodes.stqr(_TMP3, (uint)((-8) & 0xffff)), //Write the new instruction

            new SPEEmulator.OpCodes.br(_TMP0, (uint)(-10 & 0xffff)), //End of while loop

            //We restore the modified instruction, so the bootloader can be called twice
            new SPEEmulator.OpCodes.nop(), //Adjust offset so we can access the instruction directly
            new SPEEmulator.OpCodes.nop(), //Adjust offset so we can access the instruction directly
            new SPEEmulator.OpCodes.stqr(_TMP4, (uint)(-12 & 0xffff)), //Write the unmodified instruction back

            //Jump to the address of the entry function
            new SPEEmulator.OpCodes.brsl(_LR, 0xffff), //Jump to entry
            new SPEEmulator.OpCodes.xor(_TMP0, _TMP0, _TMP0), //Clear a pointer
            new SPEEmulator.OpCodes.stqd(_ARG0, _TMP0, 0x0), //Copy the return value to position 0x0
            new SPEEmulator.OpCodes.stop(0x3000)
        };

        /// <summary>
        /// The offset of the bootloader main brsl instruction, counting from the end of the bootloader
        /// </summary>
        private const int INSTRUCTION_OFFSET_FOR_MAIN_BRSL = 4;


        /// <summary>
        /// This is the call handler function
        /// All function calls are routed through this, and it uses the PPE to resolve the actual call address
        /// </summary>
        private static readonly SPEEmulator.OpCodes.Bases.Instruction[] CALL_HANDLER = new SPEEmulator.OpCodes.Bases.Instruction[] {
            new SPEEmulator.OpCodes.stop(0x3010) //TODO: Make it actually work

            //To get this working, there should be a table in memory with the current
            // methods loaded and the call instructions
            //When a method is invoked, the table is checked first, and otherwise
            // the PPE is activated and asked to load the code and update the table
        };


        public ICompiledMethod JIT(AccCIL.IR.MethodEntry method)
        {
            CompiledMethod state = new CompiledMethod(method);
            SPEOpCodeMapper mapper = new SPEOpCodeMapper(state);

            state.StartFunction();

            //We store the local variables in the permanent registers followed by the function arguments
            int locals = method.Method.Body.Variables.Count;
            int args = method.Method.Parameters.Count;
            int permRegs = locals + args;

            //TODO: Handle this case by storing the extra data on the stack
            if (permRegs > MAX_LV_REGISTERS)
                throw new Exception("Too many locals+arguments");

            //Make sure the register usage is clean
            method.ResetVirtualRegisters();

            //TODO: should be able to re-use registers used by the arguments, which frees appx 70 extra registers
            List<int> usedRegs = new RegisterAllocator().AllocateRegisters(_LV0 + permRegs, new AccCIL.SimpleAllocator(), method);

            //If we need to store locals, we must preserve the local variable registers
            for (int i = 0; i < permRegs; i++)
            {
                mapper.PushStack(new TemporaryRegister((uint)(_LV0 + i)));
                usedRegs.Remove(_LV0 + i);
            }

            //All remaining used registers must also be preserved
            foreach (int i in usedRegs.Reverse<int>())
                if (i > _LV0)
                    mapper.PushStack(new TemporaryRegister((uint)(i)));

            //Clear as required
            if (method.Method.Body.InitLocals)
                for (int i = 0; i < locals; i++)
                    mapper.ClearRegister((uint)(_LV0 + i));

            //Now copy over the arguments
            for (int i = 0; i < args; i++)
                mapper.CopyRegister((uint)(_ARG0 + i), (uint)(_LV0 + locals + i));

            int requiredStackDepth = state.StackDepth;

            //Now add each parsed subtree
            foreach (AccCIL.IR.InstructionElement el in method.Childnodes)
            {
                RecursiveTranslate(state, mapper, el);
                System.Diagnostics.Trace.Assert(state.StackDepth >= requiredStackDepth);
            }

            state.EndFunction();

            //All used registers must be preserved
            foreach (int i in usedRegs)
                if (i > _LV0)
                    mapper.PopStack((uint)(i), true);

            //If we had to store locals, we must restore the local variable registers
            for (int i = 0; i < permRegs; i++)
                mapper.PopStack((uint)(_LV0 + (permRegs - i - 1)), true);


            System.Diagnostics.Trace.Assert(state.StackDepth == 0);

            return state;
        }

        /// <summary>
        /// Adds an ABI compliant SPE method prolouge to the instruction stream
        /// </summary>
        private static List<SPEEmulator.OpCodes.Bases.Instruction> GenerateProlouge(CompiledMethod state)
        {
            List<SPEEmulator.OpCodes.Bases.Instruction> tmplist = new List<SPEEmulator.OpCodes.Bases.Instruction>();

            tmplist.Add(new SPEEmulator.OpCodes.stqd(_LR, _SP, 1));

            uint stackDepth = state.MaxStackDepth + 2;

            if (stackDepth * (REGISTER_SIZE / 16) <= 0x1FF)
                tmplist.Add(new SPEEmulator.OpCodes.stqd(_SP, _SP, (uint)((-(stackDepth * (REGISTER_SIZE / 16))) & 0x3ff)));
            else if (stackDepth * (REGISTER_SIZE / 16) <= 0x7FFF)
            {
                tmplist.Add(new SPEEmulator.OpCodes.il(_TMP0, (uint)((-(stackDepth * (REGISTER_SIZE / 16))) & 0xFFFF)));
                tmplist.Add(new SPEEmulator.OpCodes.a(_TMP0, _SP, _TMP0));
                tmplist.Add(new SPEEmulator.OpCodes.stqd(_SP, _TMP0, 0));
            }
            else
            {
                //Note if this restraint is removed, beware that all code that uses "lqd $target, _SP(index)" won't work
                throw new Exception("Stack space is larger than 0x7fff");
            }

            if (stackDepth * (REGISTER_SIZE) <= 0x1FF)
                tmplist.Add(new SPEEmulator.OpCodes.ai(_SP, _SP, (uint)((-(stackDepth * (REGISTER_SIZE))) & 0x3ff)));
            else if (stackDepth * (REGISTER_SIZE) <= 0x7FFF)
            {
                tmplist.Add(new SPEEmulator.OpCodes.il(_TMP0, (uint)((-(stackDepth * (REGISTER_SIZE))) & 0xFFFF)));
                tmplist.Add(new SPEEmulator.OpCodes.a(_SP, _SP, _TMP0));
            }
            else
                throw new Exception("Stack space is larger than 0x7fff");

            return tmplist;
        }

        /// <summary>
        /// Adds an ABI compliant SPE method epilouge to the instruction stream
        /// </summary>
        private static List<SPEEmulator.OpCodes.Bases.Instruction> GenerateEpilouge(CompiledMethod state)
        {
            List<SPEEmulator.OpCodes.Bases.Instruction> tmplist = new List<SPEEmulator.OpCodes.Bases.Instruction>();

            uint stackDepth = state.MaxStackDepth + 2;

            if (stackDepth * REGISTER_SIZE <= 0x1FF)
                tmplist.Add(new SPEEmulator.OpCodes.ai(_SP, _SP, stackDepth * REGISTER_SIZE));
            else if (stackDepth * REGISTER_SIZE <= 0x7FFF)
            {
                tmplist.Add(new SPEEmulator.OpCodes.il(_TMP0, stackDepth * (REGISTER_SIZE)));
                tmplist.Add(new SPEEmulator.OpCodes.a(_SP, _SP, _TMP0));
            }
            else
                throw new Exception("Stack space is larger than 0x7fff");

            tmplist.Add(new SPEEmulator.OpCodes.lqd(_LR, _SP, 1));
            tmplist.Add(new SPEEmulator.OpCodes.bi(_LR, _LR));

            int size = state.Prolouge.Count + state.Instructions.Count + tmplist.Count;

            while (size % 4 != 0)
            {
                tmplist.Add(new SPEEmulator.OpCodes.nop());
                size++;
            }
                
            return tmplist;
        }

        private static void RecursiveTranslate(CompiledMethod state, SPEOpCodeMapper mapper, AccCIL.IR.InstructionElement el)
        {
            foreach (AccCIL.IR.InstructionElement els in el.Childnodes)
                RecursiveTranslate(state, mapper, els);

            System.Reflection.MethodInfo translator;
            if (!_opTranslations.TryGetValue(el.Instruction.OpCode.Code, out translator))
                throw new Exception(string.Format("Missing a translator for CIL code {0}", el.Instruction.OpCode.Code));

            state.StartInstruction(el.Instruction);
            translator.Invoke(mapper, new object[] { el });
            state.EndInstruction();
        }

        private static Dictionary<Mono.Cecil.Cil.Code, System.Reflection.MethodInfo> BuildTranslationTable()
        {
            Dictionary<Mono.Cecil.Cil.Code, System.Reflection.MethodInfo> res = new Dictionary<Mono.Cecil.Cil.Code, System.Reflection.MethodInfo>();

            Mono.Cecil.Cil.Code v;
            foreach (System.Reflection.MethodInfo mi in typeof(SPEOpCodeMapper).GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
                if (Enum.TryParse<Mono.Cecil.Cil.Code>(mi.Name, true, out v))
                    res[v] = mi;
            
            return res;
        }

    }
}
