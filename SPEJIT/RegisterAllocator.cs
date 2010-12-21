using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPEJIT
{
    /// <summary>
    /// A register allocator wrapper that ensures that function calls use the ABI specified registers
    /// </summary>
    public class RegisterAllocator
    {
        private static int ABI_RETURN_REGISTER = 3;
        private static int ABI_FUNCTION_PARAMETER_REGISTER_START = 3;

        public List<int> AllocateRegisters(int extraRegisters, AccCIL.IRegisterAllocator allocator, AccCIL.IR.MethodEntry method)
        {
            List<int> usedRegisters = new List<int>();
            AssignFixedRegisters(usedRegisters, method);

            if (allocator != null)
            {
                Stack<int> registers = new Stack<int>(Enumerable.Range(extraRegisters, 128 - extraRegisters));
                usedRegisters.AddRange(allocator.AllocateRegisters(registers, method));
            }

            return usedRegisters.Distinct().ToList();
        }

        private void AssignRegister(AccCIL.IR.InstructionElement el, int register, List<int> usedRegisters, bool assignparent)
        {
            if (el == null || el.Register == null)
                return;
            
            el.Register.RegisterNumber = register;
            usedRegisters.Add(register);

            if (assignparent)
            {
                //Pass the assignment up
                while (el.Parent != null && el.Parent.Register != null && el.Parent.Register.RegisterNumber < 0)
                {
                    el.Parent.Register.RegisterNumber = register;
                    el = el.Parent;
                }
            }
        }

        private void AssignFixedRegisters(List<int> usedRegisters, AccCIL.IR.MethodEntry method)
        {
            foreach (var i in method.FlatInstructionList)
            {
                switch (i.Instruction.OpCode.Code)
                {
                    case Mono.Cecil.Cil.Code.Ret:
                        AssignRegister(i, ABI_RETURN_REGISTER, usedRegisters, true);
                        break;
                    case Mono.Cecil.Cil.Code.Call:
                    case Mono.Cecil.Cil.Code.Calli:
                    case Mono.Cecil.Cil.Code.Callvirt:
                        AssignRegister(i, ABI_RETURN_REGISTER, usedRegisters, true);
                        
                        //TODO: Add spill limit for arguments
                        for (int j = 0; j < i.Childnodes.Length; j++)
                            AssignRegister(i.Childnodes[j], ABI_FUNCTION_PARAMETER_REGISTER_START + j, usedRegisters, true);
                        break;

                    case Mono.Cecil.Cil.Code.Stloc_0:
                        AssignRegister(i.Childnodes[0], SPEJITCompiler._LV0, usedRegisters, true);
                        break;
                    case Mono.Cecil.Cil.Code.Stloc_1:
                        AssignRegister(i.Childnodes[0], SPEJITCompiler._LV0 + 1, usedRegisters, true);
                        break;
                    case Mono.Cecil.Cil.Code.Stloc_2:
                        AssignRegister(i.Childnodes[0], SPEJITCompiler._LV0 + 2, usedRegisters, true);
                        break;
                    case Mono.Cecil.Cil.Code.Stloc_3:
                        AssignRegister(i.Childnodes[0], SPEJITCompiler._LV0 + 3, usedRegisters, true);
                        break;
                    case Mono.Cecil.Cil.Code.Stloc_S:
                        AssignRegister(i.Childnodes[0], SPEJITCompiler._LV0 + ((Mono.Cecil.Cil.VariableReference)i.Instruction.Operand).Index, usedRegisters, true);
                        break;

                    case Mono.Cecil.Cil.Code.Ldloc_0:
                        AssignRegister(i, SPEJITCompiler._LV0, usedRegisters, false);
                        break;
                    case Mono.Cecil.Cil.Code.Ldloc_1:
                        AssignRegister(i, SPEJITCompiler._LV0 + 1, usedRegisters, false);
                        break;
                    case Mono.Cecil.Cil.Code.Ldloc_2:
                        AssignRegister(i, SPEJITCompiler._LV0 + 2, usedRegisters, false);
                        break;
                    case Mono.Cecil.Cil.Code.Ldloc_3:
                        AssignRegister(i, SPEJITCompiler._LV0 + 3, usedRegisters, false);
                        break;
                    case Mono.Cecil.Cil.Code.Ldloc_S:
                        AssignRegister(i, SPEJITCompiler._LV0 + ((Mono.Cecil.Cil.VariableReference)i.Instruction.Operand).Index, usedRegisters, false);
                        break;
                    case Mono.Cecil.Cil.Code.Ldarg_0:
                        AssignRegister(i, SPEJITCompiler._LV0 + method.Method.Body.Variables.Count, usedRegisters, false);
                        break;
                    case Mono.Cecil.Cil.Code.Ldarg_1:
                        AssignRegister(i, SPEJITCompiler._LV0 + method.Method.Body.Variables.Count + 1, usedRegisters, false);
                        break;
                    case Mono.Cecil.Cil.Code.Ldarg_2:
                        AssignRegister(i, SPEJITCompiler._LV0 + method.Method.Body.Variables.Count + 2, usedRegisters, false);
                        break;
                    case Mono.Cecil.Cil.Code.Ldarg_3:
                        AssignRegister(i, SPEJITCompiler._LV0 + method.Method.Body.Variables.Count + 3, usedRegisters, false);
                        break;
                    case Mono.Cecil.Cil.Code.Ldarg_S:
                    case Mono.Cecil.Cil.Code.Ldarg:
                        AssignRegister(i, SPEJITCompiler._LV0 + method.Method.Body.Variables.Count + method.Method.Parameters.IndexOf(((Mono.Cecil.ParameterDefinition)i.Instruction.Operand)), usedRegisters, false);
                        break;
                }
            }
        }
    }
}
