using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccCIL;
using AccCIL.IR;

namespace SPEJIT
{
    /// <summary>
    /// This class contains all required 
    /// </summary>
    public class CompiledMethod : ICompiledMethod
    {
        /// <summary>
        /// A list of known labels, the key is the label name and the value is the offset of the JIT'ed sequence
        /// </summary>
        private Dictionary<string, int> m_labels;

        /// <summary>
        /// A list of all source-to-target offsets, used to prevent filling the m_labels dictionary
        /// </summary>
        private Dictionary<Mono.Cecil.Cil.Instruction, int> m_instructionOffsets;

        /// <summary>
        /// A list of all known branch instructions, the key is the branch instruction offset, the value is the target instruction source offset
        /// </summary>
        private List<KeyValuePair<int, Mono.Cecil.Cil.Instruction>> m_branches;

        /// <summary>
        /// A list of all registered constant loads, key is the instruction offset, value is the constant
        /// </summary>
        private List<KeyValuePair<int, string>> m_constantLoads;

        /// <summary>
        /// A list of all registered literal strings, key is the instruction offset, value is the string
        /// </summary>
        private List<KeyValuePair<int, string>> m_stringLoads;

        /// <summary>
        /// A list of calls, key is the call instruction offset, value is the invoked method
        /// </summary>
        private List<KeyValuePair<int, Mono.Cecil.MethodReference>> m_calls;

        /// <summary>
        /// A reference to the current accumulated list of instructions
        /// </summary>
        private List<SPEEmulator.OpCodes.Bases.Instruction> m_instructionList;

        /// <summary>
        /// The current stack
        /// </summary>
        private Stack<VirtualRegister> m_stack;

        /// <summary>
        /// The depth of the current stack, disregarding register assigned variables
        /// </summary>
        private int m_stackDepth;

        /// <summary>
        /// The highest number of stack positions required
        /// </summary>
        private int m_maxStackDepth;

        /// <summary>
        /// The method that this compilation concerns
        /// </summary>
        private MethodEntry m_method;

        /// <summary>
        /// The number of instructions before the return code
        /// </summary>
        private int m_returnOffset;

        /// <summary>
        /// The method that this compilation belongs to
        /// </summary>
        public MethodEntry Method { get { return m_method; } }

        /// <summary>
        /// A cached list of the builtin SPE functions
        /// </summary>
        internal static IDictionary<string, Mono.Cecil.MethodDefinition> m_spe_builtins = null;

        /// <summary>
        /// A cached list of the builtin PPE functions
        /// </summary>
        internal static IDictionary<string, Mono.Cecil.MethodDefinition> m_ppe_builtins = null;

        /// <summary>
        /// Static initializer, used to load the builtins
        /// </summary>
        static CompiledMethod()
        {
            m_spe_builtins = new Dictionary<string, Mono.Cecil.MethodDefinition>();
            m_ppe_builtins = new Dictionary<string, Mono.Cecil.MethodDefinition>();

            foreach (Mono.Cecil.MethodDefinition mdef in Mono.Cecil.AssemblyFactory.GetAssembly(System.Reflection.Assembly.GetExecutingAssembly().Location).MainModule.Types["SPEJIT.BuiltInSPEMethods"].Methods)
                m_spe_builtins.Add(mdef.Name, mdef);
        
            foreach (Mono.Cecil.MethodDefinition mdef in Mono.Cecil.AssemblyFactory.GetAssembly(System.Reflection.Assembly.GetExecutingAssembly().Location).MainModule.Types["SPEJIT.BuiltInPPEMethods"].Methods)
                m_ppe_builtins.Add(mdef.Name, mdef);

        }


        internal CompiledMethod(MethodEntry method)
        {
            m_method = method;
            m_labels = new Dictionary<string, int>();
            m_instructionOffsets = new Dictionary<Mono.Cecil.Cil.Instruction, int>();
            m_branches = new List<KeyValuePair<int, Mono.Cecil.Cil.Instruction>>();
            m_calls = new List<KeyValuePair<int, Mono.Cecil.MethodReference>>();
            m_instructionList = new List<SPEEmulator.OpCodes.Bases.Instruction>();
            m_constantLoads = new List<KeyValuePair<int, string>>();
            m_stringLoads = new List<KeyValuePair<int, string>>();
            m_stack = new Stack<VirtualRegister>();
        }

        public List<Mono.Cecil.MethodReference> CalledMethods
        {
            get
            {
                List<Mono.Cecil.MethodReference> res = new List<Mono.Cecil.MethodReference>();
                foreach (KeyValuePair<int, Mono.Cecil.MethodReference> k in m_calls)
                    res.Add(k.Value);
                return res;
            }
        }

        public VirtualRegister PopStack()
        {
            VirtualRegister r = m_stack.Pop();
            if (r is TemporaryRegister || r.RegisterNumber < 0)
                m_stackDepth--;
            return r;
        }

        public void PushStack(VirtualRegister r)
        {
            m_stack.Push(r);
            if (r is TemporaryRegister || r.RegisterNumber < 0)
                m_stackDepth++;
            m_maxStackDepth = Math.Max(m_maxStackDepth, m_stackDepth);
        }

        /// <summary>
        /// Gets the execution size of the stack, that is without any register optimized stack elements
        /// </summary>
        public int StackDepth { get { return m_stackDepth; } }

        /// <summary>
        /// Gets the virtual size of the stack, that is the stack size as the VM sees it
        /// </summary>
        public int VirtualStackDepth { get { return m_stack.Count; } }

        public uint MaxStackDepth
        {
            get { return (uint)m_maxStackDepth; }
        }

        public List<SPEEmulator.OpCodes.Bases.Instruction> Instructions { get { return m_instructionList; } }

        public void RegisterLabel(string label, int offset)
        {
            m_labels.Add(label, offset);
        }

        public void RegisterCall(Mono.Cecil.MethodReference t)
        {
            m_calls.Add(new KeyValuePair<int, Mono.Cecil.MethodReference>(m_instructionList.Count, t));
        }

        public void StartInstruction(Mono.Cecil.Cil.Instruction instr)
        {
            if (!m_instructionOffsets.ContainsKey(instr))
                m_instructionOffsets.Add(instr, m_instructionList.Count);
        }

        public void EndInstruction()
        {
        }

        public void RegisterBranch(Mono.Cecil.Cil.Instruction target)
        {
            m_branches.Add(new KeyValuePair<int, Mono.Cecil.Cil.Instruction>(m_instructionList.Count, target));
        }

        public void StartFunction()
        {
        }

        public void EndFunction()
        {
            m_returnOffset = m_instructionList.Count;
        }

        public List<SPEEmulator.OpCodes.Bases.Instruction> Prolouge;
        public List<SPEEmulator.OpCodes.Bases.Instruction> Epilouge;

        public void RegisterConstantLoad(string constant)
        {
            m_constantLoads.Add(new KeyValuePair<int, string>(m_instructionList.Count, constant));
        }

        public void RegisterConstantLoad(ulong high, ulong low)
        {
            RegisterConstantLoad(string.Format("{0:x16}{1:x16}", high, low));
        }

        public void RegisterStringLoad(string value)
        {
            m_stringLoads.Add(new KeyValuePair<int, string>(m_instructionList.Count, value));
        }

        public IEnumerable<string> Constants 
        { 
            get 
            {
                return m_constantLoads.Select(x => x.Value).Distinct().ToList();
            } 
        }

        public void PatchConstants(Dictionary<string, int> offsets)
        {
            foreach (KeyValuePair<int, string> ko in m_constantLoads)
            {
                if (m_instructionList[ko.Key] is SPEEmulator.OpCodes.lqr)
                    ((SPEEmulator.OpCodes.lqr)m_instructionList[ko.Key]).I16 = (uint)(offsets[ko.Value] - ko.Key) & 0xffff;
                else
                    throw new Exception("Unexpected SPE instruction where a constant load should have been?");
            }
        }

        public void PatchBranches()
        {
            foreach (KeyValuePair<int, Mono.Cecil.Cil.Instruction> branch in m_branches)
            {
                int targetInstruction = branch.Value == null ? m_returnOffset : m_instructionOffsets[branch.Value];

                if (m_instructionList[branch.Key] is SPEEmulator.OpCodes.Bases.RI16)
                    ((SPEEmulator.OpCodes.Bases.RI16)m_instructionList[branch.Key]).I16 = ((uint)(targetInstruction - branch.Key)) & 0xffff;
                else
                    throw new Exception("Unexpected SPE instruction where a branch should have been?");
            }
        }

        public void PatchCalls(Dictionary<Mono.Cecil.MethodReference, int> methodOffsets, int callhandlerOffset, Dictionary<int, Mono.Cecil.MethodReference> callpoints)
        {
            foreach (KeyValuePair<int, Mono.Cecil.MethodReference> call in m_calls)
            {
                int callOffset = methodOffsets.ContainsKey(call.Value) ? methodOffsets[call.Value] : callhandlerOffset;
                int ownOffset = methodOffsets[this.Method.Method] + Prolouge.Count;

                if (m_instructionList[call.Key] is SPEEmulator.OpCodes.brasl)
                    ((SPEEmulator.OpCodes.brasl)m_instructionList[call.Key]).I16 = (uint)(callOffset);
                else
                    throw new Exception("Unexpected SPE instruction where a branch should have been?");

                if (m_instructionList[call.Key - 1] is SPEEmulator.OpCodes.il)
                {
                    ((SPEEmulator.OpCodes.il)m_instructionList[call.Key - 1]).I16 = (uint)call.Value.Parameters.Count;
                    if (((SPEEmulator.OpCodes.il)m_instructionList[call.Key - 1]).RT != SPEJITCompiler._TMP4)
                        throw new Exception("SPE branch instruction for call was missing the previous parameter count instruction?");
                }
                else
                    throw new Exception("SPE branch instruction for call was missing the previous parameter count instruction?");

                int callposition = call.Key + ownOffset;
                callpoints.Add(callposition, call.Value);
            }
        }

        /// <summary>
        /// Returns the total number of instructions generated by this method
        /// </summary>
        public int InstructionCount { get { return Prolouge.Count + m_instructionList.Count + Epilouge.Count; } }
        /// <summary>
        /// Gets the size of the emitted code in bytes, including any constants
        /// </summary>
        public int CodeSize { get { return (InstructionCount + (Constants.Count() * 4)) * 4; } }
        /// <summary>
        /// Gets the total size, in bytes, of this function, including namestring, constants and padding
        /// </summary>
        public int TotalSize 
        { 
            get 
            { 
                int realSize = CodeSize;
                int padding = realSize % 16 > 0 ? 16 - (realSize % 16) : 0;

                return realSize + padding;
            } 
        }

        internal void RegisterReturn()
        {
            m_branches.Add(new KeyValuePair<int, Mono.Cecil.Cil.Instruction>(m_instructionList.Count, null));
        }

        /// <summary>
        /// Gets a list of instruction offsets
        /// </summary>
        public IDictionary<Mono.Cecil.Cil.Instruction, int> InstructionOffsets { get { return m_instructionOffsets; } }

        public string Fullname { get { return this.Method.Method.DeclaringType.FullName + "." + this.Method.Method.Name; } }

        public IEnumerable<string> StringLiterals 
        { 
            get 
            { 
                List<string> r = m_stringLoads.Select(c => c.Value).ToList();
                r.Add(this.Fullname);
                return r.Distinct();
            } 
        }

        internal void PatchStringLoads(Dictionary<string, uint> stringLiteralRefs)
        {
            foreach (KeyValuePair<int, string> c in m_stringLoads)
            {
                if (m_instructionList[c.Key] is SPEEmulator.OpCodes.il)
                    ((SPEEmulator.OpCodes.il)m_instructionList[c.Key]).I16 = stringLiteralRefs[c.Value];
                else
                    throw new Exception("SPE string ref had invalid instruction");
            }
        }
    }
}
