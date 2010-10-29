using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPEJIT
{
    internal class SPEOpCodeMapper
    {
        public const uint _SP = SPEJIT._SP;
        public const uint _LR = SPEJIT._LR;
        public const uint _LV0 = SPEJIT._LV0;

        //ABI specification states that $75 to $79 are scratch registers
        public const uint _TMP0 = SPEJIT._TMP0;
        public const uint _TMP1 = SPEJIT._TMP1;

        /// <summary>
        /// The inverse value of register size, used in add operations
        /// </summary>
        public const uint REGISTER_SIZE_NEGATED = (-REGISTERS_SIZE & 0x3ff);

        /// <summary>
        /// The register used for the first argument
        /// </summary>
        public const uint _ARG0 = SPEJIT._ARG0;

        /// <summary>
        /// The max allowed number of function arguments
        /// </summary>
        public const int MAX_FUCTION_ARGUMENTS = 74 - 3;

        /// <summary>
        /// The size of a register in bytes when placed on stack
        /// </summary>
        public const int REGISTERS_SIZE = SPEJIT.REGISTER_SIZE;

        //NOTE: This code uses the convention that _SP points to the first unused element on the stack,
        // eg. the stack top is _SP - REGISTERS_SIZE, and the next element is _SP - REGISTERS_SIZE, so
        // writing a new element is always stqd(_SP, value) and then SP += REGISTERS_SIZE
        //TODO: Figure out if this is ABI compatible

        /// <summary>
        /// Emits instructions for popping a value from the stack
        /// </summary>
        /// <param name="targetRegister">The register into which the value is written</param>
        public void PopStack(uint targetRegister)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.lqd(targetRegister, _SP, REGISTER_SIZE_NEGATED));
            m_state.Instructions.Add(new SPEEmulator.OpCodes.ai(_SP, _SP, REGISTER_SIZE_NEGATED));
            m_state.StackDepth--;
        }

        /// <summary>
        /// Emits instructions for pushing a value onto the stack
        /// </summary>
        /// <param name="sourceRegister"></param>
        public void PushStack(uint sourceRegister)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.stqd(sourceRegister, _SP, 0));
            m_state.Instructions.Add(new SPEEmulator.OpCodes.ai(_SP, _SP, REGISTERS_SIZE));
            m_state.StackDepth++;
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
        /// Common instruction for copying the contents of one register into another
        /// </summary>
        /// <param name="sourceRegister">The register to copy from</param>
        /// <param name="targetRegister">The register to copy to</param>
        public void CopyRegister(uint sourceRegister, uint targetRegister)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.ai(targetRegister, sourceRegister, 0));
        }

        /// <summary>
        /// The JIT state manager
        /// </summary>
        private CompiledMethod m_state;

        public SPEOpCodeMapper(CompiledMethod state)
        {
            m_state = state;
        }

        public void nop(IR.InstructionElement el)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.nop(0));
        }


        public void Ldarg_0(IR.InstructionElement el)
        {
            PushStack((uint)(_LV0 + m_state.Method.Method.Body.Variables.Count));
        }

        public void Ldarg_1(IR.InstructionElement el)
        {
            PushStack((uint)(_LV0 + m_state.Method.Method.Body.Variables.Count + 1));
        }

        public void Ldc_I4_0(IR.InstructionElement el)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_TMP0, 0));
            PushStack(_TMP0);
        }

        public void Ldc_I4_1(IR.InstructionElement el)
        {
            m_state.Instructions.Add(new SPEEmulator.OpCodes.il(_TMP0, 1));
            PushStack(_TMP0);
        }

        public void Conv_I8(IR.InstructionElement el)
        {
            PopStack(_TMP0);
            m_state.Instructions.Add(new SPEEmulator.OpCodes.xswd(_TMP0, _TMP0));
            PushStack(_TMP0);
        }

        public void Ceq(IR.InstructionElement el)
        {
            BinaryOp(new SPEEmulator.OpCodes.ceq(_TMP0, _TMP0, _TMP1));
        }

        public void Stloc_0(IR.InstructionElement el)
        {
            PopStack(_LV0);
        }

        public void Stloc_1(IR.InstructionElement el)
        {
            PopStack(_LV0 + 1u);
        }

        public void Ldloc_0(IR.InstructionElement el)
        {
            PushStack(_LV0);
        }

        public void Ldloc_1(IR.InstructionElement el)
        {
            PushStack(_LV0 + 1u);
        }

        public void Brtrue_S(IR.InstructionElement el)
        {
            PopStack(_TMP0);
            m_state.RegisterBranch(((Mono.Cecil.Cil.Instruction)el.Instruction.Operand));
            m_state.Instructions.Add(new SPEEmulator.OpCodes.brz(_TMP0, 0xffff));
        }

        public void Br_S(IR.InstructionElement el)
        {
            PopStack(_TMP0);
            m_state.RegisterBranch(((Mono.Cecil.Cil.Instruction)el.Instruction.Operand));
            m_state.Instructions.Add(new SPEEmulator.OpCodes.br(_TMP0, 0xffff));
        }

        public void Bne_un_s(IR.InstructionElement el)
        {
            PopStack(_TMP0);
            PopStack(_TMP1);
            m_state.Instructions.Add(new SPEEmulator.OpCodes.cgt(_TMP0, _TMP0, _TMP1));

            m_state.RegisterBranch(((Mono.Cecil.Cil.Instruction)el.Instruction.Operand));
            m_state.Instructions.Add(new SPEEmulator.OpCodes.brz(_TMP0, 0xffff));
        }

        public void Sub(IR.InstructionElement el)
        {
            BinaryOp(new SPEEmulator.OpCodes.sf(_TMP0, _TMP1, _TMP0));
        }

        public void Mul(IR.InstructionElement el)
        {
            BinaryOp(new SPEEmulator.OpCodes.mpy(_TMP0, _TMP1, _TMP0));
        }

        public void Call(IR.InstructionElement el)
        {
            Mono.Cecil.MethodDefinition mdef = (Mono.Cecil.MethodDefinition)el.Instruction.Operand;

            if (mdef.Parameters.Count > MAX_FUCTION_ARGUMENTS)
                throw new Exception("Too many arguments ");

            //Pop all required registers off stack and place them in call registers
            uint register = (uint)((_ARG0 + mdef.Parameters.Count) - 1);
            for (int i = 0; i < mdef.Parameters.Count; i++)
                PopStack((uint)(register - i));

            m_state.RegisterCall(mdef);
            // i16 (set to 0xffff) should be replaced with correct value, when it is known!
            m_state.Instructions.Add(new SPEEmulator.OpCodes.brsl(1, 0xffff));

            //TODO: Not sure if void is detected this way
            if (mdef.ReturnType != null)
                PushStack(_ARG0);

        }

        public void Ret(IR.InstructionElement el)
        {
            PopStack(_ARG0);
        }

    }
}
