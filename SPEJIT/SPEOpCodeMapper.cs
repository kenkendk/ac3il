using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccCIL;
using AccCIL.IR;

namespace SPEJIT
{
    internal class SPEOpCodeMapper
    {
        public const uint _SP = SPEJITCompiler._SP;
        public const uint _LR = SPEJITCompiler._LR;
        public const uint _LV0 = SPEJITCompiler._LV0;

        //ABI specification states that $75 to $79 are scratch registers
        public readonly TemporaryRegister _VTMP0 = new TemporaryRegister(SPEJITCompiler._TMP0);
        public readonly TemporaryRegister _VTMP1 = new TemporaryRegister(SPEJITCompiler._TMP1);
        public readonly TemporaryRegister _VTMP2 = new TemporaryRegister(SPEJITCompiler._TMP2);
        public readonly TemporaryRegister _VTMP3 = new TemporaryRegister(SPEJITCompiler._TMP3);
        public readonly TemporaryRegister _VTMP4 = new TemporaryRegister(SPEJITCompiler._TMP4);

        public const uint _RTMP0 = SPEJITCompiler._TMP0;
        public const uint _RTMP1 = SPEJITCompiler._TMP1;
        public const uint _RTMP2 = SPEJITCompiler._TMP2;
        public const uint _RTMP3 = SPEJITCompiler._TMP3;
        public const uint _RTMP4 = SPEJITCompiler._TMP4;

        /// <summary>
        /// The inverse value of register size, used in add operations
        /// </summary>
        public const uint REGISTER_SIZE_NEGATED = (-REGISTER_SIZE & 0x3ff);

        /// <summary>
        /// The register used for the first argument
        /// </summary>
        public const uint _ARG0 = SPEJITCompiler._ARG0;

        /// <summary>
        /// The max allowed number of function arguments
        /// </summary>
        public const int MAX_FUCTION_ARGUMENTS = 74 - 3;

        /// <summary>
        /// The size of a register in bytes when placed on stack
        /// </summary>
        public const int REGISTER_SIZE = SPEJITCompiler.REGISTER_SIZE;

        /// <summary>
        /// Emits instructions for popping a value from the stack
        /// </summary>
        /// <param name="targetRegister">The register into which the value is written</param>
        public VirtualRegister PopStack(uint targetRegister)
        {
            VirtualRegister r = m_state.PopStack();
            if (r.RegisterNumber < 0 || r is TemporaryRegister)
            {
                if (m_state.StackDepth + 1 > 511)
                    throw new Exception("Too deep stack");

                m_state.Instructions.Add(new SPEEmulator.OpCodes.lqd((uint)targetRegister, _SP, (uint)m_state.StackDepth + 2));

                if (r.RegisterNumber < 0)
                    return new VirtualRegister(targetRegister);
                else
                    return new TemporaryRegister(targetRegister);
            }
            else
                return r;
        }

        public VirtualRegister PopStack(uint targetRegister, bool move)
        {
            VirtualRegister r = PopStack(targetRegister);
            if (r.RegisterNumber != targetRegister)
                m_state.Instructions.Add(new SPEEmulator.OpCodes.ori(targetRegister, (uint)r.RegisterNumber, 0));

            return r;
        }

        /// <summary>
        /// Emits instructions for pushing a value onto the stack
        /// </summary>
        /// <param name="sourceRegister"></param>
        public void PushStack(VirtualRegister sourceRegister)
        {
            m_state.PushStack(sourceRegister);
            if (sourceRegister is TemporaryRegister || sourceRegister.RegisterNumber < 0)
            {
                if (m_state.StackDepth > 511)
                    throw new Exception("Too deep stack");

                m_state.Instructions.Add(new SPEEmulator.OpCodes.stqd((uint)sourceRegister.RegisterNumber, _SP, (uint)m_state.StackDepth + 1));
            }
        }

        /// <summary>
        /// Common instruction for clearing the value of a register
        /// </summary>
        /// <param name="register">The register to clear</param>
        public void ClearRegister(uint register)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.xor(register, register, register));
        }


        /// <summary>
        /// Common instruction for copying the contents of one register into another
        /// </summary>
        /// <param name="sourceRegister">The register to copy from</param>
        /// <param name="targetRegister">The register to copy to</param>
        public void CopyRegister(uint sourceRegister, uint targetRegister)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.ori(targetRegister, sourceRegister, 0));
        }

        /// <summary>
        /// The JIT state manager
        /// </summary>
        private CompiledMethod m_state;

        public SPEOpCodeMapper(CompiledMethod state)
        {
            m_state = state;
        }

        public void nop(InstructionElement el)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.nop(0));
        }


        public void Ldarg_0(InstructionElement el)
        {
            PushStack(new VirtualRegister((uint)(_LV0 + m_state.Method.Method.Body.Variables.Count)));
        }

        public void Ldarg_1(InstructionElement el)
        {
            PushStack(new VirtualRegister((uint)(_LV0 + m_state.Method.Method.Body.Variables.Count + 1)));
        }

        public void Ldarg_2(InstructionElement el)
        {
            PushStack(new VirtualRegister((uint)(_LV0 + m_state.Method.Method.Body.Variables.Count + 2)));
        }

        public void Ldarg_3(InstructionElement el)
        {
            PushStack(new VirtualRegister((uint)(_LV0 + m_state.Method.Method.Body.Variables.Count + 3)));
        }

        public void Ldarg_s(InstructionElement el)
        {
            int index = el.ParentMethod.Parameters.IndexOf(((Mono.Cecil.ParameterDefinition)el.Instruction.Operand));
            PushStack(new VirtualRegister((uint)(_LV0 + m_state.Method.Method.Body.Variables.Count + index)));
        }

        public void Ldarg(InstructionElement el)
        {
            int index = el.ParentMethod.Parameters.IndexOf(((Mono.Cecil.ParameterDefinition)el.Instruction.Operand));
            PushStack(new VirtualRegister((uint)(_LV0 + m_state.Method.Method.Body.Variables.Count + index)));
        }

        private void Starg_common(InstructionElement el)
        {
            int index = el.ParentMethod.Parameters.IndexOf(((Mono.Cecil.ParameterDefinition)el.Instruction.Operand));
            uint regNo = (uint)(_LV0 + m_state.Method.Method.Body.Variables.Count + index);
            PopStack(regNo, true);
        }

        public void Starg_s(InstructionElement el)
        {
            Starg_common(el);
        }

        public void Starg(InstructionElement el)
        {
            Starg_common(el);
        }

        public void Ldc_I4_0(InstructionElement el)
        {
            VirtualRegister r = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il((uint)r.RegisterNumber, 0));
            PushStack(r);
        }

        public void Ldc_I4_1(InstructionElement el)
        {
            VirtualRegister r = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il((uint)r.RegisterNumber, 1));
            PushStack(r);
        }

        public void Ldc_I4_2(InstructionElement el)
        {
            VirtualRegister r = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il((uint)r.RegisterNumber, 2));
            PushStack(r);
        }

        public void Ldc_I4_3(InstructionElement el)
        {
            VirtualRegister r = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il((uint)r.RegisterNumber, 3));
            PushStack(r);
        }

        public void Ldc_I4_4(InstructionElement el)
        {
            VirtualRegister r = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il((uint)r.RegisterNumber, 4));
            PushStack(r);
        }

        public void Ldc_I4_5(InstructionElement el)
        {
            VirtualRegister r = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il((uint)r.RegisterNumber, 5));
            PushStack(r);
        }

        public void Ldc_I4_6(InstructionElement el)
        {
            VirtualRegister r = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il((uint)r.RegisterNumber, 6));
            PushStack(r);
        }

        public void Ldc_I4_7(InstructionElement el)
        {
            VirtualRegister r = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il((uint)r.RegisterNumber, 7));
            PushStack(r);
        }

        public void Ldc_I4_8(InstructionElement el)
        {
            VirtualRegister r = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il((uint)r.RegisterNumber, 8));
            PushStack(r);
        }

        public void Ldc_i4_m1(InstructionElement el)
        {
            VirtualRegister r = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il((uint)r.RegisterNumber, 0xffff));
            PushStack(r);
        }

        public void Ldc_I4(InstructionElement el)
        {
            VirtualRegister r = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;
            uint reg = (uint)r.RegisterNumber;

            //TODO: Negative values can be loaded more efficiently, if they are < 0xffff
            uint opr = (uint)(int)el.Instruction.Operand;
            if (opr < 0x7fff)
                m_state.Instructions.Add(new SPEEmulator.OpCodes.il(reg, (uint)opr));
            else if (opr < 0x40000)
                m_state.Instructions.Add(new SPEEmulator.OpCodes.ila(reg, (uint)opr));
            else
            {
                ulong value = (ulong)((int) opr);
                m_state.RegisterConstantLoad((value << 32) | (value & 0xffffffff), (value << 32) | (value & 0xffffffff));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.lqr(reg, 0));
            }
            PushStack(r);
        }

        public void Ldc_I8(InstructionElement el)
        {
            VirtualRegister r = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;
            uint reg = (uint)r.RegisterNumber;

            ulong opr = (ulong)(long)el.Instruction.Operand;
            m_state.RegisterConstantLoad((ulong)opr, opr);
            m_state.Instructions.Add(new SPEEmulator.OpCodes.lqr(reg, 0));
            PushStack(r);
        }

        public void Ldc_I4_S(InstructionElement el)
        {
            VirtualRegister r = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;
            uint reg = (uint)r.RegisterNumber;

            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(reg, ((uint)(sbyte)el.Instruction.Operand) & 0xffff));
            PushStack(r);
        }

        public void Ldc_r4(InstructionElement el)
        {
            VirtualRegister r = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;
            float opr = (float)el.Instruction.Operand;
            byte[] data = BitConverter.GetBytes(opr);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(data);
            m_state.RegisterConstantLoad(string.Format("{0:x2}{1:x2}{2:x2}{3:x2}{0:x2}{1:x2}{2:x2}{3:x2}{0:x2}{1:x2}{2:x2}{3:x2}{0:x2}{1:x2}{2:x2}{3:x2}", data[0], data[1], data[2], data[3]));
            m_state.Instructions.Add(new SPEEmulator.OpCodes.lqr((uint)r.RegisterNumber, 0));
            PushStack(r);
        }

        public void Ldc_r8(InstructionElement el)
        {
            VirtualRegister r = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;
            double opr = (double)el.Instruction.Operand;
            byte[] data = BitConverter.GetBytes(opr);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(data);
            m_state.RegisterConstantLoad(string.Format("{0:x2}{1:x2}{2:x2}{3:x2}{4:x2}{5:x2}{6:x2}{7:x2}{0:x2}{1:x2}{2:x2}{3:x2}{4:x2}{5:x2}{6:x2}{7:x2}", data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7]));
            m_state.Instructions.Add(new SPEEmulator.OpCodes.lqr((uint)r.RegisterNumber, 0));
            PushStack(r);
        }

        public void Conv_R4(InstructionElement el)
        {
            if (el.Childnodes == null || el.Childnodes.Length != 1)
                throw new InvalidProgramException();
            if (el.Childnodes[0].StorageClass == typeof(float))
                return;
            else if (el.Childnodes[0].StorageClass == typeof(int))
            {
                VirtualRegister input = PopStack(_RTMP0);
                VirtualRegister output = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;

                m_state.Instructions.Add(new SPEEmulator.OpCodes.csflt((uint)output.RegisterNumber, (uint)input.RegisterNumber, 0));
                PushStack(output);
            }
            else if (el.Childnodes[0].StorageClass == typeof(long))
            {
                throw new MissingMethodException();
            }
            else if (el.Childnodes[0].StorageClass == typeof(double))
            {
                VirtualRegister input = PopStack(_RTMP0);
                VirtualRegister output = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;

                m_state.Instructions.Add(new SPEEmulator.OpCodes.frds((uint)output.RegisterNumber, (uint)input.RegisterNumber));
                PushStack(output);
            }
            else
                throw new InvalidProgramException();
        }


        public void Conv_R8(InstructionElement el)
        {
            if (el.Childnodes == null || el.Childnodes.Length != 1)
                throw new InvalidProgramException();
            if (el.Childnodes[0].StorageClass == typeof(double))
                return;
            else if (el.Childnodes[0].StorageClass == typeof(int))
            {
                VirtualRegister input = PopStack(_RTMP0);
                VirtualRegister output = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;

                m_state.Instructions.Add(new SPEEmulator.OpCodes.csflt((uint)output.RegisterNumber, (uint)input.RegisterNumber, 0));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.fesd((uint)output.RegisterNumber, (uint)output.RegisterNumber));
                PushStack(output);
            }
            else if (el.Childnodes[0].StorageClass == typeof(long))
            {
                throw new MissingMethodException();
            }
            else if (el.Childnodes[0].StorageClass == typeof(float))
            {
                VirtualRegister input = PopStack(_RTMP0);
                VirtualRegister output = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;

                m_state.Instructions.Add(new SPEEmulator.OpCodes.fesd((uint)output.RegisterNumber, (uint)input.RegisterNumber));
                PushStack(output);
            }
            else
                throw new InvalidProgramException();
        }


        public void Conv_I8(InstructionElement el)
        {
            if (el.Childnodes == null || el.Childnodes.Length != 1)
                throw new InvalidProgramException();
            if (el.Childnodes[0].StorageClass == typeof(long))
                return;
            else if (el.Childnodes[0].StorageClass == typeof(int))
            {
                VirtualRegister input = PopStack(_RTMP0);
                VirtualRegister output = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;

                m_state.Instructions.Add(new SPEEmulator.OpCodes.xswd((uint)output.RegisterNumber, (uint)input.RegisterNumber));
                PushStack(output);
            }
            else if (el.Childnodes[0].StorageClass == typeof(float))
            {
                throw new MissingMethodException();
            }
            else if (el.Childnodes[0].StorageClass == typeof(double))
            {
                throw new MissingMethodException();
            }
            else
                throw new InvalidProgramException();
        }

        public void Conv_I4(InstructionElement el)
        {
            if (el.Childnodes == null || el.Childnodes.Length != 1)
                throw new InvalidProgramException();
            if (el.Childnodes[0].StorageClass == typeof(int))
                return;
            else if (el.Childnodes[0].StorageClass == typeof(long))
            {
                VirtualRegister input = PopStack(_RTMP0);
                VirtualRegister output = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;

                m_state.RegisterConstantLoad(0x0405060704050607, 0x0405060704050607); //This mask loads the lower word from the prefered doubleword slot
                m_state.Instructions.Add(new SPEEmulator.OpCodes.lqr(_RTMP1, 0));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.shufb((uint)output.RegisterNumber, (uint)input.RegisterNumber, (uint)input.RegisterNumber, _RTMP1));
                PushStack(output);
            }
            else if (el.Childnodes[0].StorageClass == typeof(float))
            {
                VirtualRegister input = PopStack(_RTMP0);
                VirtualRegister output = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;

                m_state.Instructions.Add(new SPEEmulator.OpCodes.cflts((uint)output.RegisterNumber, (uint)input.RegisterNumber, 0));
                PushStack(output);
            }
            else if (el.Childnodes[0].StorageClass == typeof(double))
            {
                VirtualRegister input = PopStack(_RTMP0);
                VirtualRegister output = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;

                m_state.Instructions.Add(new SPEEmulator.OpCodes.frds((uint)output.RegisterNumber, (uint)input.RegisterNumber)); //Convert to float
                m_state.Instructions.Add(new SPEEmulator.OpCodes.cflts((uint)output.RegisterNumber, (uint)output.RegisterNumber, 0)); //Convert to int
                PushStack(output);
            }
        }

        public void Conv_I1(InstructionElement el)
        {
            Conv_I4(el);
        }

        public void Conv_U1(InstructionElement el)
        {
            Conv_I1(el);
        }

        public void Conv_I2(InstructionElement el)
        {
            Conv_I4(el);
        }

        public void Conv_U2(InstructionElement el)
        {
            Conv_I2(el);
        }

        public void Conv_U4(InstructionElement el)
        {
            Conv_I4(el);
        }

        public void Conv_U(InstructionElement el)
        {
            Conv_I4(el);
        }

        public void Conv_U8(InstructionElement el)
        {
            if (el.Childnodes == null || el.Childnodes.Length != 1)
                throw new InvalidProgramException();
            if (el.Childnodes[0].StorageClass == typeof(long))
                return;
            else if (el.Childnodes[0].StorageClass == typeof(int))
            {
                VirtualRegister input = PopStack(_RTMP0);
                VirtualRegister output = el.Register.RegisterNumber < 0 ? _VTMP0 : el.Register;

                //According to ECMA specs i4 -> u8 is always zero extended: http://jilc.sourceforge.net/ecma_p3_cil.shtml#Table7ConversionOperations
                m_state.RegisterConstantLoad(0x8080808000010203, 0x8080808000010203);
                m_state.Instructions.Add(new SPEEmulator.OpCodes.lqr(_RTMP1, 0));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.shufb((uint)output.RegisterNumber, (uint)input.RegisterNumber, (uint)input.RegisterNumber, _RTMP1));
                PushStack(output);
            }
            else if (el.Childnodes[0].StorageClass == typeof(float))
            {
                throw new MissingMethodException();
            }
            else if (el.Childnodes[0].StorageClass == typeof(double))
            {
                throw new MissingMethodException();
            }
            else
                throw new InvalidProgramException();
        }

        private Type ValidateBinaryOp(InstructionElement el, bool allowPointers)
        {
            if (el.Childnodes == null || el.Childnodes.Length != 2)
                throw new InvalidProgramException();
            if (el.Childnodes[0].StorageClass != el.Childnodes[1].StorageClass)
                throw new InvalidProgramException("TODO: Type cohersion?");

            if (el.Childnodes[0].StorageClass == typeof(int))
                return typeof(int);
            else if (el.Childnodes[0].StorageClass == typeof(long))
                return typeof(long);
            else if (el.Childnodes[0].StorageClass == typeof(float))
                return typeof(float);
            else if (el.Childnodes[0].StorageClass == typeof(double))
                return typeof(double);
            else if (el.Childnodes[0].StorageClass == typeof(IntPtr) && allowPointers)
                return typeof(IntPtr);
            else
                throw new InvalidProgramException("Binary Op for <" + el.Childnodes[0].StorageClass + "> ?");
        }

        public void Not(InstructionElement el)
        {
            VirtualRegister r = PopStack(_RTMP0);
            m_state.Instructions.Add(new SPEEmulator.OpCodes.xori((uint)r.RegisterNumber, (uint)r.RegisterNumber, 0x3ff));
            PushStack(r);
        }

        public void Clt(InstructionElement el)
        {
            Type t = ValidateBinaryOp(el, false);

            VirtualRegister r1 = PopStack(_RTMP1);
            VirtualRegister r0 = PopStack(_RTMP0);
            VirtualRegister o = el.Register.RegisterNumber < 0 ? new TemporaryRegister(_RTMP0) : el.Register;
            //There are no clt instructions, so we use "not (cgt or ceq)"

            if (t == typeof(int))
                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.cgt(_RTMP2, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //Greater than
                    new SPEEmulator.OpCodes.ceq(_RTMP1, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //Equal
                    new SPEEmulator.OpCodes.or(_RTMP0, _RTMP1, _RTMP2), //Greater than _or_ equal
                    new SPEEmulator.OpCodes.xori(_RTMP0, _RTMP0, 0x3ff), //Invert bitmask (not)
                    new SPEEmulator.OpCodes.il(_RTMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and((uint)o.RegisterNumber, _RTMP0, _RTMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            else if (t == typeof(float))
                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.fcgt(_RTMP2, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //Greater than
                    new SPEEmulator.OpCodes.fceq(_RTMP1, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //Equal
                    new SPEEmulator.OpCodes.or(_RTMP0, _RTMP1, _RTMP2), //Greater than _or_ equal
                    new SPEEmulator.OpCodes.xori(_RTMP0, _RTMP0, 0x3ff), //Invert bitmask (not)
                    new SPEEmulator.OpCodes.il(_RTMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and((uint)o.RegisterNumber, _RTMP0, _RTMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            else if (t == typeof(long))
            {
                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    //Part1: cgt
                    new SPEEmulator.OpCodes.ceq(_RTMP3, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //This will compare words, giving 0xffffffff for equal than and 0 otherwise
                    new SPEEmulator.OpCodes.clgt(_RTMP2, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //This will compare words, giving 0xffffffff for logically greater than and 0 otherwise
                    new SPEEmulator.OpCodes.rotqbyi(_RTMP2, _RTMP2, 4), //Prepare the tmp3 by an 8 byte rotate
                    new SPEEmulator.OpCodes.and(_RTMP3, _RTMP2, _RTMP3), //And the results so we disregard the lower word unless the upper words are equal
                    new SPEEmulator.OpCodes.cgt(_RTMP2, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //This will compare words, giving 0xffffffff for greater than and 0 otherwise
                    new SPEEmulator.OpCodes.or(_RTMP2, _RTMP2, _RTMP3), //Or the results so prefered word slot is either 0 or 0xffffffff

                    //Part2: ceq
                    new SPEEmulator.OpCodes.ceq(_RTMP1, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //This will compare words, giving 0xffffffff for greater than and 0 otherwise
                    new SPEEmulator.OpCodes.rotqbyi(_RTMP3, _RTMP1, 4), //Prepare the tmp1 by an 8 byte rotate
                    new SPEEmulator.OpCodes.and(_RTMP1, _RTMP1, _RTMP3), //And the results so prefered word slot is either 0 or 0xffffffff

                    //Combine
                    new SPEEmulator.OpCodes.or(_RTMP0, _RTMP2, _RTMP1), //Greater than _or_ equal
                    new SPEEmulator.OpCodes.xori(_RTMP0, _RTMP0, 0x3ff), //Invert bitmask (not)

                    //Mask
                    new SPEEmulator.OpCodes.il(_RTMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and((uint)o.RegisterNumber, _RTMP0, _RTMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            }
            else if (t == typeof(double))
                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.dfcgt(_RTMP2, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //Greater than
                    new SPEEmulator.OpCodes.dfceq(_RTMP1, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //Equal
                    new SPEEmulator.OpCodes.or(_RTMP2, _RTMP2, _RTMP1), //Greater than _or_ equal
                    new SPEEmulator.OpCodes.xori(_RTMP2, _RTMP2, 0x3ff), //Invert bitmask (not)
                    new SPEEmulator.OpCodes.il(_RTMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and((uint)o.RegisterNumber, _RTMP2, _RTMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            else
                throw new InvalidProgramException("clt for <" + el.Childnodes[0].StorageClass + "> ?");

            PushStack(o);
        }

        public void Clt_un(InstructionElement el)
        {
            Type t = ValidateBinaryOp(el, false);

            VirtualRegister r1 = PopStack(_RTMP1);
            VirtualRegister r0 = PopStack(_RTMP0);
            VirtualRegister o = el.Register.RegisterNumber < 0 ? new TemporaryRegister(_RTMP0) : el.Register;

            //There are no clt_un instructions, so we use "not (clgt or ceq)"

            if (t == typeof(int))
                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.clgt(_RTMP2, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //Greater than
                    new SPEEmulator.OpCodes.ceq(_RTMP1, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //Equal
                    new SPEEmulator.OpCodes.or(_RTMP0, _RTMP2, _RTMP1), //Greater than _or_ equal
                    new SPEEmulator.OpCodes.xori(_RTMP0, _RTMP0, 0x3ff), //Invert bitmask (not)
                    new SPEEmulator.OpCodes.il(_RTMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and((uint)o.RegisterNumber, _RTMP0, _RTMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            else if (t == typeof(float))
                throw new MissingMethodException("Floating unordered?");
            else if (t == typeof(long))
            {
                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    //Part1: cgt
                    new SPEEmulator.OpCodes.ceq(_RTMP3, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //This will compare words, giving 0xffffffff for equal than and 0 otherwise
                    new SPEEmulator.OpCodes.clgt(_RTMP2, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //This will compare words, giving 0xffffffff for logically greater than and 0 otherwise
                    new SPEEmulator.OpCodes.rotqbyi(_RTMP2, _RTMP2, 4), //Prepare the tmp3 by an 8 byte rotate
                    new SPEEmulator.OpCodes.and(_RTMP3, _RTMP2, _RTMP3), //And the results so we disregard the lower word unless the upper words are equal
                    new SPEEmulator.OpCodes.clgt(_RTMP2, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //This will compare words, giving 0xffffffff for greater than and 0 otherwise
                    new SPEEmulator.OpCodes.or(_RTMP2, _RTMP2, _RTMP3), //Or the results so prefered word slot is either 0 or 0xffffffff

                    //Part2: ceq
                    new SPEEmulator.OpCodes.ceq(_RTMP1, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //This will compare words, giving 0xffffffff for greater than and 0 otherwise
                    new SPEEmulator.OpCodes.rotqbyi(_RTMP3, _RTMP1, 4), //Prepare the tmp1 by an 8 byte rotate
                    new SPEEmulator.OpCodes.and(_RTMP1, _RTMP1, _RTMP3), //And the results so prefered word slot is either 0 or 0xffffffff

                    //Combine
                    new SPEEmulator.OpCodes.or(_RTMP0, _RTMP2, _RTMP1), //Greater than _or_ equal
                    new SPEEmulator.OpCodes.xori(_RTMP0, _RTMP0, 0x3ff), //Invert bitmask (not)

                    //Mask
                    new SPEEmulator.OpCodes.il(_RTMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and((uint)o.RegisterNumber, _RTMP0, _RTMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            }
            else if (t == typeof(double))
                throw new MissingMethodException("Double floating unordered?");
            else
                throw new InvalidProgramException("clt for <" + el.Childnodes[0].StorageClass + "> ?");

            PushStack(o);
        }


        public void Cgt(InstructionElement el)
        {
            Type t = ValidateBinaryOp(el, false);

            VirtualRegister r1 = PopStack(_RTMP1);
            VirtualRegister r0 = PopStack(_RTMP0);
            VirtualRegister o = el.Register.RegisterNumber < 0 ? new TemporaryRegister(_RTMP0) : el.Register;

            if (t == typeof(int))
                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.cgt(_RTMP0, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber),
                    new SPEEmulator.OpCodes.il(_RTMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and((uint)o.RegisterNumber, _RTMP0, _RTMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            else if (t == typeof(float))
                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.fcgt(_RTMP0, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber),
                    new SPEEmulator.OpCodes.il(_RTMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and((uint)o.RegisterNumber, _RTMP0, _RTMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            else if (t == typeof(long))
            {
                //There is no cgtd, so we make our own
                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.ceq(_RTMP3, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //This will compare words, giving 0xffffffff for equal than and 0 otherwise
                    new SPEEmulator.OpCodes.clgt(_RTMP2, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //This will compare words, giving 0xffffffff for logically greater than and 0 otherwise
                    new SPEEmulator.OpCodes.rotqbyi(_RTMP2, _RTMP2, 4), //Prepare the tmp3 by an 8 byte rotate
                    new SPEEmulator.OpCodes.and(_RTMP3, _RTMP2, _RTMP3), //And the results so we disregard the lower word unless the upper words are equal
                    new SPEEmulator.OpCodes.cgt(_RTMP0, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //This will compare words, giving 0xffffffff for greater than and 0 otherwise
                    new SPEEmulator.OpCodes.or(_RTMP0, _RTMP0, _RTMP3), //Or the results so prefered word slot is either 0 or 0xffffffff
                    new SPEEmulator.OpCodes.il(_RTMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and((uint)o.RegisterNumber, _RTMP0, _RTMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            }
            else if (t == typeof(double))
                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.dfcgt(_RTMP0, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber),
                    new SPEEmulator.OpCodes.il(_RTMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and((uint)o.RegisterNumber, _RTMP0, _RTMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            else
                throw new InvalidProgramException("cgt for <" + el.Childnodes[0].StorageClass + "> ?");

            PushStack(o);
        }

        public void Cgt_un(InstructionElement el)
        {
            Type t = ValidateBinaryOp(el, false);

            VirtualRegister r1 = PopStack(_RTMP1);
            VirtualRegister r0 = PopStack(_RTMP0);
            VirtualRegister o = el.Register.RegisterNumber < 0 ? new TemporaryRegister(_RTMP0) : el.Register;

            if (t == typeof(int))
                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.clgt(_RTMP0, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber),
                    new SPEEmulator.OpCodes.il(_RTMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and((uint)o.RegisterNumber, _RTMP0, _RTMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            else if (t == typeof(float))
                throw new MissingMethodException("Floating unordered?");
            else if (t == typeof(long))
            {
                //There is no cgtd, so we make our own
                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.ceq(_RTMP3, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //This will compare words, giving 0xffffffff for equal than and 0 otherwise
                    new SPEEmulator.OpCodes.clgt(_RTMP2, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //This will compare words, giving 0xffffffff for logically greater than and 0 otherwise
                    new SPEEmulator.OpCodes.rotqbyi(_RTMP2, _RTMP2, 4), //Prepare the tmp3 by an 8 byte rotate
                    new SPEEmulator.OpCodes.and(_RTMP3, _RTMP2, _RTMP3), //And the results so we disregard the lower word unless the upper words are equal
                    new SPEEmulator.OpCodes.clgt(_RTMP0, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //This will compare words, giving 0xffffffff for greater than and 0 otherwise
                    new SPEEmulator.OpCodes.or(_RTMP0, _RTMP0, _RTMP3), //Or the results so prefered word slot is either 0 or 0xffffffff
                    new SPEEmulator.OpCodes.il(_RTMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and((uint)o.RegisterNumber, _RTMP0, _RTMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            }
            else if (t == typeof(double))
                throw new MissingMethodException("Double floating unordered?");
            else
                throw new InvalidProgramException("cgt_un for <" + el.Childnodes[0].StorageClass + "> ?");

            PushStack(o);
        }

        public void Ceq(InstructionElement el)
        {
            Type t = ValidateBinaryOp(el, true);

            VirtualRegister r1 = PopStack(_RTMP1);
            VirtualRegister r0 = PopStack(_RTMP0);
            VirtualRegister o = el.Register.RegisterNumber < 0 ? new TemporaryRegister(_RTMP0) : el.Register;

            if (t == typeof(int) || t == typeof(IntPtr))
                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.ceq(_RTMP0, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber),
                    new SPEEmulator.OpCodes.il(_RTMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and((uint)o.RegisterNumber, _RTMP0, _RTMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            else if (t == typeof(float))
                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.fceq(_RTMP0, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber),
                    new SPEEmulator.OpCodes.il(_RTMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and((uint)o.RegisterNumber, _RTMP0, _RTMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            else if (t == typeof(long))
            {
                //There is no ceqd, so we make our own
                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.ceq(_RTMP0, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber), //This will compare words, giving 0 for equal and 0xffffffff otherwise
                    new SPEEmulator.OpCodes.rotqbyi(_RTMP1, _RTMP0, 4), //Prepare the tmp1 by an 8 byte rotate
                    new SPEEmulator.OpCodes.and(_RTMP0, _RTMP0, _RTMP1), //Or the results so prefered word slot is either 0 or 0xffffffff
                    new SPEEmulator.OpCodes.il(_RTMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and((uint)o.RegisterNumber, _RTMP0, _RTMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            }
            else if (t == typeof(double))
                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.dfceq(_RTMP0, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber),
                    new SPEEmulator.OpCodes.il(_RTMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and((uint)o.RegisterNumber, _RTMP0, _RTMP1), //Make sure that the result is a word of either 0 or 1
                });
            else
                throw new InvalidProgramException("ceq for <" + el.Childnodes[0].StorageClass + "> ?");

            PushStack(o);
        }

        public void Stloc_0(InstructionElement el)
        {
            PopStack(_LV0, true);
        }

        public void Stloc_1(InstructionElement el)
        {
            PopStack(_LV0 + 1u, true);
        }

        public void Stloc_2(InstructionElement el)
        {
            PopStack(_LV0 + 2u, true);
        }

        public void Stloc_3(InstructionElement el)
        {
            PopStack(_LV0 + 3u, true);
        }

        public void Stloc_s(InstructionElement el)
        {
            PopStack(_LV0 + (uint)((Mono.Cecil.Cil.VariableReference)el.Instruction.Operand).Index, true);
        }

        public void Ldloc_0(InstructionElement el)
        {
            PushStack(new VirtualRegister(_LV0));
        }

        public void Ldloc_1(InstructionElement el)
        {
            PushStack(new VirtualRegister(_LV0 + 1u));
        }

        public void Ldloc_2(InstructionElement el)
        {
            PushStack(new VirtualRegister(_LV0 + 2u));
        }

        public void Ldloc_3(InstructionElement el)
        {
            PushStack(new VirtualRegister(_LV0 + 3u));
        }

        public void Ldloc_s(InstructionElement el)
        {
            PushStack(new VirtualRegister(_LV0 + (uint)((Mono.Cecil.Cil.VariableReference)el.Instruction.Operand).Index));
        }

        public void Br(InstructionElement el)
        {
            m_state.RegisterBranch(((Mono.Cecil.Cil.Instruction)el.Instruction.Operand));
            m_state.Instructions.Add(new SPEEmulator.OpCodes.br(_RTMP0, 0xffff));
        }

        public void Br_s(InstructionElement el)
        {
            Br(el);
        }


        public void Sub(InstructionElement el)
        {
            Type t = ValidateBinaryOp(el, false);

            VirtualRegister r1 = PopStack(_RTMP1);
            VirtualRegister r0 = PopStack(_RTMP0);
            VirtualRegister o = el.Register.RegisterNumber < 0 ? new TemporaryRegister(_RTMP0) : el.Register;

            if (t == typeof(int))
                m_state.Instructions.Add(new SPEEmulator.OpCodes.sf((uint)o.RegisterNumber, (uint)r1.RegisterNumber, (uint)r0.RegisterNumber));
            else if (t == typeof(float))
                m_state.Instructions.Add(new SPEEmulator.OpCodes.fs((uint)o.RegisterNumber, (uint)r1.RegisterNumber, (uint)r0.RegisterNumber));
            else if (t == typeof(double))
                m_state.Instructions.Add(new SPEEmulator.OpCodes.dfs((uint)o.RegisterNumber, (uint)r1.RegisterNumber, (uint)r0.RegisterNumber));
            else if (t == typeof(long))
            {
                //We have no subfd, so we make one ourselves
                m_state.RegisterConstantLoad(0x04050607c0c0c0c0, 0x0c0d0e0fc0c0c0c0);

                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.lqr(_RTMP3, 0), //Load substract mask
                    new SPEEmulator.OpCodes.bg(_RTMP2, (uint)r1.RegisterNumber, (uint)r0.RegisterNumber), //Calculate borrow
                    new SPEEmulator.OpCodes.shufb(_RTMP2, _RTMP2, _RTMP2, _RTMP3), //Move the borrow into the right place
                    new SPEEmulator.OpCodes.sfx(_RTMP2, (uint)r1.RegisterNumber, (uint)r0.RegisterNumber), //Subtract the two dwords
                    new SPEEmulator.OpCodes.ori((uint)o.RegisterNumber, _RTMP2, 0),
                });
            }
            else
                throw new InvalidProgramException();

            PushStack(o);
        }

        public void Mul(InstructionElement el)
        {
            Type t = ValidateBinaryOp(el, false);

            VirtualRegister r0 = PopStack(_RTMP0);
            VirtualRegister r1 = PopStack(_RTMP1);
            VirtualRegister o = el.Register.RegisterNumber < 0 ? new TemporaryRegister(_RTMP0) : el.Register;

            if (t == typeof(long))
            {

                //64bit multiply is emulated with a function call

                //We will now overwrite these two registers
                PushStack(new TemporaryRegister(_ARG0 + 1));

                if (o.RegisterNumber != _ARG0)
                    PushStack(new TemporaryRegister(_ARG0));

                m_state.Instructions.Add(new SPEEmulator.OpCodes.ori(_ARG0, (uint)r0.RegisterNumber, 0));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.ori(_ARG0 + 1, (uint)r1.RegisterNumber, 0));

                m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_RTMP4, 2)); //Signal 2 parameters for function
                m_state.RegisterCall(CompiledMethod.m_spe_builtins["umul"]);
                m_state.Instructions.Add(new SPEEmulator.OpCodes.brasl(0, 0xffff));

                PopStack(_ARG0 + 1, true);

                //If the output is _ARG0, everything is now in place, otherwise we need to move the value and restore _ARG0
                if (o.RegisterNumber != _ARG0)
                {
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.ori((uint)o.RegisterNumber, _ARG0, 0));
                    PopStack(_ARG0, true);
                }


            }
            else if (t == typeof(int))
            {
                //Unfortunately, the SPU favors 16bit integer multiply, so this block emulates 32bit multiply
                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.mpyh(_RTMP3, (uint)r1.RegisterNumber, (uint)r0.RegisterNumber),
                    new SPEEmulator.OpCodes.mpyh(_RTMP2, (uint)r0.RegisterNumber, (uint)r1.RegisterNumber),
                    new SPEEmulator.OpCodes.mpyu(_RTMP1, (uint)r1.RegisterNumber, (uint)r0.RegisterNumber),
                    new SPEEmulator.OpCodes.a(_RTMP0, _RTMP3, _RTMP2),
                    new SPEEmulator.OpCodes.a((uint)o.RegisterNumber, _RTMP0, _RTMP1)
                });
            }
            else if (t == typeof(float))
                m_state.Instructions.Add(new SPEEmulator.OpCodes.fm((uint)o.RegisterNumber, (uint)r1.RegisterNumber, (uint)r0.RegisterNumber));
            else if (t == typeof(double))
                m_state.Instructions.Add(new SPEEmulator.OpCodes.dfm((uint)o.RegisterNumber, (uint)r1.RegisterNumber, (uint)r0.RegisterNumber));
            else
                throw new InvalidProgramException();

            PushStack(o);
        }

        public void And(InstructionElement el)
        {
            VirtualRegister r1 = PopStack(_RTMP1);
            VirtualRegister r0 = PopStack(_RTMP0);
            VirtualRegister o = el.Register.RegisterNumber < 0 ? new TemporaryRegister(_RTMP0) : el.Register;

            m_state.Instructions.Add(new SPEEmulator.OpCodes.and((uint)o.RegisterNumber, (uint)r1.RegisterNumber, (uint)r0.RegisterNumber));
        
            PushStack(o);
        }

        public void Or(InstructionElement el)
        {
            VirtualRegister r1 = PopStack(_RTMP1);
            VirtualRegister r0 = PopStack(_RTMP0);
            VirtualRegister o = el.Register.RegisterNumber < 0 ? new TemporaryRegister(_RTMP0) : el.Register;

            m_state.Instructions.Add(new SPEEmulator.OpCodes.or((uint)o.RegisterNumber, (uint)r1.RegisterNumber, (uint)r0.RegisterNumber));

            PushStack(o);
        }

        public void Shl(InstructionElement el)
        {
            if (el.Childnodes == null || el.Childnodes.Length != 2)
                throw new InvalidProgramException();

            Type t = el.Childnodes[0].StorageClass;
            Type bitCounter = el.Childnodes[1].StorageClass;

            VirtualRegister r1 = PopStack(_RTMP1);
            VirtualRegister r0 = PopStack(_RTMP0);
            VirtualRegister o = el.Register.RegisterNumber < 0 ? new TemporaryRegister(_RTMP0) : el.Register;

            if (bitCounter == typeof(long))
            {
                //We discard the high word, and rotate the low word into the preferred slot
                m_state.Instructions.Add(new SPEEmulator.OpCodes.rotqbyi(_RTMP1, (uint)r1.RegisterNumber, 4));
            }
            else if (bitCounter == typeof(int))
            {
                //We need the value in a temporary register
                if (r1.RegisterNumber != _RTMP1)
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.ori(_RTMP1, (uint)r1.RegisterNumber, 0));
            }
            else
                throw new InvalidProgramException("Cannot shl with type " + el.Childnodes[1].ReturnType.ToString());

            if (t == typeof(int))
            {
                m_state.Instructions.Add(new SPEEmulator.OpCodes.shl((uint)o.RegisterNumber, (uint)r0.RegisterNumber, _RTMP1));
            }
            else if (t == typeof(long))
            {
                //We need to do this with each dword, so we mask the low dword for _TMP2
                m_state.Instructions.Add(new SPEEmulator.OpCodes.fsmbi(_RTMP2, 0xff00));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.and(_RTMP2, _RTMP2, (uint)r0.RegisterNumber));

                //Perform shl on both high and low dword for bits, eg modulo 8
                m_state.Instructions.Add(new SPEEmulator.OpCodes.shlqbi(_RTMP0, (uint)r0.RegisterNumber, _RTMP1));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.shlqbi(_RTMP2, _RTMP2, _RTMP1));

                //Calculate the number of bytes to shift
                m_state.Instructions.Add(new SPEEmulator.OpCodes.rotmi(_RTMP1, _RTMP1, ((-3) & 0x7F)));

                //Perform shl on both high and low dword for bytes
                m_state.Instructions.Add(new SPEEmulator.OpCodes.shlqby(_RTMP0, _RTMP0, _RTMP1));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.shlqby(_RTMP2, _RTMP2, _RTMP1));

                //Mask out any bits that are shifted from the low dword into the high dword
                m_state.Instructions.Add(new SPEEmulator.OpCodes.fsmbi(_RTMP3, 0x00ff));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.and(_RTMP0, _RTMP0, _RTMP3));

                //Combine the two dwords
                m_state.Instructions.Add(new SPEEmulator.OpCodes.or((uint)o.RegisterNumber, _RTMP0, _RTMP2));

            }
            else
                throw new InvalidProgramException();

            PushStack(o);
        }

        public void Shr(InstructionElement el)
        {
            Shr_common(el);
        }

        public void Shr_un(InstructionElement el)
        {
            Shr_common(el);
        }

        public void Shr_common(InstructionElement el)
        {
            VirtualRegister r1 = PopStack(_RTMP1);
            VirtualRegister r0 = PopStack(_RTMP0);
            VirtualRegister o = el.Register.RegisterNumber < 0 ? new TemporaryRegister(_RTMP0) : el.Register;

            if (el.Childnodes == null || el.Childnodes.Length != 2)
                throw new InvalidProgramException();

            Type t = el.Childnodes[0].StorageClass;
            Type bitCounter = el.Childnodes[1].StorageClass;

            if (bitCounter == typeof(long))
            {
                //We discard the high word, and rotate the low word into the preferred slot
                m_state.Instructions.Add(new SPEEmulator.OpCodes.rotqbyi(_RTMP1, (uint)r1.RegisterNumber, 4));
            }
            else if (bitCounter == typeof(int))
            {
                //We need the value in a temporary register
                if (r1.RegisterNumber != _RTMP1)
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.ori(_RTMP1, (uint)r1.RegisterNumber, 0));
            }
            else
                throw new InvalidProgramException("Cannot shr_un with type " + el.Childnodes[1].ReturnType.ToString());


            if (t == typeof(int))
            {
                m_state.Instructions.Add(new SPEEmulator.OpCodes.sfi(_RTMP1, _RTMP1, 0)); //Form 2's complement mask
                if (el.Instruction.OpCode.Code == Mono.Cecil.Cil.Code.Shr_Un)
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.rotm((uint)o.RegisterNumber, (uint)r0.RegisterNumber, _RTMP1));
                else
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.rotma((uint)o.RegisterNumber, (uint)r0.RegisterNumber, _RTMP1));
            }
            else if (t == typeof(long))
            {
                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.ori(_RTMP2, _RTMP1, 0),
                    new SPEEmulator.OpCodes.rotmi(_RTMP1, _RTMP1, (uint)((-3) & 0x7f)),
                    new SPEEmulator.OpCodes.sfi(_RTMP1, _RTMP1, 0), //Form 2's complement mask for byte count
                    new SPEEmulator.OpCodes.sfi(_RTMP2, _RTMP2, 0), //Form 2's complement mask for bit count
                });

                if (el.Instruction.OpCode.Code == Mono.Cecil.Cil.Code.Shr_Un)
                {
                    //Mask out the high dword so it doesn't flow into the lower dword
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.fsmbi(_RTMP3, 0x00ff));
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.and(_RTMP3, (uint)r0.RegisterNumber, _RTMP3));
                    
                    //Rotate the high dword
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.rotqmby(_RTMP0, (uint)r0.RegisterNumber, _RTMP1));
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.rotqmbi(_RTMP0, _RTMP0, _RTMP2));

                    //Rotate the low dword
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.rotqmby(_RTMP3, _RTMP3, _RTMP1));
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.rotqmbi(_RTMP3, _RTMP3, _RTMP2));

                    //Mask out any overflow from the high dword rotate
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.fsmbi(_RTMP2, 0xff00));
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.and(_RTMP0, _RTMP2, _RTMP0));

                    //Combine the two results
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.or((uint)o.RegisterNumber, _RTMP3, _RTMP0));

                }
                else
                {
                    throw new MissingMethodException("64bit signed shr is not supported");
                }
            }
            else
                throw new InvalidProgramException();

            PushStack(o);
        }

        public void Add(InstructionElement el)
        {
            Type t = ValidateBinaryOp(el, false);

            VirtualRegister r1 = PopStack(_RTMP1);
            VirtualRegister r0 = PopStack(_RTMP0);
            VirtualRegister o = el.Register.RegisterNumber < 0 ? new TemporaryRegister(_RTMP2) : el.Register;

            if (t == typeof(int))
                m_state.Instructions.Add(new SPEEmulator.OpCodes.a((uint)o.RegisterNumber, (uint)r1.RegisterNumber, (uint)r0.RegisterNumber));
            else if (t == typeof(float))
                m_state.Instructions.Add(new SPEEmulator.OpCodes.fa((uint)o.RegisterNumber, (uint)r1.RegisterNumber, (uint)r0.RegisterNumber));
            else if (t == typeof(double))
                m_state.Instructions.Add(new SPEEmulator.OpCodes.dfa((uint)o.RegisterNumber, (uint)r1.RegisterNumber, (uint)r0.RegisterNumber));
            else if (t == typeof(long))
            {
                m_state.RegisterConstantLoad(0x0405060780808080, 0x0c0d0e0f80808080);

                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.lqr(_RTMP3, 0), //Load add mask
                    new SPEEmulator.OpCodes.cg(_RTMP2, (uint)r1.RegisterNumber, (uint)r0.RegisterNumber), //Calculate carry
                    new SPEEmulator.OpCodes.shufb(_RTMP2, _RTMP2, _RTMP2, _RTMP3), //Rotate the carry to the far right
                    new SPEEmulator.OpCodes.addx(_RTMP2, (uint)r1.RegisterNumber, (uint)r0.RegisterNumber), //Add the two words
                    new SPEEmulator.OpCodes.ori((uint)o.RegisterNumber, _RTMP2, 0), //Add the two words
                });

            }
            else
                throw new InvalidProgramException();

            PushStack(o);
        }

        public void Call(InstructionElement el)
        {
            Mono.Cecil.MethodReference mdef = (Mono.Cecil.MethodReference)el.Instruction.Operand;

            if (mdef.Parameters.Count > MAX_FUCTION_ARGUMENTS)
                throw new Exception("Too many arguments ");

            //Pop all required registers off stack and place them in call registers
            uint register = (uint)((_ARG0 + mdef.Parameters.Count) - 1);
            for (int i = 0; i < mdef.Parameters.Count; i++)
                PopStack((uint)(register - i), true);

            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_RTMP4, (uint)mdef.Parameters.Count)); //Signal parameter count for function
            m_state.RegisterCall(mdef);
            // i16 (set to 0xffff) should be replaced with correct value, when it is known!
            m_state.Instructions.Add(new SPEEmulator.OpCodes.brasl(0, 0xffff));

            if (mdef.ReturnType.ReturnType.FullName != "System.Void")
                PushStack(new VirtualRegister(_ARG0));
        }

        public void Ret(InstructionElement el)
        {
            //If the function returns a value, place it in $3
            if (el.ParentMethod.ReturnType.ReturnType.FullName != "System.Void")
                PopStack(_ARG0, true);

            m_state.RegisterReturn();
            m_state.Instructions.Add(new SPEEmulator.OpCodes.br(_RTMP0, 0xffff));
        }

        public void Beq(InstructionElement el)
        {
            System.Diagnostics.Debug.Assert(el.Register == null);

            //Assign a valid virtual register to prevent it from ending on stack
            el.Register = new VirtualRegister(_RTMP0);

            Ceq(el);

            VirtualRegister r = PopStack(_RTMP0);

            m_state.RegisterBranch(((Mono.Cecil.Cil.Instruction)el.Instruction.Operand));
            m_state.Instructions.Add(new SPEEmulator.OpCodes.brnz((uint)r.RegisterNumber, 0xffff));

            //Clear the dummy register
            el.Register = null;
        }

        public void Beq_s(InstructionElement el)
        {
            Beq(el);
        }

        public void Brfalse(InstructionElement el)
        {
            Brtrue(el);

            //Invert the branch instruction to use brz instead of brnz
            uint regno = ((SPEEmulator.OpCodes.brnz)m_state.Instructions[m_state.Instructions.Count - 1]).RT;
            m_state.Instructions[m_state.Instructions.Count - 1] = new SPEEmulator.OpCodes.brz(regno, 0xffff);
        }

        public void Brfalse_s(InstructionElement el)
        {
            Brfalse(el);
        }

        public void Brtrue(InstructionElement el)
        {
            VirtualRegister r = PopStack(_RTMP0);

            if (el.Childnodes[0].StorageClass == typeof(long))
            {
                m_state.Instructions.Add(new SPEEmulator.OpCodes.rotqbyi(_RTMP1, (uint)r.RegisterNumber, 4));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.or(_RTMP0, _RTMP0, _RTMP1));
                
                m_state.RegisterBranch(((Mono.Cecil.Cil.Instruction)el.Instruction.Operand));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.brnz(_RTMP0, 0xffff));

            }
            else
            {
                m_state.RegisterBranch(((Mono.Cecil.Cil.Instruction)el.Instruction.Operand));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.brnz((uint)r.RegisterNumber, 0xffff));
            }
        }

        public void Brtrue_s(InstructionElement el)
        {
            Brtrue(el);
        }

        public void Bne_un(InstructionElement el)
        {
            System.Diagnostics.Debug.Assert(el.Register == null);

            //Assign a valid virtual register to prevent it from ending on stack
            el.Register = new VirtualRegister(_RTMP0);

            //Note: The specification states that this instruction is the same as 
            //"ceq followed by brfalse"
            Ceq(el);

            VirtualRegister r = PopStack(_RTMP0);

            m_state.RegisterBranch(((Mono.Cecil.Cil.Instruction)el.Instruction.Operand));
            m_state.Instructions.Add(new SPEEmulator.OpCodes.brz((uint)r.RegisterNumber, 0xffff));

            //Clear the dummy register
            el.Register = null;
        }

        public void Bne_un_s(InstructionElement el)
        {
            Bne_un(el);
        }

        public void Pop(InstructionElement el)
        {
            PopStack(_RTMP0);
        }

        public void Ldelem_i1(InstructionElement el)
        {
            Ldelem_common(el);
        }

        public void Ldelem_i2(InstructionElement el)
        {
            Ldelem_common(el);
        }

        public void Ldelem_i4(InstructionElement el)
        {
            Ldelem_common(el);
        }

        public void Ldelem_i8(InstructionElement el)
        {
            Ldelem_common(el);
        }

        public void Ldelem_u1(InstructionElement el)
        {
            Ldelem_common(el);
        }

        public void Ldelem_u2(InstructionElement el)
        {
            Ldelem_common(el);
        }
        
        public void Ldelem_u4(InstructionElement el)
        {
            Ldelem_common(el);
        }

        public void Ldelem_r4(InstructionElement el)
        {
            Ldelem_common(el);
        }

        public void Ldelem_r8(InstructionElement el)
        {
            Ldelem_common(el);
        }

        public void Ldelem_common(InstructionElement el)
        {
            VirtualRegister elementIndex = PopStack(_RTMP0);
            VirtualRegister arrayPointer = PopStack(_RTMP1);
            VirtualRegister o = el.Register.RegisterNumber < 0 ? new TemporaryRegister(_RTMP0) : el.Register;

            if (el.Childnodes[1].StorageClass != typeof(int))
                throw new Exception("Unexpected index type: " + el.Childnodes[1].StorageClass.ToString());

            KnownObjectTypes[] arraytypes;

            switch (el.Instruction.OpCode.Code)
            {
                case Mono.Cecil.Cil.Code.Ldelem_I1:
                //case Mono.Cecil.Cil.Code.Ldelem_Bool: //Does not exist
                    arraytypes = new KnownObjectTypes[] { KnownObjectTypes.SByte, KnownObjectTypes.Boolean };
                    break;
                case Mono.Cecil.Cil.Code.Ldelem_I2:
                    arraytypes = new KnownObjectTypes[] { KnownObjectTypes.Short };
                    break;
                case Mono.Cecil.Cil.Code.Ldelem_I4:
                    arraytypes = new KnownObjectTypes[] {  KnownObjectTypes.Int };
                    break;
                case Mono.Cecil.Cil.Code.Ldelem_I8:
                //case Mono.Cecil.Cil.Code.Ldelem_U8: //Does not exist
                    arraytypes = new KnownObjectTypes[] {  KnownObjectTypes.Long, KnownObjectTypes.ULong };
                    break;
                case Mono.Cecil.Cil.Code.Ldelem_R4:
                    arraytypes = new KnownObjectTypes[] {  KnownObjectTypes.Float };
                    break;
                case Mono.Cecil.Cil.Code.Ldelem_R8:
                    arraytypes = new KnownObjectTypes[] {  KnownObjectTypes.Double };
                    break;
                case Mono.Cecil.Cil.Code.Ldelem_U1:
                    arraytypes = new KnownObjectTypes[] {  KnownObjectTypes.Byte };
                    break;
                case Mono.Cecil.Cil.Code.Ldelem_U2:
                    arraytypes = new KnownObjectTypes[] {  KnownObjectTypes.UShort };
                    break;
                case Mono.Cecil.Cil.Code.Ldelem_U4:
                    arraytypes = new KnownObjectTypes[] {  KnownObjectTypes.UInt };
                    break;
                default:
                    throw new Exception("Unsupport array type: " + el.Instruction.OpCode.Code);
            }

            LoadElementAddress(el, arraytypes, (uint)elementIndex.RegisterNumber, (uint)arrayPointer.RegisterNumber, _RTMP0);
            LoadElement(arraytypes[0], _RTMP0, (uint)o.RegisterNumber);

            PushStack(o);
        }


        public void Stelem_i1(InstructionElement el)
        {
            Stelem_common(el);
        }

        public void Stelem_i2(InstructionElement el)
        {
            Stelem_common(el);
        }

        public void Stelem_i4(InstructionElement el)
        {
            Stelem_common(el);
        }

        public void Stelem_i8(InstructionElement el)
        {
            Stelem_common(el);
        }

        public void Stelem_r4(InstructionElement el)
        {
            Stelem_common(el);
        }

        public void Stelem_r8(InstructionElement el)
        {
            Stelem_common(el);
        }

        public void Stelem_common(InstructionElement el)
        {
            VirtualRegister value = PopStack(_RTMP4);

            VirtualRegister elementIndex = PopStack(_RTMP0);
            VirtualRegister arrayPointer = PopStack(_RTMP1);

            KnownObjectTypes[] arraytypes;

            switch (el.Instruction.OpCode.Code)
            {
                case Mono.Cecil.Cil.Code.Stelem_I:
                    arraytypes = new KnownObjectTypes[] { KnownObjectTypes.Int };
                    break;
                case Mono.Cecil.Cil.Code.Stelem_I1:
                    arraytypes = new KnownObjectTypes[] { KnownObjectTypes.SByte, KnownObjectTypes.Boolean, KnownObjectTypes.Byte };
                    break;
                case Mono.Cecil.Cil.Code.Stelem_I2:
                    arraytypes = new KnownObjectTypes[] { KnownObjectTypes.Short, KnownObjectTypes.UShort };
                    break;
                case Mono.Cecil.Cil.Code.Stelem_I4:
                    arraytypes = new KnownObjectTypes[] { KnownObjectTypes.Int, KnownObjectTypes.UInt };
                    break;
                case Mono.Cecil.Cil.Code.Stelem_I8:
                    arraytypes = new KnownObjectTypes[] { KnownObjectTypes.Long, KnownObjectTypes.ULong };
                    break;
                case Mono.Cecil.Cil.Code.Stelem_R4:
                    arraytypes = new KnownObjectTypes[] { KnownObjectTypes.Float };
                    break;
                case Mono.Cecil.Cil.Code.Stelem_R8:
                    arraytypes = new KnownObjectTypes[] { KnownObjectTypes.Double };
                    break;
                default:
                    throw new Exception("Unsupported stelem instruction: " + el.Instruction.OpCode.Code);
            }

            LoadElementAddress(el, arraytypes, (uint)elementIndex.RegisterNumber, (uint)arrayPointer.RegisterNumber, _RTMP1);
            StoreElement(arraytypes[0], _RTMP1, (uint)value.RegisterNumber);
        }

        /// <summary>
        /// Internal helper function that takes two registers (tmp0 and tmp1) and calculates the absolute LS address of an
        /// array element and stores the address in the output register
        /// </summary>
        /// <param name="el">The instruction being issued</param>
        /// <param name="arraytype">The accepted array element type, all must have same size</param>
        /// <param name="elementIndex">The register that holds the element index, must be _RTMP0 or _RTMP1</param>
        /// <param name="arrayPointer">The register that holds the object pointer index, must be _RTMP0 or _RTMP1</param>
        /// <param name="output">The register into which the resulting address will be written</param>
        private void LoadElementAddress(InstructionElement el, AccCIL.KnownObjectTypes[] arraytypes, uint elementIndex, uint arrayPointer, uint output)
        {
            //Ensure that we are not using the temp registers
            System.Diagnostics.Debug.Assert(elementIndex != _RTMP2 && elementIndex != _RTMP3);
            System.Diagnostics.Debug.Assert(arrayPointer != _RTMP2 && arrayPointer != _RTMP3);

            uint extraTmp = arrayPointer;
            if (extraTmp != _RTMP0 && extraTmp != _RTMP1)
                extraTmp = elementIndex == _RTMP0 ? _RTMP1 : _RTMP0;

            //Test that the array is not null
            //TODO: Jump to an NullException raise function
            m_state.Instructions.Add(new SPEEmulator.OpCodes.brz(arrayPointer, 0));

            //Get the object table entry
            m_state.Instructions.Add(new SPEEmulator.OpCodes.shli(_RTMP2, arrayPointer, 0x4)); //Times 16
            m_state.Instructions.Add(new SPEEmulator.OpCodes.lqd(_RTMP2, _RTMP2, SPEJITCompiler.OBJECT_TABLE_OFFSET / 16));

            uint eldivsize = BuiltInSPEMethods.get_array_elem_len_mult((uint)arraytypes[0]);

            //Verify that the array has the correct type and that the index is within range
            if (!el.IsIndexChecked)
            {
                //TODO: It seems that the CIL is expected to perform the compatible type check?

                if (arraytypes.Length == 1)
                {
                    //Verrify the data type
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.ceqi(_RTMP3, _RTMP2, (uint)arraytypes[0]));
                }
                else
                {
                    //Clear a storage area
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.xor(_RTMP3, _RTMP3, _RTMP3));

                    foreach (KnownObjectTypes t in arraytypes)
                    {
                        //Verrify the data type
                        m_state.Instructions.Add(new SPEEmulator.OpCodes.ceqi(extraTmp, _RTMP2, (uint)t));
                        m_state.Instructions.Add(new SPEEmulator.OpCodes.or(_RTMP3, extraTmp, _RTMP3));
                    }
                }

                //TODO: Jump to an InvalidProgram exception raise function
                m_state.Instructions.Add(new SPEEmulator.OpCodes.brz(_RTMP3, 0));


                //Verify the size of the array
                m_state.Instructions.Add(new SPEEmulator.OpCodes.shlqbyi(extraTmp, _RTMP2, 0x4)); //Move word into position
                m_state.Instructions.Add(new SPEEmulator.OpCodes.rotmi(_RTMP3, extraTmp, (uint)((-eldivsize) & 0x7f))); //Move word into position and divide with elementsize
                m_state.Instructions.Add(new SPEEmulator.OpCodes.cgt(extraTmp, elementIndex, _RTMP3));
                //TODO: Jump to an IndexOutOfRange exception raise function
                m_state.Instructions.Add(new SPEEmulator.OpCodes.brnz(extraTmp, 0x0));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.ceq(extraTmp, elementIndex, _RTMP3));
                //TODO: Jump to an IndexOutOfRange exception raise function
                m_state.Instructions.Add(new SPEEmulator.OpCodes.brnz(extraTmp, 0x0));
            }

            //Get the base pointer offset
            m_state.Instructions.Add(new SPEEmulator.OpCodes.shlqbyi(_RTMP3, _RTMP2, 0x8));

            //Calculate adress based on index * elsize + offset
            m_state.Instructions.Add(new SPEEmulator.OpCodes.shli(_RTMP2, elementIndex, (uint)eldivsize));
            m_state.Instructions.Add(new SPEEmulator.OpCodes.a(output, _RTMP3, _RTMP2));
        }

        /// <summary>
        /// Function that loads an array element from an address and places it on the stack.
        /// Note that this function is expected to be the last in a sequence and as
        /// such destroys all temporary registers.
        /// Note that this function does no type/index checking.
        /// </summary>
        /// <param name="arraytype">The array element type</param>
        /// <param name="pointer">The register that holds the address, cannot be _RTMP3</param>
        /// <param name="output">The register to receive the loaded value</param>
        private void LoadElement(KnownObjectTypes arraytype, uint pointer, uint output)
        {
            //We overwrite this register early on
            System.Diagnostics.Debug.Assert(pointer != _RTMP3);

            uint eldivsize = BuiltInSPEMethods.get_array_elem_len_mult((uint)arraytype);

            //Load the value
            m_state.Instructions.Add(new SPEEmulator.OpCodes.lqd(_RTMP3, pointer, 0));

            //Figure out how much non-aligned we are
            m_state.Instructions.Add(new SPEEmulator.OpCodes.andi(_RTMP1, pointer, 0xfu)); //Use only the lower bits
            m_state.Instructions.Add(new SPEEmulator.OpCodes.rotqby(_RTMP3, _RTMP3, _RTMP1)); //Rotate into prefered slot

            bool signed = arraytype == KnownObjectTypes.SByte || arraytype == KnownObjectTypes.Short || arraytype == KnownObjectTypes.Int || arraytype == KnownObjectTypes.Long;

            if (signed)
            {
                ulong mask;
                if (eldivsize == 0)
                    mask = 0x1010101010101010;
                else if (eldivsize == 1)
                    mask = 0x1011101110111011;
                else if (eldivsize == 2)
                    mask = 0x1011121310111213;
                else if (eldivsize == 3)
                    mask = 0x1011121314151617;
                else
                    throw new InvalidProgramException();

                //Load the new value into the register
                m_state.RegisterConstantLoad(mask, mask); //Load element into register
                m_state.Instructions.Add(new SPEEmulator.OpCodes.lqr(_RTMP0, 0));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.shufb(output, _RTMP3, _RTMP3, _RTMP0)); //Move word into position (should be 4 bytes)

                //Since the element must be stored as int32 on stack, we must convert it and sign extend it
                if (eldivsize <= 0)
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.xsbh(output, output)); //Convert to halfword
                if (eldivsize <= 1)
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.xshw(output, output)); //Convert to word
            }
            else
            {
                ulong mask;
                if (eldivsize == 0)
                    mask = 0x8080801080808010;
                else if (eldivsize == 1)
                    mask = 0x8080101180801011;
                else if (eldivsize == 2)
                    mask = 0x1011121310111213;
                else if (eldivsize == 3)
                    mask = 0x1011121314151617;
                else
                    throw new InvalidProgramException();

                //Load the new value into the register
                m_state.RegisterConstantLoad(mask, mask); //Load element into register
                m_state.Instructions.Add(new SPEEmulator.OpCodes.lqr(_RTMP0, 0));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.shufb(output, _RTMP3, _RTMP3, _RTMP0)); //Move word into position (should be 4 bytes)
            }
        }

        /// <summary>
        /// Stores the given value as an array element at the position indicated by the pointer.
        /// Note that this function is expected to be the last in the sequence and
        /// destroys all temporary registers.
        /// Note that no type/index checks are performed.
        /// </summary>
        /// <param name="arraytype">The type of the array elements</param>
        /// <param name="pointer">The register that contains the element address must not be _RTMP0, _RTMP2, _RTMP3</param>
        /// <param name="value">The register that contains the value to store, must not be _RTMP1, _RTMP2, _RTMP3</param>
        private void StoreElement(KnownObjectTypes arraytype, uint pointer, uint value)
        {
            System.Diagnostics.Debug.Assert(pointer != _RTMP0 && pointer != _RTMP2 && pointer != _RTMP3);
            System.Diagnostics.Debug.Assert(value != _RTMP1 && value != _RTMP2 && value != _RTMP3);

            uint eldivsize = BuiltInSPEMethods.get_array_elem_len_mult((uint)arraytype);

            //Calculate the number of bytes to shift left, i.e. 16 - bytes_shift_right
            m_state.Instructions.Add(new SPEEmulator.OpCodes.sfi(_RTMP2, pointer, 0x10)); //16 - value
            
            //The instruction ignores anything but the low four bits anyway
            //m_state.Instructions.Add(new SPEEmulator.OpCodes.andi(_RTMP2, (uint)pointer.RegisterNumber, 0xfu)); //Remove anything but the low 4 bits

            uint mask;
            if (eldivsize == 0)
                mask = 0x8000;
            else if (eldivsize == 1)
                mask = 0xC000;
            else if (eldivsize == 2)
                mask = 0xf000;
            else if (eldivsize == 3)
                mask = 0xff00;
            else
                throw new InvalidProgramException();

            //Get a mask for selecting the element from the source, and rotate it into place
            m_state.Instructions.Add(new SPEEmulator.OpCodes.fsmbi(_RTMP3, mask)); //Get mask
            m_state.Instructions.Add(new SPEEmulator.OpCodes.rotqby(_RTMP3, _RTMP3, _RTMP2)); //Rotate into place

            
            //Place the new value in the slot
            if (eldivsize == 0)
            {
                m_state.Instructions.Add(new SPEEmulator.OpCodes.sfi(_RTMP2, pointer, 0x03)); //3 - Pointer
                m_state.Instructions.Add(new SPEEmulator.OpCodes.andi(_RTMP2, _RTMP2, 0x03)); //Only low 2 bits
                m_state.Instructions.Add(new SPEEmulator.OpCodes.rotqby(_RTMP0, value, _RTMP2)); //Rotate into place
                m_state.Instructions.Add(new SPEEmulator.OpCodes.and(_RTMP0, _RTMP0, _RTMP3)); //Select the portion of the value
            }
            else if (eldivsize == 1)
            {
                m_state.Instructions.Add(new SPEEmulator.OpCodes.sfi(_RTMP2, pointer, 0x02)); //2 - Pointer
                m_state.Instructions.Add(new SPEEmulator.OpCodes.andi(_RTMP2, _RTMP2, 0x02)); //Only bit at pos 1
                m_state.Instructions.Add(new SPEEmulator.OpCodes.rotqby(_RTMP0, value, _RTMP2)); //Rotate into place
                m_state.Instructions.Add(new SPEEmulator.OpCodes.and(_RTMP0, _RTMP0, _RTMP3)); //Select the portion of the value
            }
            else
            {
                m_state.Instructions.Add(new SPEEmulator.OpCodes.and(_RTMP0, value, _RTMP3)); //Select the portion of the value
            }

            //Remove the current value in the slot
            m_state.Instructions.Add(new SPEEmulator.OpCodes.xori(_RTMP3, _RTMP3, 0x3ff)); //Invert mask
            m_state.Instructions.Add(new SPEEmulator.OpCodes.lqd(_RTMP2, pointer, 0x0)); //Load current
            m_state.Instructions.Add(new SPEEmulator.OpCodes.and(_RTMP2, _RTMP2, _RTMP3)); //Mask out slot
            m_state.Instructions.Add(new SPEEmulator.OpCodes.or(_RTMP0, _RTMP0, _RTMP2)); //Merge the two values        

            //Save the value in LS
            m_state.Instructions.Add(new SPEEmulator.OpCodes.stqd(_RTMP0, pointer, 0));
        }

        public void Ldelema(InstructionElement el)
        {
            VirtualRegister elementIndex = PopStack(_RTMP0);
            VirtualRegister arrayPointer = PopStack(_RTMP1);
            VirtualRegister o = el.Register.RegisterNumber < 0 ? new TemporaryRegister(_RTMP0) : el.Register;

            if (el.Childnodes[1].StorageClass != typeof(int))
                throw new Exception("Unexpected index type: " + el.Childnodes[1].StorageClass.ToString());

            LoadElementAddress(el, new KnownObjectTypes[] { AccCIL.AccCIL.GetObjType(el.ReturnType) }, (uint)elementIndex.RegisterNumber, (uint)arrayPointer.RegisterNumber, (uint)o.RegisterNumber);
            PushStack(o);
        }

        public void Ldobj(InstructionElement el)
        {
            VirtualRegister arrayPointer = PopStack(_RTMP0);
            VirtualRegister o = el.Register.RegisterNumber < 0 ? new TemporaryRegister(_RTMP0) : el.Register;

            LoadElement(AccCIL.AccCIL.GetObjType(el.ReturnType), (uint)arrayPointer.RegisterNumber, (uint)o.RegisterNumber);

            PushStack(o);
        }

        public void Stobj(InstructionElement el)
        {
            VirtualRegister value = PopStack(_RTMP0);
            VirtualRegister pointer = PopStack(_RTMP1);

            AccCIL.KnownObjectTypes arraytype = AccCIL.AccCIL.GetObjType(el.Childnodes[0].ReturnType);

            StoreElement(arraytype, (uint)pointer.RegisterNumber, (uint)value.RegisterNumber);
        }

        public void Ldlen(InstructionElement el)
        {
            VirtualRegister arrayPointer = PopStack(_RTMP0);
            VirtualRegister o = el.Register.RegisterNumber < 0 ? new TemporaryRegister(_RTMP0) : el.Register;

            //Test that the array is not null
            //TODO: Jump to an NullException raise function
            m_state.Instructions.Add(new SPEEmulator.OpCodes.brz((uint)arrayPointer.RegisterNumber, 0));

            //Get the object table entry

            //HACK: Since the "get_array_elem_len_mult" uses _RTMP0 we use _RTMP3
            //TODO: Fix to not rely on _RTMP3 not being used by "get_array_elem_len_mult"
            m_state.Instructions.Add(new SPEEmulator.OpCodes.shli(_RTMP3, (uint)arrayPointer.RegisterNumber, 4)); //Times 16
            m_state.Instructions.Add(new SPEEmulator.OpCodes.lqd(_RTMP3, _RTMP3, SPEJITCompiler.OBJECT_TABLE_OFFSET / 16));

            if (o.RegisterNumber != _ARG0)
                PushStack(new TemporaryRegister(_ARG0));

            //Calculate the size of the element
            m_state.Instructions.Add(new SPEEmulator.OpCodes.ori(_ARG0, _RTMP3, 0));
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_RTMP4, 1)); //Signal 1 parameter for function
            m_state.RegisterCall(CompiledMethod.m_spe_builtins["get_array_elem_len_mult"]);
            m_state.Instructions.Add(new SPEEmulator.OpCodes.brasl(0, 0xffff));

            //Get the size of the array
            m_state.Instructions.Add(new SPEEmulator.OpCodes.shlqbyi(_RTMP1, _RTMP3, 0x4)); //Move word into position
            m_state.Instructions.Add(new SPEEmulator.OpCodes.sfi(_RTMP2, _ARG0, 0)); //Make two's complement
            m_state.Instructions.Add(new SPEEmulator.OpCodes.rotm((uint)o.RegisterNumber, _RTMP1, _RTMP2)); //Divide with elementsize

            if (o.RegisterNumber != _ARG0)
                PopStack(_ARG0, true);


            PushStack(o);
        }

        public void Switch(InstructionElement el)
        {
            VirtualRegister nval = PopStack(_RTMP0);
            Mono.Cecil.Cil.Instruction[] targets = (Mono.Cecil.Cil.Instruction[])el.Instruction.Operand;

            if (targets.Length == 0)
                throw new Exception("Empty switch statement?");

            //TODO: Implement with a lookup table instead, much faster

            /*if (targets.Length < 0x7fff) 
            {
                m_state.Instructions.Add(new SPEEmulator.OpCodes.cgti(_RTMP1, (uint)nval.RegisterNumber, (uint)targets.Length - 1));
            }
            else
            {
                m_state.RegisterConstantLoad((ulong)(targets.Length - 1) << 32, 0u);
                m_state.Instructions.Add(new SPEEmulator.OpCodes.lqr(_RTMP1, 0));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.cgt(_RTMP1, (uint)nval.RegisterNumber, _RTMP1));
            }

            //Too large means fall-through
            m_state.RegisterBranch(el.Instruction.Next);
            m_state.Instructions.Add(new SPEEmulator.OpCodes.brnz(_RTMP1, 0));
             
            //Within range, look in table for offset
             
            */


            //The simple implementation just uses a decrement counter 

            //Copy the value into a temp register
            if (nval.RegisterNumber != _RTMP0)
                m_state.Instructions.Add(new SPEEmulator.OpCodes.ori(_RTMP0, (uint)nval.RegisterNumber, 0));

            foreach (Mono.Cecil.Cil.Instruction t in targets)
            {
                m_state.RegisterBranch(t);
                m_state.Instructions.Add(new SPEEmulator.OpCodes.brz(_RTMP0, 0));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.ai(_RTMP0, _RTMP0, 0x3FF)); //Subtract 1
            }

        }

        public void Dup(InstructionElement el)
        {
            VirtualRegister i = PopStack(_RTMP0);
            VirtualRegister o1 = el.Register.RegisterNumber < 0 ? new TemporaryRegister(_RTMP0) : el.Register;
            VirtualRegister o2 = el.DupRegister.RegisterNumber < 0 ? new TemporaryRegister(_RTMP1) : el.DupRegister;

            if (i.RegisterNumber != o1.RegisterNumber)
                m_state.Instructions.Add(new SPEEmulator.OpCodes.ori((uint)o1.RegisterNumber, (uint)i.RegisterNumber, 0));
            if (i.RegisterNumber != o2.RegisterNumber)
                m_state.Instructions.Add(new SPEEmulator.OpCodes.ori((uint)o2.RegisterNumber, (uint)i.RegisterNumber, 0));

            PushStack(o2);
            PushStack(o1);
        }

        public void Ldstr(InstructionElement el)
        {
            VirtualRegister o = el.Register.RegisterNumber < 0 ? new TemporaryRegister(_RTMP0) : el.Register;
            m_state.RegisterStringLoad((string)el.Instruction.Operand);
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il((uint)o.RegisterNumber, 0));

            PushStack(o);
        }

        public void Box(InstructionElement el)
        {
            //Because the register allocator cannot detect that we are using $3-$6 it uses it, so
            //we need to push those arguments onto stack and pop them after the function call

            VirtualRegister o = el.Register.RegisterNumber < 0 ? new TemporaryRegister(_RTMP2) : el.Register;

            if (o.RegisterNumber != _ARG0)
                PushStack(new TemporaryRegister(_ARG0));
            PushStack(new TemporaryRegister(_ARG0 + 1));
            PushStack(new TemporaryRegister(_ARG0 + 2));
            PushStack(new TemporaryRegister(_ARG0 + 3));

            KnownObjectTypes t = AccCIL.AccCIL.GetObjType(el.Childnodes[0].ReturnType);

            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_ARG0, SPEJITCompiler.OBJECT_TABLE_INDEX)); //Object ref
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_ARG0 + 1, (uint)KnownObjectTypes.Object)); //Type
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_ARG0 + 2, 1u << (int)BuiltInSPEMethods.get_array_elem_len_mult((uint)t))); //Size

            m_state.RegisterStringLoad(el.Childnodes[0].ReturnType.FullName);
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_ARG0 + 3, 0)); //Typename
            
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_RTMP4, 4)); //Register 4 parameters
            m_state.RegisterCall(CompiledMethod.m_spe_builtins["malloc"]);
            m_state.Instructions.Add(new SPEEmulator.OpCodes.brasl(0, 0xffff));

            m_state.Instructions.Add(new SPEEmulator.OpCodes.shli(_RTMP1, _ARG0, 4));
            m_state.Instructions.Add(new SPEEmulator.OpCodes.lqd(_RTMP1, _RTMP1, SPEJITCompiler.OBJECT_TABLE_OFFSET / 16));
            m_state.Instructions.Add(new SPEEmulator.OpCodes.rotqbyi(_RTMP1, _RTMP1, 8));
            m_state.Instructions.Add(new SPEEmulator.OpCodes.ori(_RTMP2, _ARG0, 0));

            PopStack(_ARG0 + 3, true);
            PopStack(_ARG0 + 2, true);
            PopStack(_ARG0 + 1, true);
            if (o.RegisterNumber != _ARG0)
                PopStack(_ARG0, true);


            VirtualRegister value = PopStack(_RTMP0);
            m_state.Instructions.Add(new SPEEmulator.OpCodes.stqd((uint)value.RegisterNumber, _RTMP1, 0));

            if (o.RegisterNumber != _RTMP2)
                m_state.Instructions.Add(new SPEEmulator.OpCodes.ori((uint)o.RegisterNumber, _RTMP2, 0));

            PushStack(o);
        }

    }
}
