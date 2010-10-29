using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPEJIT
{
    /// <summary>
    /// This class contains all required 
    /// </summary>
    public class CompiledMethod
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
        /// A list of calls, key is the call instruction offset, value is the invoked method
        /// </summary>
        private List<KeyValuePair<int, Mono.Cecil.MethodDefinition>> m_calls;

        /// <summary>
        /// A reference to the current accumulated list of instructions
        /// </summary>
        private List<SPEEmulator.OpCodes.Bases.Instruction> m_instructionList;

        /// <summary>
        /// The current depth of the stack
        /// </summary>
        private int m_stackDepth;

        /// <summary>
        /// The highest number of stack positions required
        /// </summary>
        private int m_maxStackDepth;

        /// <summary>
        /// The method that this compilation concerns
        /// </summary>
        private IR.MethodEntry m_method;

        internal IR.MethodEntry Method { get { return m_method; } }

        internal CompiledMethod(IR.MethodEntry method)
        {
            m_method = method;
            m_labels = new Dictionary<string, int>();
            m_instructionOffsets = new Dictionary<Mono.Cecil.Cil.Instruction, int>();
            m_branches = new List<KeyValuePair<int, Mono.Cecil.Cil.Instruction>>();
            m_calls = new List<KeyValuePair<int, Mono.Cecil.MethodDefinition>>();
            m_instructionList = new List<SPEEmulator.OpCodes.Bases.Instruction>();
        }

        public int StackDepth 
        { 
            get { return m_stackDepth; }
            set
            {
                m_stackDepth = value;
                m_maxStackDepth = Math.Max(m_maxStackDepth, m_stackDepth);
            }
        }

        public uint MaxStackDepth
        {
            get { return (uint)m_maxStackDepth; }
        }

        public List<SPEEmulator.OpCodes.Bases.Instruction> Instructions { get { return m_instructionList; } }

        public void RegisterLabel(string label, int offset)
        {
            m_labels.Add(label, offset);
        }

        public void RegisterCall(Mono.Cecil.MethodDefinition t)
        {
            m_calls.Add(new KeyValuePair<int, Mono.Cecil.MethodDefinition>(m_instructionList.Count, t));
        }

        public void StartInstruction(Mono.Cecil.Cil.Instruction instr)
        {
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
            PatchBranches();
        }


        private void PatchBranches()
        {
            foreach (KeyValuePair<int, Mono.Cecil.Cil.Instruction> branch in m_branches)
            {
                if (m_instructionList[branch.Key] is SPEEmulator.OpCodes.Bases.RI16)
                    ((SPEEmulator.OpCodes.Bases.RI16)m_instructionList[branch.Key]).I16 = ((uint)(m_instructionOffsets[branch.Value] - branch.Key)) & 0xffff;
                else
                    throw new Exception("Unexpected SPE instruction where a branch should have been?");
            }
        }

        public void PatchCalls(Dictionary<Mono.Cecil.MethodDefinition, int> methodOffsets, int callhandlerOffset)
        {
            foreach (KeyValuePair<int, Mono.Cecil.MethodDefinition> call in m_calls)
            {
                int callOffset = methodOffsets.ContainsKey(call.Value) ? methodOffsets[call.Value] : callhandlerOffset;

                if (m_instructionList[call.Key] is SPEEmulator.OpCodes.Bases.RI16)
                    ((SPEEmulator.OpCodes.Bases.RI16)m_instructionList[call.Key]).I16 = (uint)(callOffset & 0xffff);
                else
                    throw new Exception("Unexpected SPE instruction where a branch should have been?");
            }
                

        }
    }
}
