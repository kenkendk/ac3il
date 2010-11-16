using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JITManager;
using JITManager.IR;

namespace SPEJIT
{
    internal class SPEOpCodeMapper
    {
        public const uint _SP = SPEJITCompiler._SP;
        public const uint _LR = SPEJITCompiler._LR;
        public const uint _LV0 = SPEJITCompiler._LV0;

        //ABI specification states that $75 to $79 are scratch registers
        public const uint _TMP0 = SPEJITCompiler._TMP0;
        public const uint _TMP1 = SPEJITCompiler._TMP1;
        public const uint _TMP2 = SPEJITCompiler._TMP2;
        public const uint _TMP3 = SPEJITCompiler._TMP3;

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

        //NOTE: This code uses the convention that _SP points to the first unused element on the stack,
        // eg. the stack top is _SP - REGISTER_SIZE, and the next element is _SP - REGISTER_SIZE, so
        // writing a new element is always stqd(_SP, value) and then SP += REGISTER_SIZE
        //TODO: Figure out if this is ABI compatible

        /// <summary>
        /// Emits instructions for popping a value from the stack
        /// </summary>
        /// <param name="targetRegister">The register into which the value is written</param>
        public void PopStack(uint targetRegister)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.lqd(targetRegister, _SP, (uint)m_state.StackDepth));
            m_state.StackDepth--;
        }

        /// <summary>
        /// Emits instructions for pushing a value onto the stack
        /// </summary>
        /// <param name="sourceRegister"></param>
        public void PushStack(uint sourceRegister)
        {
            m_state.StackDepth++;
            m_state.Instructions.Add(new SPEEmulator.OpCodes.stqd(sourceRegister, _SP, (uint)m_state.StackDepth));
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
        /// Common template for instructions that read two values, performs an operation and writes a value
        /// </summary>
        /// <param name="i">The actual instruction</param>
        /// <returns>An instruction stream</returns>
        public void BinaryOp(SPEEmulator.OpCodes.Bases.Instruction i)
        {
            PopStack(_TMP1);
            PopStack(_TMP0);
            m_state.Instructions.Add(i);
            PushStack(_TMP0);
        }

        /// <summary>
        /// Common template for instructions that read two values, performs an operation and writes a value
        /// </summary>
        /// <param name="i">The actual instruction</param>
        /// <returns>An instruction stream</returns>
        public void BinaryOp(IEnumerable<SPEEmulator.OpCodes.Bases.Instruction> insts)
        {
            PopStack(_TMP1);
            PopStack(_TMP0);
            m_state.Instructions.AddRange(insts);
            PushStack(_TMP0);
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
            PushStack((uint)(_LV0 + m_state.Method.Method.Body.Variables.Count));
        }

        public void Ldarg_1(InstructionElement el)
        {
            PushStack((uint)(_LV0 + m_state.Method.Method.Body.Variables.Count + 1));
        }

        public void Ldarg_2(InstructionElement el)
        {
            PushStack((uint)(_LV0 + m_state.Method.Method.Body.Variables.Count + 2));
        }

        public void Ldarg_3(InstructionElement el)
        {
            PushStack((uint)(_LV0 + m_state.Method.Method.Body.Variables.Count + 3));
        }

        public void Ldarg_s(InstructionElement el)
        {
            int index = el.ParentMethod.Parameters.IndexOf(((Mono.Cecil.ParameterDefinition)el.Instruction.Operand));
            PushStack((uint)(_LV0 + m_state.Method.Method.Body.Variables.Count + index));
        }

        public void Ldarg(InstructionElement el)
        {
            int index = el.ParentMethod.Parameters.IndexOf(((Mono.Cecil.ParameterDefinition)el.Instruction.Operand));
            PushStack((uint)(_LV0 + m_state.Method.Method.Body.Variables.Count + index));
        }

        public void Starg_s(InstructionElement el)
        {
            int index = el.ParentMethod.Parameters.IndexOf(((Mono.Cecil.ParameterDefinition)el.Instruction.Operand));
            PopStack((uint)(_LV0 + m_state.Method.Method.Body.Variables.Count + index));
        }

        public void Starg(InstructionElement el)
        {
            int index = el.ParentMethod.Parameters.IndexOf(((Mono.Cecil.ParameterDefinition)el.Instruction.Operand));
            PopStack((uint)(_LV0 + m_state.Method.Method.Body.Variables.Count + index));
        }

        public void Ldc_I4_0(InstructionElement el)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_TMP0, 0));
            PushStack(_TMP0);
        }

        public void Ldc_I4_1(InstructionElement el)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_TMP0, 1));
            PushStack(_TMP0);
        }

        public void Ldc_I4_2(InstructionElement el)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_TMP0, 2));
            PushStack(_TMP0);
        }

        public void Ldc_I4_3(InstructionElement el)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_TMP0, 3));
            PushStack(_TMP0);
        }

        public void Ldc_I4_4(InstructionElement el)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_TMP0, 4));
            PushStack(_TMP0);
        }

        public void Ldc_I4_5(InstructionElement el)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_TMP0, 5));
            PushStack(_TMP0);
        }

        public void Ldc_I4_6(InstructionElement el)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_TMP0, 6));
            PushStack(_TMP0);
        }

        public void Ldc_I4_7(InstructionElement el)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_TMP0, 7));
            PushStack(_TMP0);
        }

        public void Ldc_I4_8(InstructionElement el)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_TMP0, 8));
            PushStack(_TMP0);
        }

        public void Ldc_i4_m1(InstructionElement el)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_TMP0,  0xffff));
            PushStack(_TMP0);
        }

        public void Ldc_I4(InstructionElement el)
        {
            //TODO: Negative values can be loaded more efficiently, if they are < 0xffff
            uint opr = (uint)(int)el.Instruction.Operand;
            if (opr < 0x7fff)
                m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_TMP0, (uint)opr));
            else if (opr < 0x40000)
                m_state.Instructions.Add(new SPEEmulator.OpCodes.ila(_TMP0, (uint)opr));
            else
            {
                ulong value = (ulong)((int) opr);
                m_state.RegisterConstantLoad((value << 32) | (value & 0xffffffff), (value << 32) | (value & 0xffffffff));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.lqr(_TMP0, 0));
            }
            PushStack(_TMP0);
        }

        public void Ldc_I8(InstructionElement el)
        {
            ulong opr = (ulong)(long)el.Instruction.Operand;
            m_state.RegisterConstantLoad((ulong)opr, opr);
            m_state.Instructions.Add(new SPEEmulator.OpCodes.lqr(_TMP0, 0));
            PushStack(_TMP0);
        }

        public void Ldc_I4_S(InstructionElement el)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_TMP0, ((uint)(sbyte)el.Instruction.Operand) & 0xffff));
            PushStack(_TMP0);
        }

        public void Conv_R4(InstructionElement el)
        {
            if (el.Childnodes == null || el.Childnodes.Length != 1)
                throw new InvalidProgramException();
            if (el.Childnodes[0].StorageClass == typeof(float))
                return;
            else if (el.Childnodes[0].StorageClass == typeof(int))
            {
                PopStack(_TMP0);
                m_state.Instructions.Add(new SPEEmulator.OpCodes.csflt(_TMP0, _TMP0, 0));
                PushStack(_TMP0);
            }
            else if (el.Childnodes[0].StorageClass == typeof(long))
            {
                throw new MissingMethodException();
            }
            else if (el.Childnodes[0].StorageClass == typeof(double))
            {
                PopStack(_TMP0);
                m_state.Instructions.Add(new SPEEmulator.OpCodes.frds(_TMP0, _TMP0));
                PushStack(_TMP0);
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
                PopStack(_TMP0);
                m_state.Instructions.Add(new SPEEmulator.OpCodes.csflt(_TMP0, _TMP0, 0));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.fesd(_TMP0, _TMP0));
                PushStack(_TMP0);
            }
            else if (el.Childnodes[0].StorageClass == typeof(long))
            {
                throw new MissingMethodException();
            }
            else if (el.Childnodes[0].StorageClass == typeof(float))
            {
                PopStack(_TMP0);
                m_state.Instructions.Add(new SPEEmulator.OpCodes.fesd(_TMP0, _TMP0));
                PushStack(_TMP0);
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
                PopStack(_TMP0);
                m_state.Instructions.Add(new SPEEmulator.OpCodes.xswd(_TMP0, _TMP0));
                PushStack(_TMP0);
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
                PopStack(_TMP0);
                m_state.RegisterConstantLoad(0x0405060704050607, 0x0405060704050607); //This mask loads the lower word from the prefered doubleword slot
                m_state.Instructions.Add(new SPEEmulator.OpCodes.lqr(_TMP1, 0));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.shufb(_TMP0, _TMP0, _TMP0, _TMP1));
                PushStack(_TMP0);
            }
            else if (el.Childnodes[0].StorageClass == typeof(float))
            {
                PopStack(_TMP0);
                m_state.Instructions.Add(new SPEEmulator.OpCodes.cflts(_TMP0, _TMP0, 0));
                PushStack(_TMP0);
            }
            else if (el.Childnodes[0].StorageClass == typeof(float))
            {
                PopStack(_TMP0);
                m_state.Instructions.Add(new SPEEmulator.OpCodes.frds(_TMP0, _TMP0)); //Convert to float
                m_state.Instructions.Add(new SPEEmulator.OpCodes.cflts(_TMP0, _TMP0, 0)); //Convert to int
                PushStack(_TMP0);
            }
        }

        public void Conv_U4(InstructionElement el)
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
                PopStack(_TMP0);
                //According to ECMA specs i4 -> u8 is always zero extended: http://jilc.sourceforge.net/ecma_p3_cil.shtml#Table7ConversionOperations
                m_state.RegisterConstantLoad(0x8080808000010203, 0x8080808000010203);
                m_state.Instructions.Add(new SPEEmulator.OpCodes.lqr(_TMP1, 0));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.shufb(_TMP0, _TMP0, _TMP0, _TMP1));
                PushStack(_TMP0);
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

        private Type ValidateBinaryOp(InstructionElement el)
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
            else
                throw new InvalidProgramException("Binary Op for <" + el.Childnodes[0].StorageClass + "> ?");
        }

        public void Not(InstructionElement el)
        {
            PopStack(_TMP0);
            m_state.Instructions.Add(new SPEEmulator.OpCodes.xori(_TMP0, _TMP0, 0x3ff));
            PushStack(_TMP0);
        }

        public void Clt(InstructionElement el)
        {
            Type t = ValidateBinaryOp(el);

            //There are no clt instructions, so we use "not (cgt or ceq)"

            if (t == typeof(int))
                BinaryOp(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.cgt(_TMP0, _TMP0, _TMP1), //Greater than
                    new SPEEmulator.OpCodes.ceq(_TMP1, _TMP0, _TMP1), //Equal
                    new SPEEmulator.OpCodes.or(_TMP0, _TMP0, _TMP1), //Greater than _or_ equal
                    new SPEEmulator.OpCodes.xori(_TMP0, _TMP0, 0x3ff), //Invert bitmask (not)
                    new SPEEmulator.OpCodes.il(_TMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            else if (t == typeof(float))
                BinaryOp(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.fcgt(_TMP0, _TMP0, _TMP1), //Greater than
                    new SPEEmulator.OpCodes.fceq(_TMP1, _TMP0, _TMP1), //Equal
                    new SPEEmulator.OpCodes.or(_TMP0, _TMP0, _TMP1), //Greater than _or_ equal
                    new SPEEmulator.OpCodes.xori(_TMP0, _TMP0, 0x3ff), //Invert bitmask (not)
                    new SPEEmulator.OpCodes.il(_TMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            else if (t == typeof(long))
            {
                BinaryOp(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    //Part1: cgt
                    new SPEEmulator.OpCodes.ceq(_TMP3, _TMP0, _TMP1), //This will compare words, giving 0xffffffff for equal than and 0 otherwise
                    new SPEEmulator.OpCodes.clgt(_TMP2, _TMP0, _TMP1), //This will compare words, giving 0xffffffff for logically greater than and 0 otherwise
                    new SPEEmulator.OpCodes.rotqbyi(_TMP2, _TMP2, 4), //Prepare the tmp3 by an 8 byte rotate
                    new SPEEmulator.OpCodes.and(_TMP3, _TMP2, _TMP3), //And the results so we disregard the lower word unless the upper words are equal
                    new SPEEmulator.OpCodes.cgt(_TMP2, _TMP0, _TMP1), //This will compare words, giving 0xffffffff for greater than and 0 otherwise
                    new SPEEmulator.OpCodes.or(_TMP2, _TMP2, _TMP3), //Or the results so prefered word slot is either 0 or 0xffffffff

                    //Part2: ceq
                    new SPEEmulator.OpCodes.ceq(_TMP1, _TMP0, _TMP1), //This will compare words, giving 0xffffffff for greater than and 0 otherwise
                    new SPEEmulator.OpCodes.rotqbyi(_TMP3, _TMP1, 4), //Prepare the tmp1 by an 8 byte rotate
                    new SPEEmulator.OpCodes.and(_TMP1, _TMP1, _TMP3), //And the results so prefered word slot is either 0 or 0xffffffff

                    //Combine
                    new SPEEmulator.OpCodes.or(_TMP0, _TMP2, _TMP1), //Greater than _or_ equal
                    new SPEEmulator.OpCodes.xori(_TMP0, _TMP0, 0x3ff), //Invert bitmask (not)

                    //Mask
                    new SPEEmulator.OpCodes.il(_TMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            }
            else if (t == typeof(double))
                BinaryOp(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.dfcgt(_TMP0, _TMP0, _TMP1), //Greater than
                    new SPEEmulator.OpCodes.dfceq(_TMP1, _TMP0, _TMP1), //Equal
                    new SPEEmulator.OpCodes.or(_TMP0, _TMP0, _TMP1), //Greater than _or_ equal
                    new SPEEmulator.OpCodes.xori(_TMP0, _TMP0, 0x3ff), //Invert bitmask (not)
                    new SPEEmulator.OpCodes.il(_TMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            else
                throw new InvalidProgramException("clt for <" + el.Childnodes[0].StorageClass + "> ?");
        }

        public void Clt_un(InstructionElement el)
        {
            Type t = ValidateBinaryOp(el);

            //There are no clt_un instructions, so we use "not (clgt or ceq)"

            if (t == typeof(int))
                BinaryOp(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.clgt(_TMP0, _TMP0, _TMP1), //Greater than
                    new SPEEmulator.OpCodes.ceq(_TMP1, _TMP0, _TMP1), //Equal
                    new SPEEmulator.OpCodes.or(_TMP0, _TMP0, _TMP1), //Greater than _or_ equal
                    new SPEEmulator.OpCodes.xori(_TMP0, _TMP0, 0x3ff), //Invert bitmask (not)
                    new SPEEmulator.OpCodes.il(_TMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            else if (t == typeof(float))
                throw new MissingMethodException("Floating unordered?");
            else if (t == typeof(long))
            {
                BinaryOp(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    //Part1: cgt
                    new SPEEmulator.OpCodes.ceq(_TMP3, _TMP0, _TMP1), //This will compare words, giving 0xffffffff for equal than and 0 otherwise
                    new SPEEmulator.OpCodes.clgt(_TMP2, _TMP0, _TMP1), //This will compare words, giving 0xffffffff for logically greater than and 0 otherwise
                    new SPEEmulator.OpCodes.rotqbyi(_TMP2, _TMP2, 4), //Prepare the tmp3 by an 8 byte rotate
                    new SPEEmulator.OpCodes.and(_TMP3, _TMP2, _TMP3), //And the results so we disregard the lower word unless the upper words are equal
                    new SPEEmulator.OpCodes.clgt(_TMP2, _TMP0, _TMP1), //This will compare words, giving 0xffffffff for greater than and 0 otherwise
                    new SPEEmulator.OpCodes.or(_TMP2, _TMP2, _TMP3), //Or the results so prefered word slot is either 0 or 0xffffffff

                    //Part2: ceq
                    new SPEEmulator.OpCodes.ceq(_TMP1, _TMP0, _TMP1), //This will compare words, giving 0xffffffff for greater than and 0 otherwise
                    new SPEEmulator.OpCodes.rotqbyi(_TMP3, _TMP1, 4), //Prepare the tmp1 by an 8 byte rotate
                    new SPEEmulator.OpCodes.and(_TMP1, _TMP1, _TMP3), //And the results so prefered word slot is either 0 or 0xffffffff

                    //Combine
                    new SPEEmulator.OpCodes.or(_TMP0, _TMP2, _TMP1), //Greater than _or_ equal
                    new SPEEmulator.OpCodes.xori(_TMP0, _TMP0, 0x3ff), //Invert bitmask (not)

                    //Mask
                    new SPEEmulator.OpCodes.il(_TMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            }
            else if (t == typeof(double))
                throw new MissingMethodException("Double floating unordered?");
            else
                throw new InvalidProgramException("clt for <" + el.Childnodes[0].StorageClass + "> ?");
        }


        public void Cgt(InstructionElement el)
        {
            Type t = ValidateBinaryOp(el);

            if (t == typeof(int))
                BinaryOp(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.cgt(_TMP0, _TMP0, _TMP1),
                    new SPEEmulator.OpCodes.il(_TMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            else if (t == typeof(float))
                BinaryOp(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.fcgt(_TMP0, _TMP0, _TMP1),
                    new SPEEmulator.OpCodes.il(_TMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            else if (t == typeof(long))
            {
                //There is no cgtd, so we make our own
                BinaryOp(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.ceq(_TMP3, _TMP0, _TMP1), //This will compare words, giving 0xffffffff for equal than and 0 otherwise
                    new SPEEmulator.OpCodes.clgt(_TMP2, _TMP0, _TMP1), //This will compare words, giving 0xffffffff for logically greater than and 0 otherwise
                    new SPEEmulator.OpCodes.rotqbyi(_TMP2, _TMP2, 4), //Prepare the tmp3 by an 8 byte rotate
                    new SPEEmulator.OpCodes.and(_TMP3, _TMP2, _TMP3), //And the results so we disregard the lower word unless the upper words are equal
                    new SPEEmulator.OpCodes.cgt(_TMP0, _TMP0, _TMP1), //This will compare words, giving 0xffffffff for greater than and 0 otherwise
                    new SPEEmulator.OpCodes.or(_TMP0, _TMP0, _TMP3), //Or the results so prefered word slot is either 0 or 0xffffffff
                    new SPEEmulator.OpCodes.il(_TMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            }
            else if (t == typeof(double))
                BinaryOp(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.dfcgt(_TMP0, _TMP0, _TMP1),
                    new SPEEmulator.OpCodes.il(_TMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            else
                throw new InvalidProgramException("cgt for <" + el.Childnodes[0].StorageClass + "> ?");
        }

        public void Cgt_un(InstructionElement el)
        {
            Type t = ValidateBinaryOp(el);

            if (t == typeof(int))
                BinaryOp(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.clgt(_TMP0, _TMP0, _TMP1),
                    new SPEEmulator.OpCodes.il(_TMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            else if (t == typeof(float))
                throw new MissingMethodException("Floating unordered?");
                /*BinaryOp(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.fcgt(_TMP0, _TMP0, _TMP1),
                    new SPEEmulator.OpCodes.il(_TMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });*/
            else if (t == typeof(long))
            {
                //There is no cgtd, so we make our own
                BinaryOp(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.ceq(_TMP3, _TMP0, _TMP1), //This will compare words, giving 0xffffffff for equal than and 0 otherwise
                    new SPEEmulator.OpCodes.clgt(_TMP2, _TMP0, _TMP1), //This will compare words, giving 0xffffffff for logically greater than and 0 otherwise
                    new SPEEmulator.OpCodes.rotqbyi(_TMP2, _TMP2, 4), //Prepare the tmp3 by an 8 byte rotate
                    new SPEEmulator.OpCodes.and(_TMP3, _TMP2, _TMP3), //And the results so we disregard the lower word unless the upper words are equal
                    new SPEEmulator.OpCodes.clgt(_TMP0, _TMP0, _TMP1), //This will compare words, giving 0xffffffff for greater than and 0 otherwise
                    new SPEEmulator.OpCodes.or(_TMP0, _TMP0, _TMP3), //Or the results so prefered word slot is either 0 or 0xffffffff
                    new SPEEmulator.OpCodes.il(_TMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            }
            else if (t == typeof(double))
                throw new MissingMethodException("Double floating unordered?");
                /*BinaryOp(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.dfcgt(_TMP0, _TMP0, _TMP1),
                    new SPEEmulator.OpCodes.il(_TMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });*/
            else
                throw new InvalidProgramException("cgt_un for <" + el.Childnodes[0].StorageClass + "> ?");
        }

        public void Ceq(InstructionElement el)
        {
            Type t = ValidateBinaryOp(el);

            if (t == typeof(int))
                BinaryOp(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.ceq(_TMP0, _TMP0, _TMP1),
                    new SPEEmulator.OpCodes.il(_TMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            else if (t == typeof(float))
                BinaryOp(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.fceq(_TMP0, _TMP0, _TMP1),
                    new SPEEmulator.OpCodes.il(_TMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            else if (t == typeof(long))
            {
                //There is no ceqd, so we make our own
                BinaryOp(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.ceq(_TMP0, _TMP0, _TMP1), //This will compare words, giving 0 for equal and 0xffffffff otherwise
                    new SPEEmulator.OpCodes.rotqbyi(_TMP1, _TMP0, 4), //Prepare the tmp1 by an 8 byte rotate
                    new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1), //Or the results so prefered word slot is either 0 or 0xffffffff
                    new SPEEmulator.OpCodes.il(_TMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1), //Make sure that the result is a word of either 0 (false) or 1 (true)
                });
            }
            else if (t == typeof(double))
                BinaryOp(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.dfceq(_TMP0, _TMP0, _TMP1),
                    new SPEEmulator.OpCodes.il(_TMP1, 0x1), //Load a 0 or 1 mask
                    new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1), //Make sure that the result is a word of either 0 or 1
                });
            else
                throw new InvalidProgramException("ceq for <" + el.Childnodes[0].StorageClass + "> ?");
        }

        public void Stloc_0(InstructionElement el)
        {
            PopStack(_LV0);
        }

        public void Stloc_1(InstructionElement el)
        {
            PopStack(_LV0 + 1u);
        }

        public void Stloc_2(InstructionElement el)
        {
            PopStack(_LV0 + 2u);
        }

        public void Stloc_3(InstructionElement el)
        {
            PopStack(_LV0 + 3u);
        }

        public void Stloc_s(InstructionElement el)
        {
            PopStack(_LV0 + (uint)((Mono.Cecil.Cil.VariableReference)el.Instruction.Operand).Index);
        }

        public void Ldloc_0(InstructionElement el)
        {
            PushStack(_LV0);
        }

        public void Ldloc_1(InstructionElement el)
        {
            PushStack(_LV0 + 1u);
        }

        public void Ldloc_2(InstructionElement el)
        {
            PushStack(_LV0 + 2u);
        }

        public void Ldloc_3(InstructionElement el)
        {
            PushStack(_LV0 + 3u);
        }

        public void Ldloc_s(InstructionElement el)
        {
            PushStack(_LV0 + (uint)((Mono.Cecil.Cil.VariableReference)el.Instruction.Operand).Index);
        }

        public void Brtrue_S(InstructionElement el)
        {
            PopStack(_TMP0);
            m_state.RegisterBranch(((Mono.Cecil.Cil.Instruction)el.Instruction.Operand));
            m_state.Instructions.Add(new SPEEmulator.OpCodes.brnz(_TMP0, 0xffff));
        }

        public void Br_S(InstructionElement el)
        {
            m_state.RegisterBranch(((Mono.Cecil.Cil.Instruction)el.Instruction.Operand));
            m_state.Instructions.Add(new SPEEmulator.OpCodes.br(_TMP0, 0xffff));
        }

        public void Bne_un_s(InstructionElement el)
        {
            PopStack(_TMP0);
            PopStack(_TMP1);
            m_state.Instructions.Add(new SPEEmulator.OpCodes.cgt(_TMP0, _TMP0, _TMP1));

            m_state.RegisterBranch(((Mono.Cecil.Cil.Instruction)el.Instruction.Operand));
            m_state.Instructions.Add(new SPEEmulator.OpCodes.brz(_TMP0, 0xffff));
        }

        public void Sub(InstructionElement el)
        {
            Type t = ValidateBinaryOp(el);

            if (t == typeof(int))
                BinaryOp(new SPEEmulator.OpCodes.sf(_TMP0, _TMP1, _TMP0));
            else if (t == typeof(float))
                BinaryOp(new SPEEmulator.OpCodes.fs(_TMP0, _TMP1, _TMP0));
            else if (t == typeof(double))
                BinaryOp(new SPEEmulator.OpCodes.dfs(_TMP0, _TMP1, _TMP0));
            else if (t == typeof(long))
            {
                //We have no subfd, so we make one ourselves

                PopStack(_TMP1);
                PopStack(_TMP0);

                m_state.RegisterConstantLoad(0x04050607c0c0c0c0, 0x0c0d0e0fc0c0c0c0);

                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.lqr(_TMP3, 0), //Load substract mask
                    new SPEEmulator.OpCodes.bg(_TMP2, _TMP1, _TMP0), //Calculate borrow
                    new SPEEmulator.OpCodes.shufb(_TMP2, _TMP2, _TMP2, _TMP3), //Move the borrow into the right place
                    new SPEEmulator.OpCodes.sfx(_TMP2, _TMP1, _TMP0), //Subtract the two dwords
                });

                PushStack(_TMP2);
            }
            else
                throw new InvalidProgramException();
        }

        public void Mul(InstructionElement el)
        {
            Type t = ValidateBinaryOp(el);

            if (t == typeof(int))
            {
                //Unfortunately, the SPU favors 16bit integer multiply, so this block emulates 32bit multiply
                BinaryOp(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.mpyh(_TMP3, _TMP1, _TMP0),
                    new SPEEmulator.OpCodes.mpyh(_TMP2, _TMP0, _TMP1),
                    new SPEEmulator.OpCodes.mpyu(_TMP1, _TMP1, _TMP0),
                    new SPEEmulator.OpCodes.a(_TMP0, _TMP3, _TMP2),
                    new SPEEmulator.OpCodes.a(_TMP0, _TMP0, _TMP1)
                });
            }
            else if (t == typeof(float))
                BinaryOp(new SPEEmulator.OpCodes.fm(_TMP0, _TMP1, _TMP0));
            else if (t == typeof(double))
                BinaryOp(new SPEEmulator.OpCodes.dfm(_TMP0, _TMP1, _TMP0));
            else if (t == typeof(long))
            {
                //64bit multiply is emulated with a function call
                PopStack(_ARG0 + 1);
                PopStack(_ARG0);

                m_state.RegisterCall(CompiledMethod.m_builtins["umul"]);
                m_state.Instructions.Add(new SPEEmulator.OpCodes.brsl(0, 0xffff));

                PushStack(_ARG0);
            }
            else
                throw new InvalidProgramException();
        }

        public void And(InstructionElement el)
        {
            BinaryOp(new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP1));
        }

        public void Or(InstructionElement el)
        {
            BinaryOp(new SPEEmulator.OpCodes.or(_TMP0, _TMP0, _TMP1));
        }

        public void Shl(InstructionElement el)
        {
            if (el.Childnodes == null || el.Childnodes.Length != 2)
                throw new InvalidProgramException();

            Type t = el.Childnodes[0].StorageClass;
            Type bitCounter = el.Childnodes[1].StorageClass;

            PopStack(_TMP1);
            PopStack(_TMP0);

            if (bitCounter == typeof(long))
            {
                //We discard the high word, and rotate the low word into the preferred slot
                m_state.Instructions.Add(new SPEEmulator.OpCodes.rotqbyi(_TMP1, _TMP1, 4));
            }
            else if (bitCounter != typeof(int))
                throw new InvalidProgramException("Cannot shl with type " + el.Childnodes[1].ReturnType.ToString());

            if (t == typeof(int))
            {
                m_state.Instructions.Add(new SPEEmulator.OpCodes.shl(_TMP0, _TMP0, _TMP1));
            }
            else if (t == typeof(long))
            {
                //We need to do this with each dword, so we mask the low dword for _TMP2
                m_state.Instructions.Add(new SPEEmulator.OpCodes.fsmbi(_TMP2, 0xff00));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.and(_TMP2, _TMP2, _TMP0));

                //Perform shl on both high and low dword for bits, eg modulo 8
                m_state.Instructions.Add(new SPEEmulator.OpCodes.shlqbi(_TMP0, _TMP0, _TMP1));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.shlqbi(_TMP2, _TMP2, _TMP1));

                //Calculate the number of bytes to shift
                m_state.Instructions.Add(new SPEEmulator.OpCodes.rotmi(_TMP1, _TMP1, ((-3) & 0x7F)));

                //Perform shl on both high and low dword for bytes
                m_state.Instructions.Add(new SPEEmulator.OpCodes.shlqby(_TMP0, _TMP0, _TMP1));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.shlqby(_TMP2, _TMP2, _TMP1));

                //Mask out any bits that are shifted from the low dword into the high dword
                m_state.Instructions.Add(new SPEEmulator.OpCodes.fsmbi(_TMP3, 0x00ff));
                m_state.Instructions.Add(new SPEEmulator.OpCodes.and(_TMP0, _TMP0, _TMP3));

                //Combine the two dwords
                m_state.Instructions.Add(new SPEEmulator.OpCodes.or(_TMP0, _TMP0, _TMP2));

            }
            else
                throw new InvalidProgramException();

            PushStack(_TMP0);
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
            PopStack(_TMP1);
            PopStack(_TMP0);

            if (el.Childnodes == null || el.Childnodes.Length != 2)
                throw new InvalidProgramException();

            Type t = el.Childnodes[0].StorageClass;
            Type bitCounter = el.Childnodes[1].StorageClass;

            if (bitCounter == typeof(long))
            {
                //We discard the high word, and rotate the low word into the preferred slot
                m_state.Instructions.Add(new SPEEmulator.OpCodes.rotqbyi(_TMP1, _TMP1, 4));
            }
            else if (bitCounter != typeof(int))
                throw new InvalidProgramException("Cannot shr_un with type " + el.Childnodes[1].ReturnType.ToString());


            if (t == typeof(int))
            {
                m_state.Instructions.Add(new SPEEmulator.OpCodes.sfi(_TMP1, _TMP1, 0)); //Form 2's complement mask
                if (el.Instruction.OpCode.Code == Mono.Cecil.Cil.Code.Shr_Un)
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.rotm(_TMP0, _TMP0, _TMP1));
                else
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.rotma(_TMP0, _TMP0, _TMP1));
            }
            else if (t == typeof(long))
            {
                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.ori(_TMP2, _TMP1, 0),
                    new SPEEmulator.OpCodes.rotmi(_TMP1, _TMP1, (uint)((-3) & 0x7f)),
                    new SPEEmulator.OpCodes.sfi(_TMP1, _TMP1, 0), //Form 2's complement mask for byte count
                    new SPEEmulator.OpCodes.sfi(_TMP2, _TMP2, 0), //Form 2's complement mask for bit count
                });

                if (el.Instruction.OpCode.Code == Mono.Cecil.Cil.Code.Shr_Un)
                {
                    //Mask out the high dword so it doesn't flow into the lower dword
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.fsmbi(_TMP3, 0x00ff));
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.and(_TMP3, _TMP0, _TMP3));
                    
                    //Rotate the high dword
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.rotqmby(_TMP0, _TMP0, _TMP1));
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.rotqmbi(_TMP0, _TMP0, _TMP2));

                    //Rotate the low dword
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.rotqmby(_TMP3, _TMP3, _TMP1));
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.rotqmbi(_TMP3, _TMP3, _TMP2));

                    //Mask out any overflow from the high dword rotate
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.fsmbi(_TMP2, 0xff00));
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.and(_TMP2, _TMP2, _TMP0));

                    //Combine the two results
                    m_state.Instructions.Add(new SPEEmulator.OpCodes.or(_TMP0, _TMP3, _TMP0));

                }
                else
                {
                    throw new MissingMethodException("64bit signed shr is not supported");
                }
            }
            else
                throw new InvalidProgramException();

            PushStack(_TMP0);
        }

        public void Add(InstructionElement el)
        {
            Type t = ValidateBinaryOp(el);

            if (t == typeof(int))
                BinaryOp(new SPEEmulator.OpCodes.a(_TMP0, _TMP1, _TMP0));
            else if (t == typeof(float))
                BinaryOp(new SPEEmulator.OpCodes.fa(_TMP0, _TMP1, _TMP0));
            else if (t == typeof(double))
                BinaryOp(new SPEEmulator.OpCodes.dfa(_TMP0, _TMP1, _TMP0));
            else if (t == typeof(long))
            {
                //We have no addd, so we make one ourselves
                PopStack(_TMP1);
                PopStack(_TMP0);

                m_state.RegisterConstantLoad(0x0405060780808080, 0x0c0d0e0f80808080);

                m_state.Instructions.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] {
                    new SPEEmulator.OpCodes.lqr(_TMP3, 0), //Load add mask
                    new SPEEmulator.OpCodes.cg(_TMP2, _TMP0, _TMP1), //Calculate carry
                    new SPEEmulator.OpCodes.shufb(_TMP2, _TMP2, _TMP2, _TMP3), //Rotate the carry to the far right
                    new SPEEmulator.OpCodes.addx(_TMP2, _TMP0, _TMP1), //Add the two words
                });

                PushStack(_TMP2);
            }
            else
                throw new InvalidProgramException();
        }

        public void Call(InstructionElement el)
        {
            Mono.Cecil.MethodReference mdef = (Mono.Cecil.MethodReference)el.Instruction.Operand;

            if (mdef.Parameters.Count > MAX_FUCTION_ARGUMENTS)
                throw new Exception("Too many arguments ");

            //Pop all required registers off stack and place them in call registers
            uint register = (uint)((_ARG0 + mdef.Parameters.Count) - 1);
            for (int i = 0; i < mdef.Parameters.Count; i++)
                PopStack((uint)(register - i));

            m_state.RegisterCall(mdef);
            // i16 (set to 0xffff) should be replaced with correct value, when it is known!
            m_state.Instructions.Add(new SPEEmulator.OpCodes.brsl(0, 0xffff));

            if (mdef.ReturnType.ReturnType.FullName != "System.Void")
                PushStack(_ARG0);
        }

        public void Ret(InstructionElement el)
        {
            m_state.RegisterReturn();
            m_state.Instructions.Add(new SPEEmulator.OpCodes.br(_TMP0, 0xffff));
        }

    }
}
