using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JITManager.IR
{
    /// <summary>
    /// Represents a marker instruction that indicates a method entry
    /// </summary>
    public class MethodEntry : InstructionElement
    {
        private Mono.Cecil.MethodDefinition m_mdef;

        public MethodEntry(Mono.Cecil.MethodDefinition mdef)
        {
            m_mdef = mdef;
        }

        public Mono.Cecil.MethodDefinition Method { get { return m_mdef; } }
    }
}
