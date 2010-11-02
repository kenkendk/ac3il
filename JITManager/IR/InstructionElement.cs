using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JITManager.IR
{
    /// <summary>
    /// Represents a single node in the instruction tree
    /// </summary>
    public class InstructionElement
    {
        public InstructionElement() : this(null, null) { }
        public InstructionElement(InstructionElement[] childnodes) : this(childnodes, null) { }

        public InstructionElement(InstructionElement[] childnodes, Mono.Cecil.Cil.Instruction instruction)
        {
            this.Childnodes = childnodes ?? new InstructionElement[0];
            this.Instruction = instruction;
        }

        public InstructionElement[] Childnodes;
        public Mono.Cecil.Cil.Instruction Instruction;
        public VirtualRegister[] Registers;
    }
}
