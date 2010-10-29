using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPEJIT
{
    /// <summary>
    /// Class that converts IR to SPE opcodes
    /// </summary>
    public class SPEJIT
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

        //ABI specification states that $75 to $79 are scratch registers
        public const uint _TMP0 = 75;
        public const uint _TMP1 = 76;
        public const uint _TMP2 = 77;
        public const uint _TMP3 = 78;

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
        static SPEJIT()
        {
            _opTranslations = BuildTranslationTable();
        }

        /// <summary>
        /// An ABI compliant SPE method prologue, note that instructions [1] and [2] must be patched with the used stack size
        /// </summary>
        private static readonly SPEEmulator.OpCodes.Bases.Instruction[] METHOD_PROLOGUE = new SPEEmulator.OpCodes.Bases.Instruction[] 
        {
            new SPEEmulator.OpCodes.stqd(_LR, _SP, LR_OFFSET),
            new SPEEmulator.OpCodes.stqd(_SP, _SP, 0), //0 is placeholder for negative stackframe size
            new SPEEmulator.OpCodes.ai(_SP, _SP, 0)  //0 is placeholder for negative stackframe size
        };


        /// <summary>
        /// An ABI compliant SPE method epilogue, note that instruction [0] must be patched with the used stack size
        /// </summary>
        private static readonly SPEEmulator.OpCodes.Bases.Instruction[] METHOD_EPILOGUE = new SPEEmulator.OpCodes.Bases.Instruction[] 
        {
            new SPEEmulator.OpCodes.ai(_SP, _SP, 0), //0 is placeholder for stackframe size
            new SPEEmulator.OpCodes.lqd(_LR, _SP, LR_OFFSET),
            new SPEEmulator.OpCodes.bi(_LR, _LR)

        };


        /// <summary>
        /// Emits an instruction stream.
        /// </summary>
        /// <param name="outstream">The output stream</param>
        /// <param name="assemblyOutput">The assembly text output, can be null</param>
        /// <param name="methods">The compiled methods</param>
        internal void EmitInstructionStream(System.IO.Stream outstream, System.IO.TextWriter assemblyOutput, IEnumerable<IR.MethodEntry> methods)
        {
            List<CompiledMethod> cmps = new List<CompiledMethod>();
            foreach (IR.MethodEntry me in methods)
                cmps.Add(JIT(me));

            EmitInstructionStream(outstream, assemblyOutput, cmps);
        }

        /// <summary>
        /// Emits an instruction stream.
        /// </summary>
        /// <param name="outstream">The output stream</param>
        /// <param name="assemblyOutput">The assembly text output, can be null</param>
        /// <param name="methods">The compiled methods</param>
        public void EmitInstructionStream(System.IO.Stream outstream, System.IO.TextWriter assemblyOutput, IEnumerable<CompiledMethod> methods)
        {
            List<SPEEmulator.OpCodes.Bases.Instruction> output = new List<SPEEmulator.OpCodes.Bases.Instruction>();
            output.AddRange(BOOT_LOADER);

            int callhandlerOffset = output.Count;
            output.AddRange(CALL_HANDLER);

            //Before we emit the actual code, we need to patch all calls
            Dictionary<Mono.Cecil.MethodDefinition, int> methodOffsets = new Dictionary<Mono.Cecil.MethodDefinition, int>();

            int offset = output.Count;
            foreach (CompiledMethod cm in methods)
            {
                methodOffsets.Add(cm.Method.Method, offset);
                offset += cm.Instructions.Count;
            }

            //Now that we know the layout of each method, we can patch the call instructions
            foreach (CompiledMethod cm in methods)
                cm.PatchCalls(methodOffsets, callhandlerOffset);

            //Now gather all instructions
            foreach (CompiledMethod cm in methods)
                output.AddRange(cm.Instructions);

            //All instructions are JIT'ed, so flush them as binary output
            foreach (SPEEmulator.OpCodes.Bases.Instruction i in output)
                outstream.Write(BitConverter.GetBytes(i.Value), 0, 4);

            //If there is an assemblyStream present, write text representation
            if (assemblyOutput != null)
            {
                offset = 0;
                foreach (SPEEmulator.OpCodes.Bases.Instruction i in output)
                {
                    if (methodOffsets.ContainsValue(offset))
                        assemblyOutput.WriteLine("# Function entry");
                    assemblyOutput.WriteLine(i.ToString());
                    offset++;
                }
            }

        }

        /// <summary>
        /// This contains a handwritten boot kernel that handles startup and call resolution
        /// </summary>
        private static readonly SPEEmulator.OpCodes.Bases.Instruction[] BOOT_LOADER = new SPEEmulator.OpCodes.Bases.Instruction[] {
            new SPEEmulator.OpCodes.stop(), //First entry (0x0) is always set to 0
            new SPEEmulator.OpCodes.stop(), //Second entry (0x4) is reserved for the pointer to LS startup data

            //Start by setting up the the SP
            new SPEEmulator.OpCodes.il(_SP, (uint)(0x10000 - 8)), //Set SP to LS_SIZE - 8
            new SPEEmulator.OpCodes.xor(0, 0, 0), //Clear register $0
            new SPEEmulator.OpCodes.stqd(_SP, 0, 0x0), //Set the Back Chain to zero

            //Entry point for the application, start by loading the argument list
            new SPEEmulator.OpCodes.lqd(_TMP0, 0, 0x4),

            //Initialize loop
            new SPEEmulator.OpCodes.il(_TMP1, _ARG0), //First argument register is $3
            new SPEEmulator.OpCodes.lqd(_TMP2, _TMP0, 0x0), //Get the argument count from the argument list
            new SPEEmulator.OpCodes.ai(_TMP2, _TMP2, REGISTER_SIZE), //Next address

            //Start loop
            new SPEEmulator.OpCodes.brz(_TMP0, 0xffff), //while($75 != 0)
            
            //Load value from list into current register
            new SPEEmulator.OpCodes.lqd(_ARG0, _TMP2, REGISTER_SIZE),

            //Adjust offsets
            new SPEEmulator.OpCodes.ai(_TMP1, _TMP1, 0x1), //Next register
            new SPEEmulator.OpCodes.ai(_TMP2, _TMP2, REGISTER_SIZE), //Next address
            new SPEEmulator.OpCodes.ai(_TMP0, _TMP0, (uint)(-REGISTER_SIZE & 0x3ff)), //Decrement counter

            //Modify instruction to use next register
            new SPEEmulator.OpCodes.lqr(_TMP3, (uint)((-4 * INSTRUCTION_SIZE) & 0x3ff)), //Load current instruction
            new SPEEmulator.OpCodes.lqr(_TMP3, (uint)((-4 * INSTRUCTION_SIZE) & 0x3ff)), //Increment the target register <---------------- Fix this ----------------------------------
            new SPEEmulator.OpCodes.stqr(_TMP3, (uint)((-6 * INSTRUCTION_SIZE) & 0x3ff)), //Write the new instruction

            new SPEEmulator.OpCodes.br(_TMP0, 0xffff), //End of while loop

            //Jump to the address of the entry function
            new SPEEmulator.OpCodes.lqd(_TMP2, _TMP0, 0x0), //Load the address of the entry function
            new SPEEmulator.OpCodes.bra(_TMP2, 0x0), //Jump to entry
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


        internal CompiledMethod JIT(IR.MethodEntry method)
        {
            CompiledMethod state = new CompiledMethod(method);
            SPEOpCodeMapper mapper = new SPEOpCodeMapper(state);

            state.StartFunction();

            //First thing we need is the prologue, which preserves the caller stack
            state.Instructions.AddRange(METHOD_PROLOGUE);

            //We store the local variables in the permanent registers followed by the function arguments
            int locals = method.Method.Body.Variables.Count;
            int args = method.Method.Parameters.Count;
            int permRegs = locals + args;

            //TODO: Handle this case by storing the extra data on the stack
            if (permRegs > MAX_LV_REGISTERS)
                throw new Exception("Too many locals+arguments");

            //If we need to store locals, we must preserve the local variable registers
            for (int i = 0; i < permRegs; i++)
                mapper.PushStack((uint)(_LV0 + i));

            //Clear as required
            if (method.Method.Body.InitLocals)
                for (int i = 0; i < locals; i++)
                    mapper.ClearRegister((uint)(_LV0 + i));

            //Now copy over the arguments
            for (int i = 0; i < args; i++)
                mapper.CopyRegister((uint)(_ARG0 + i), (uint)(_LV0 + locals + i));

            //Now add each parsed subtree
            foreach (IR.InstructionElement el in method.Childnodes)
                RecursiveTranslate(state, mapper, el);

            //If we had to store locals, we must restore the local variable registers
            for (int i = 0; i < permRegs; i++)
                mapper.PopStack((uint)(_LV0 + locals - i - 1));

            //We are done, so add the method epilogue
            state.Instructions.AddRange(METHOD_EPILOGUE);

            //Now that we have the stack size, we must patch the prologue/epilogue with the size
            ((SPEEmulator.OpCodes.Bases.RI10)state.Instructions[1]).I10 = (~(state.MaxStackDepth * REGISTER_SIZE)) & 0x3ff;
            ((SPEEmulator.OpCodes.Bases.RI10)state.Instructions[2]).I10 = (~(state.MaxStackDepth * REGISTER_SIZE)) & 0x3ff;
            ((SPEEmulator.OpCodes.Bases.RI10)state.Instructions[state.Instructions.Count - 3]).I10 = state.MaxStackDepth * REGISTER_SIZE;

            //We can now patch all branches, we cannot patch the calls until the microkernel address is emitted
            state.EndFunction();

            return state;
        }


        private void RecursiveTranslate(CompiledMethod state, SPEOpCodeMapper mapper, IR.InstructionElement el)
        {
            foreach (IR.InstructionElement els in el.Childnodes)
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
