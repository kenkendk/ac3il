using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccCIL.IR
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

        public IEnumerable<InstructionElement> FlatInstructionList { get { return from x in this.Childnodes.Descendants(c => c.Childnodes) select x; } }

        public void ResetVirtualRegisters()
        {
            foreach (IR.InstructionElement i in this.FlatInstructionList)
                switch (i.Instruction.OpCode.StackBehaviourPush)
                {
                    case Mono.Cecil.Cil.StackBehaviour.Push0:
                        break;
                    case Mono.Cecil.Cil.StackBehaviour.Varpush:
                        Mono.Cecil.MethodReference f = ((Mono.Cecil.MethodReference)i.Instruction.Operand);
                        if (f.ReturnType.ReturnType.FullName != "System.Void")
                            i.Register = new IR.VirtualRegister();
                        break;

                    //TODO: This one returns the same value twice, so it needs two registers
                    case Mono.Cecil.Cil.StackBehaviour.Push1_push1:
                        throw new Exception("Unsupported dup instruction");

                    case Mono.Cecil.Cil.StackBehaviour.Push1:
                    case Mono.Cecil.Cil.StackBehaviour.Pushi:
                    case Mono.Cecil.Cil.StackBehaviour.Pushi8:
                    case Mono.Cecil.Cil.StackBehaviour.Pushr4:
                    case Mono.Cecil.Cil.StackBehaviour.Pushr8:
                    case Mono.Cecil.Cil.StackBehaviour.Pushref:
                        i.Register = new IR.VirtualRegister();
                        break;

                    default:
                        throw new Exception("Unexpected stack push type: " + i.Instruction.OpCode.StackBehaviourPush);
                }
        }

    }
}
