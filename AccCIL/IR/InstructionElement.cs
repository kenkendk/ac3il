using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;
using System.Reflection;


namespace AccCIL.IR
{
    /// <summary>
    /// Represents a single node in the instruction tree
    /// </summary>
    public class InstructionElement
    {
        protected InstructionElement() { }

        public InstructionElement(Mono.Cecil.MethodDefinition parentMethod, InstructionElement[] childnodes, Mono.Cecil.Cil.Instruction instruction)
        {
            this.Childnodes = childnodes ?? new InstructionElement[0];
            this.Instruction = instruction;
            this.ParentMethod = parentMethod;

            foreach (InstructionElement c in this.Childnodes)
            {
                System.Diagnostics.Trace.Assert(c != null);
                c.Parent = this;
            }

            if (instruction != null)
                AssignReturnType();
        }

        /// <summary>
        /// The input operands that this instruction operates on
        /// </summary>
        public InstructionElement[] Childnodes;
        /// <summary>
        /// The actual instruction wrapped by this IR.InstructionElement
        /// </summary>
        public Mono.Cecil.Cil.Instruction Instruction;

        /// <summary>
        /// The assigned virtual output register, null if there is no return value
        /// </summary>
        public VirtualRegister Register;

        /// <summary>
        /// The IR.InstructionElement that consumes the output of this operand
        /// </summary>
        public InstructionElement Parent;

        /// <summary>
        /// The method that this instruction is located in
        /// </summary>
        public Mono.Cecil.MethodDefinition ParentMethod;

        //TODO: Consider using the Mono.Cecil.TypeReference instead
        public Type ReturnType;
        public bool IsReturnTypeRef;

        public Type StorageClass
        {
            get
            {
                if (ReturnType == null)
                    return null;
                else if (IsReturnTypeRef)
                    return typeof(IntPtr);
                else if (ReturnType == typeof(bool) || ReturnType == typeof(int) || ReturnType == typeof(uint) || ReturnType == typeof(byte) || ReturnType == typeof(sbyte) || ReturnType == typeof(short) || ReturnType == typeof(ushort))
                    return typeof(int);
                else if (ReturnType == typeof(float))
                    return typeof(float);
                else if (ReturnType == typeof(double))
                    return typeof(double);
                else if (ReturnType == typeof(long) || ReturnType == typeof(ulong))
                    return typeof(long);
                else
                    throw new Exception("Unexpected value for storage: " + ReturnType.FullName);

            }
        }

        private void AssignReturnType()
        {
            IsReturnTypeRef = false;

            switch (Instruction.OpCode.StackBehaviourPush)
            {
                case Mono.Cecil.Cil.StackBehaviour.Push0:
                    ReturnType = null;
                    break;
                case Mono.Cecil.Cil.StackBehaviour.Push1:
                    switch (Instruction.OpCode.Code)
                    {
                        //These return the argument type
                        case Mono.Cecil.Cil.Code.Ldarg_0:
                            ReturnType = Type.GetType(ParentMethod.Parameters[0].ParameterType.FullName);
                            break;
                        case Mono.Cecil.Cil.Code.Ldarg_1:
                            ReturnType = Type.GetType(ParentMethod.Parameters[1].ParameterType.FullName);
                            break;
                        case Mono.Cecil.Cil.Code.Ldarg_2:
                            ReturnType = Type.GetType(ParentMethod.Parameters[2].ParameterType.FullName);
                            break;
                        case Mono.Cecil.Cil.Code.Ldarg_3:
                            ReturnType = Type.GetType(ParentMethod.Parameters[3].ParameterType.FullName);
                            break;
                        case Mono.Cecil.Cil.Code.Ldarg_S:
                        case Mono.Cecil.Cil.Code.Ldarg:
                            ReturnType = Type.GetType(ParentMethod.Parameters[((int)Instruction.Operand)].ParameterType.FullName);
                            break;

                        //These return the local variable type
                        case Mono.Cecil.Cil.Code.Ldloc_0:
                            ReturnType = Type.GetType(ParentMethod.Body.Variables[0].VariableType.FullName);
                            break;
                        case Mono.Cecil.Cil.Code.Ldloc_1:
                            ReturnType = Type.GetType(ParentMethod.Body.Variables[1].VariableType.FullName);
                            break;
                        case Mono.Cecil.Cil.Code.Ldloc_2:
                            ReturnType = Type.GetType(ParentMethod.Body.Variables[2].VariableType.FullName);
                            break;
                        case Mono.Cecil.Cil.Code.Ldloc_3:
                            ReturnType = Type.GetType(ParentMethod.Body.Variables[3].VariableType.FullName);
                            break;
                        case Mono.Cecil.Cil.Code.Ldloc_S:
                        case Mono.Cecil.Cil.Code.Ldloc:
                            ReturnType = Type.GetType(ParentMethod.Body.Variables[((Mono.Cecil.Cil.VariableReference)(Instruction.Operand)).Index].VariableType.FullName);
                            break;

                        //These return the same type as their source operands
                        case Mono.Cecil.Cil.Code.Add:
                        case Mono.Cecil.Cil.Code.Sub:
                        case Mono.Cecil.Cil.Code.Mul:
                        case Mono.Cecil.Cil.Code.Div:
                        case Mono.Cecil.Cil.Code.Rem:
                        case Mono.Cecil.Cil.Code.And:
                        case Mono.Cecil.Cil.Code.Or:
                        case Mono.Cecil.Cil.Code.Xor:
                        case Mono.Cecil.Cil.Code.Shl:
                        case Mono.Cecil.Cil.Code.Shr:
                        case Mono.Cecil.Cil.Code.Neg:
                        case Mono.Cecil.Cil.Code.Not:
                        case Mono.Cecil.Cil.Code.Div_Un:
                        case Mono.Cecil.Cil.Code.Rem_Un:
                        case Mono.Cecil.Cil.Code.Shr_Un:
                        case Mono.Cecil.Cil.Code.Add_Ovf:
                        case Mono.Cecil.Cil.Code.Add_Ovf_Un:
                        case Mono.Cecil.Cil.Code.Mul_Ovf:
                        case Mono.Cecil.Cil.Code.Mul_Ovf_Un:
                        case Mono.Cecil.Cil.Code.Sub_Ovf:
                        case Mono.Cecil.Cil.Code.Sub_Ovf_Un:
                            if (Childnodes[0].IsReturnTypeRef)
                                throw new Exception("Unexpected operands");
                            ReturnType = Childnodes[0].ReturnType;
                            break;

                        //These are not fixed yet
                        case Mono.Cecil.Cil.Code.Mkrefany:
                        case Mono.Cecil.Cil.Code.Unbox_Any:
                        case Mono.Cecil.Cil.Code.Ldelem_Any:
                        case Mono.Cecil.Cil.Code.Ldsfld:
                        case Mono.Cecil.Cil.Code.Ldfld:
                        case Mono.Cecil.Cil.Code.Ldobj:
                            throw new Exception("The opcode is not supported");

                        default:
                            throw new Exception("Unexpected Push1 operation");
                    }
                    break;
                case Mono.Cecil.Cil.StackBehaviour.Push1_push1:
                    //AFAIK, the only instruction that does this is the dup instruction
                    System.Diagnostics.Trace.Assert(Instruction.OpCode.Code == Mono.Cecil.Cil.Code.Dup);
                    ReturnType = Childnodes[0].ReturnType;
                    IsReturnTypeRef = Childnodes[0].IsReturnTypeRef;
                    break;
                case Mono.Cecil.Cil.StackBehaviour.Pushi:
                    //This can actually also be an adress
                    ReturnType = typeof(int);
                    break;
                case Mono.Cecil.Cil.StackBehaviour.Pushi8:
                    /*switch (instruction.OpCode.Code)
                    {
                        case Mono.Cecil.Cil.Code.Ldc_I8:
                        case Mono.Cecil.Cil.Code.Ldind_I8:
                        case Mono.Cecil.Cil.Code.Conv_I8:
                        case Mono.Cecil.Cil.Code.Conv_Ovf_I8:
                        case Mono.Cecil.Cil.Code.Conv_Ovf_I8_Un:
                        case Mono.Cecil.Cil.Code.Ldelem_I8:
                            ReturnType = typeof(long);
                            break;
                        case Mono.Cecil.Cil.Code.Conv_U8:
                        case Mono.Cecil.Cil.Code.Conv_Ovf_U8:
                        case Mono.Cecil.Cil.Code.Conv_Ovf_U8_Un:
                            ReturnType = typeof(ulong);
                            break;
                        default:
                            throw new Exception("Unexpected Pushi8 operation");
                    }*/
                    //According to ECMA spec: 
                    // Special instructions are used to interpret integers on the stack as though they were unsigned, rather than tagging the stack locations as being unsigned.
                    ReturnType = typeof(long);
                    break;
                case Mono.Cecil.Cil.StackBehaviour.Pushr4:
                    ReturnType = typeof(float);
                    break;
                case Mono.Cecil.Cil.StackBehaviour.Pushr8:
                    ReturnType = typeof(double);
                    break;
                case Mono.Cecil.Cil.StackBehaviour.Varpush:
                    //Instruction is supposed to be a call type function
                    System.Diagnostics.Trace.Assert(Instruction.OpCode.Code == Mono.Cecil.Cil.Code.Call || Instruction.OpCode.Code == Mono.Cecil.Cil.Code.Calli || Instruction.OpCode.Code == Mono.Cecil.Cil.Code.Callvirt);
                    Mono.Cecil.MethodReference mdef = ((Mono.Cecil.MethodReference)Instruction.Operand);
                    switch (mdef.ReturnType.ReturnType.FullName)
                    {
                        case "System.Void":
                            ReturnType = null;
                            break;
                        case "System.Byte":
                        case "System.SByte":
                        case "System.Boolean":
                        case "System.Int16":
                        case "System.Int32":
                        case "System.UInt16":
                        case "System.UInt32":
                            ReturnType = typeof(int);
                            break;
                        case "System.Int64":
                        case "System.UInt64":
                            ReturnType = typeof(long);
                            break;
                        case "System.Float":
                            ReturnType = typeof(float);
                            break;
                        case "System.Double":
                            ReturnType = typeof(double);
                            break;
                        default:
                            ReturnType = Type.GetType(mdef.ReturnType.ReturnType.FullName);
                            IsReturnTypeRef = true;
                            break;
                    }
                    break;
                case Mono.Cecil.Cil.StackBehaviour.Pushref:
                    IsReturnTypeRef = true;
                    switch (Instruction.OpCode.Code)
                    {
                        case Mono.Cecil.Cil.Code.Ldnull:
                            System.Diagnostics.Trace.Assert(true, "Figure out how to extract the class type");
                            ReturnType = null; //TODO: can we infer the type somehow?
                            break;
                        case Mono.Cecil.Cil.Code.Ldind_Ref:
                            System.Diagnostics.Trace.Assert(true, "Figure out how to extract the class type");
                            ReturnType = null; //TODO: How do we guess this?
                            break;
                        case Mono.Cecil.Cil.Code.Ldstr:
                            ReturnType = typeof(string);
                            break;
                        case Mono.Cecil.Cil.Code.Newobj:
                        case Mono.Cecil.Cil.Code.Castclass:
                            System.Diagnostics.Trace.Assert(true, "Figure out how to extract the class type");
                            ReturnType = ExtractClassType((Mono.Cecil.MemberReference)Instruction.Operand);
                            break;
                        case Mono.Cecil.Cil.Code.Box:
                            ReturnType = Childnodes[0].ReturnType;
                            break;
                        case Mono.Cecil.Cil.Code.Newarr:
                            System.Diagnostics.Trace.Assert(true, "Figure out how to extract the class type");
                            ReturnType = ExtractClassType((Mono.Cecil.MemberReference)Instruction.Operand);
                            break;
                        case Mono.Cecil.Cil.Code.Ldelem_Ref:
                            System.Diagnostics.Trace.Assert(true, "Figure out how to extract the class type");
                            ReturnType = (Type)Instruction.Operand;
                            break;
                        default:
                            throw new Exception("Unexpected Pushref operation");
                    }
                    break;
            }
        }

        private Type ExtractClassType(Mono.Cecil.MemberReference operand)
        {
            Type type = null;
            string typeName = operand.ToString();
            
            if (operand.DeclaringType != null)
                typeName = operand.DeclaringType.FullName.Replace('/', '+');            
            
            type = Type.GetType(typeName);

            if (type == null)
                type = Type.GetType(typeName + ", " + operand.DeclaringType.Module.Assembly.Name.ToString(), true);

            return type;
        }
    }
}
