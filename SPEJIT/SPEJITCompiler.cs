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
        /// The number of bytes into the bootloader where execution should start
        /// </summary>
        private const uint BOOTLOADER_START_OFFSET = INSTRUCTION_SIZE * 4;

        /// <summary>
        /// The first register used for local variables
        /// </summary>
        public const int _LV0 = 80;

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
                EmitInstructionStream(ms, assemblyOutput, methods);

                SPEEmulator.ELFReader.EmitELFHeader((uint)ms.Length, BOOTLOADER_START_OFFSET, outstream);
                
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
        public void EmitInstructionStream(System.IO.Stream outstream, System.IO.TextWriter assemblyOutput, IEnumerable<AccCIL.IR.MethodEntry> methods)
        {
            List<ICompiledMethod> cmps = new List<ICompiledMethod>();
            foreach (AccCIL.IR.MethodEntry me in methods)
                cmps.Add(JIT(me));

            EmitInstructionStream(outstream, assemblyOutput, cmps);
        }

        /// <summary>
        /// Emits an instruction stream.
        /// </summary>
        /// <param name="outstream">The output stream</param>
        /// <param name="assemblyOutput">The assembly text output, can be null</param>
        /// <param name="methods">The compiled methods</param>
        public void EmitInstructionStream(System.IO.Stream outstream, System.IO.TextWriter assemblyOutput, IEnumerable<ICompiledMethod> _methods)
        {
            //TODO: Not sure if the SPE always starts at address 0, or if the start offset can be specified
            //Seems like all ELF files have a special ".init" section
            List<SPEEmulator.OpCodes.Bases.Instruction> output = new List<SPEEmulator.OpCodes.Bases.Instruction>();
            output.AddRange(BOOT_LOADER);

            int callhandlerOffset = output.Count;
            output.AddRange(CALL_HANDLER);

            List<CompiledMethod> methods = new List<CompiledMethod>();
            foreach (ICompiledMethod m in _methods)
                methods.Add((CompiledMethod)m);

            //Get a list of builtins
            Dictionary<string, Mono.Cecil.MethodDefinition> builtins = new Dictionary<string, Mono.Cecil.MethodDefinition>(CompiledMethod.m_builtins);

            for(int i = 0; i < methods.Count; i++)
            {
                CompiledMethod cm = methods[i];

                foreach (Mono.Cecil.MethodReference mr in cm.CalledMethods)
                    if (mr.DeclaringType.FullName == "SPEJIT.BuiltInMethods" && builtins.ContainsKey(mr.Name))
                    {
                        methods.Add((CompiledMethod)AccCIL.AccCIL.JIT(this, builtins[mr.Name]));
                        builtins.Remove(mr.Name);
                    }
                            
            }

            //Gather all required constants
            Dictionary<string, int> constants = new Dictionary<string, int>();
            int constantIndex = 0;
            foreach (CompiledMethod cm in methods)
                foreach (string v in cm.Constants)
                    if (!constants.ContainsKey(v))
                        constants.Add(v, constantIndex++);

            //Patch the entry point adress
            int entryfunctionOffset = output.Count;
            ((SPEEmulator.OpCodes.brsl)output[callhandlerOffset - 2]).I16 = (uint)(((entryfunctionOffset - callhandlerOffset)) + 2);

            //Before we emit the actual code, we need to patch all calls
            Dictionary<Mono.Cecil.MethodReference, int> methodOffsets = new Dictionary<Mono.Cecil.MethodReference, int>();

            int offset = output.Count;
            foreach (CompiledMethod cm in methods)
            {
                methodOffsets.Add(cm.Method.Method, offset);
                cm.Prolouge = GenerateProlouge(cm);
                cm.Epilouge = GenerateEpilouge(cm);
                offset += cm.Instructions.Count + cm.Prolouge.Count + cm.Epilouge.Count;
            }

            

            //Now that we know the layout of each method, we can patch the call instructions
            foreach (CompiledMethod cm in methods)
            {
                cm.PatchBranches();
                cm.PatchCalls(methodOffsets, callhandlerOffset);
            }

            //Now gather all instructions
            foreach (CompiledMethod cm in methods)
            {
                output.AddRange(cm.Prolouge);
                output.AddRange(cm.Instructions);
                output.AddRange(cm.Epilouge);
            }

            //Pad with nops to be 16 byte aligned
            while (output.Count % 4 != 0)
                output.Add(new SPEEmulator.OpCodes.nop());

            //Now we know the size of the code area, so we can layout the constants
            if (constants.Count > 0)
            {
                int constantOffset = output.Count;

                foreach (CompiledMethod cm in methods)
                    cm.PatchConstants(constantOffset - methodOffsets[cm.Method.Method] - cm.Prolouge.Count, constants);
            }


            //All instructions are JIT'ed, so flush them as binary output
            foreach (SPEEmulator.OpCodes.Bases.Instruction i in output)
                outstream.Write(ReverseEndian(BitConverter.GetBytes(i.Value)), 0, 4);

            if (constants.Count > 0)
            {
                SortedList<int, string> tmp = new SortedList<int,string>();
                foreach (KeyValuePair<string, int> c in constants)
                    tmp.Add(c.Value, c.Key);

                foreach (string s in tmp.Values)
                {
                    ulong high = ulong.Parse(s.Substring(0, 16), System.Globalization.NumberStyles.HexNumber);
                    ulong low = ulong.Parse(s.Substring(16), System.Globalization.NumberStyles.HexNumber);

                    outstream.Write(ReverseEndian(BitConverter.GetBytes(high)), 0, 8);
                    outstream.Write(ReverseEndian(BitConverter.GetBytes(low)), 0, 8);
                }
            }

            //If there is an assemblyStream present, write text representation
            if (assemblyOutput != null)
            {
                offset = 0;
                foreach (SPEEmulator.OpCodes.Bases.Instruction i in output)
                {
                    if (methodOffsets.ContainsValue(offset))
                    {
                        foreach (KeyValuePair<Mono.Cecil.MethodReference, int> p in methodOffsets)
                            if (p.Value == offset)
                                assemblyOutput.WriteLine("# Function entry: " + p.Key.Name);
                    }
                    assemblyOutput.WriteLine((offset * 4).ToString("x4") + ": " + i.ToString());
                    offset++;
                }
            }

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
            new SPEEmulator.OpCodes.stop(), //First entry (0x0) is always set to 0, used for testing null pointer read/writes 
            new SPEEmulator.OpCodes.stop(), //Second entry (0x4) is reserved for the argument count
            new SPEEmulator.OpCodes.stop(), //Third entry (0x8) is reserved for the pointer to LS startup data
            new SPEEmulator.OpCodes.stop(), //Fourth entry is reserved for padding

            //Start by setting up the the SP
            new SPEEmulator.OpCodes.il(0, 0), //Clear register $0
            new SPEEmulator.OpCodes.ila(_SP, (uint)(0x40000 - REGISTER_SIZE)), //Set SP to LS_SIZE - 16
            new SPEEmulator.OpCodes.stqd(0, _SP, 0x0), //Set the Back Chain to zero
            new SPEEmulator.OpCodes.ai(_SP, _SP, (uint)((-REGISTER_SIZE) & 0x3ff)), //Increment SP

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
            new SPEEmulator.OpCodes.stop()
        };


        /// <summary>
        /// This is the call handler function
        /// All function calls are routed through this, and it uses the PPE to resolve the actual call address
        /// </summary>
        private static readonly SPEEmulator.OpCodes.Bases.Instruction[] CALL_HANDLER = new SPEEmulator.OpCodes.Bases.Instruction[] {
            new SPEEmulator.OpCodes.stop() //TODO: Make it actually work

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
            foreach (int i in usedRegs)
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

            //If the function returns a value, place it in $3
            if (state.Method.Method.ReturnType.ReturnType.FullName != "System.Void")
                mapper.PopStack(_ARG0, true);

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

            if (state.MaxStackDepth * (REGISTER_SIZE / 4) <= 0x1FF)
                tmplist.Add(new SPEEmulator.OpCodes.stqd(_SP, _SP, (uint)((-(state.MaxStackDepth * (REGISTER_SIZE / 4))) & 0x3ff)));
            else if (state.MaxStackDepth * (REGISTER_SIZE / 4) <= 0x7FFF)
            {
                tmplist.Add(new SPEEmulator.OpCodes.il(_TMP0, (uint)((-(state.MaxStackDepth * (REGISTER_SIZE))) & 0xFFFF)));
                tmplist.Add(new SPEEmulator.OpCodes.a(_TMP0, _SP, _TMP0));
                tmplist.Add(new SPEEmulator.OpCodes.stqd(_SP, _TMP0, 0));
            }
            else
            {
                //Note if this restraint is removed, beware that all code that uses "lqd $target, _SP(index)" won't work
                throw new Exception("Stack space is larger than 0x7fff");
            }

            if (state.MaxStackDepth * (REGISTER_SIZE) <= 0x1FF)
                tmplist.Add(new SPEEmulator.OpCodes.ai(_SP, _SP, (uint)((-(state.MaxStackDepth * (REGISTER_SIZE))) & 0x3ff)));
            else if (state.MaxStackDepth * (REGISTER_SIZE) <= 0x7FFF)
            {
                tmplist.Add(new SPEEmulator.OpCodes.il(_TMP0, (uint)((-(state.MaxStackDepth * (REGISTER_SIZE))) & 0xFFFF)));
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
            
            if (state.MaxStackDepth * REGISTER_SIZE <= 0x1FF)
                tmplist.Add(new SPEEmulator.OpCodes.ai(_SP, _SP, state.MaxStackDepth * REGISTER_SIZE));
            else if (state.MaxStackDepth * REGISTER_SIZE <= 0x7FFF)
            {
                tmplist.Add(new SPEEmulator.OpCodes.il(_TMP0, state.MaxStackDepth * (REGISTER_SIZE)));
                tmplist.Add(new SPEEmulator.OpCodes.a(_SP, _SP, _TMP0));
            }
            else
                throw new Exception("Stack space is larger than 0x7fff");

            tmplist.Add(new SPEEmulator.OpCodes.lqd(_LR, _SP, 1));
            tmplist.Add(new SPEEmulator.OpCodes.bi(_LR, _LR));

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
