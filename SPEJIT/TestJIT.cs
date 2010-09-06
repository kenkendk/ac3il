using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace SPEJIT
{
    public class TestJIT
    {
        private static Dictionary<int, List<KeyValuePair<int,int>>> branches = new Dictionary<int, List<KeyValuePair<int,int>>>();
        private static List<SPEEmulator.OpCodes.Bases.Instruction> instr = new List<SPEEmulator.OpCodes.Bases.Instruction>();

        public static void AttemptJIT()
        {
            if (System.IO.File.Exists("CILFac.dll"))
            {
                AssemblyDefinition asm = AssemblyFactory.GetAssembly("CILFac.dll");
                ModuleDefinition mod = asm.MainModule;
                TypeDefinition tref = mod.Types["CILFac.Fac"];
                MethodDefinition mdef = tref.Methods[0];

                // Make copy of SP into register $2. Used to load arguments
                instr.AddRange(new SPEEmulator.OpCodes.Bases.Instruction[] { new SPEEmulator.OpCodes.ori(2, 1, 0) });

                foreach (Mono.Cecil.Cil.Instruction x in mdef.Body.Instructions)
                    instr.AddRange(JIT(x));
            }
            else
                throw new Exception("Could not find program file");
        }


        // We need to store the SP ($1) into the secret register ($2), so we can find the original stackpoint again.
        // new SPEEmulator.OpCodes.stqd()
        private static IEnumerable<SPEEmulator.OpCodes.Bases.Instruction> JIT(Mono.Cecil.Cil.Instruction x)
        {
            int operandOffset;
            KeyValuePair<int, int> branch;

            if (branches.ContainsKey(x.Offset))
            {
                foreach (KeyValuePair<int, int> pair in branches[x.Offset])
                {
                    SPEEmulator.OpCodes.Bases.Instruction inst = instr[pair.Value];

                    if (inst.GetType().BaseType == typeof(SPEEmulator.OpCodes.Bases.RI16))
                        ((SPEEmulator.OpCodes.Bases.RI16)inst).I16 = (uint)(instr.Count() - pair.Value);
                    else
                        throw new Exception("Unknown branch type");
                }

                branches.Remove(x.Offset);
            }
            
            switch (x.OpCode.Code)
            {
                case Mono.Cecil.Cil.Code.Nop:
                    return new SPEEmulator.OpCodes.Bases.Instruction[] 
                    { 
                        new SPEEmulator.OpCodes.nop(0) 
                    };
                case Mono.Cecil.Cil.Code.Ldarg_0:
                    return new SPEEmulator.OpCodes.Bases.Instruction[] 
                    { 
                        //Load argument 0 = Original SP from $2 - 8 (0x3f8) into $80
                        new SPEEmulator.OpCodes.lqd(80, 2, 0x3f8),
                        // Store value from $80 on stack 
                        new SPEEmulator.OpCodes.stqd(80, 1, 0),
                        // Increase SP with 8
                        new SPEEmulator.OpCodes.ai(1, 1, 8)
                    };
                case Mono.Cecil.Cil.Code.Ldc_I4_0:
                    return new SPEEmulator.OpCodes.Bases.Instruction[] 
                    {
                        new SPEEmulator.OpCodes.il(80, 0),
                        // Store value from $80 on stack 
                        new SPEEmulator.OpCodes.stqd(80, 1, 0),
                        // Increase SP with 8
                        new SPEEmulator.OpCodes.ai(1, 1, 8)
                    };
                case Mono.Cecil.Cil.Code.Ldc_I4_1:
                    return new SPEEmulator.OpCodes.Bases.Instruction[] 
                    {
                        new SPEEmulator.OpCodes.il(80, 1),
                        // Store value from $80 on stack 
                        new SPEEmulator.OpCodes.stqd(80, 1, 0),
                        // Increase SP with 8
                        new SPEEmulator.OpCodes.ai(1, 1, 8)
                    };
                case Mono.Cecil.Cil.Code.Conv_I8:
                    return new SPEEmulator.OpCodes.Bases.Instruction[]
                    {
                        new SPEEmulator.OpCodes.xswd(1, 1)
                    };
                case Mono.Cecil.Cil.Code.Ceq:
                    return new SPEEmulator.OpCodes.Bases.Instruction[]
                    {
                        new SPEEmulator.OpCodes.lqd(80, 1, 0),
                        new SPEEmulator.OpCodes.lqd(81, 1, 0x3f8),
                        new SPEEmulator.OpCodes.sfi(1, 1, 8),
                        new SPEEmulator.OpCodes.ceq(1, 80, 81)
                    };
                case Mono.Cecil.Cil.Code.Stloc_0:
                    return new SPEEmulator.OpCodes.Bases.Instruction[]
                    {
                        new SPEEmulator.OpCodes.lqd(90, 1, 0),
                        new SPEEmulator.OpCodes.ai(1, 1, 0x3f8)
                    };
                case Mono.Cecil.Cil.Code.Stloc_1:
                    return new SPEEmulator.OpCodes.Bases.Instruction[]
                    {
                        new SPEEmulator.OpCodes.lqd(91, 1, 0),
                        new SPEEmulator.OpCodes.sfi(1, 1, 8)
                    };
                case Mono.Cecil.Cil.Code.Ldloc_0:
                    return new SPEEmulator.OpCodes.Bases.Instruction[]
                    {
                        new SPEEmulator.OpCodes.stqd(90, 1, 0),
                        new SPEEmulator.OpCodes.ai(1, 1, 8)
                    };
                case Mono.Cecil.Cil.Code.Ldloc_1:
                    return new SPEEmulator.OpCodes.Bases.Instruction[]
                    {
                        new SPEEmulator.OpCodes.stqd(91, 1, 0),
                        new SPEEmulator.OpCodes.ai(1, 1, 8)
                    };
                case Mono.Cecil.Cil.Code.Brtrue_S:
                    operandOffset = (int)((Mono.Cecil.Cil.Instruction)x.Operand).Offset;
                    branch = new KeyValuePair<int, int>(x.Offset, instr.Count());

                    if (branches.ContainsKey(operandOffset))
                        branches[operandOffset].Add(branch);
                    else
                        branches.Add(operandOffset, new List<KeyValuePair<int, int>>() { branch });
                    
                    return new SPEEmulator.OpCodes.Bases.Instruction[]
                    {
                        // i16 (set to 0xffff) should be replaced with correct value, when it is know!
                        new SPEEmulator.OpCodes.brz(1, 0xffff)
                    };
                case Mono.Cecil.Cil.Code.Br_S:
                    operandOffset = (int)((Mono.Cecil.Cil.Instruction)x.Operand).Offset;
                    branch = new KeyValuePair<int, int>(x.Offset, instr.Count());

                    if (branches.ContainsKey(operandOffset))
                        branches[operandOffset].Add(branch);
                    else
                        branches.Add(operandOffset, new List<KeyValuePair<int, int>>() { branch });
                    
                    return new SPEEmulator.OpCodes.Bases.Instruction[]
                    {
                        // i16 (set to 0xffff) should be replaced with correct value, when it is know!
                        new SPEEmulator.OpCodes.br(1, 0xffff)
                    };
                case Mono.Cecil.Cil.Code.Sub:
                    return new SPEEmulator.OpCodes.Bases.Instruction[]
                    {
                        new SPEEmulator.OpCodes.lqd(80, 1, 0),
                        new SPEEmulator.OpCodes.lqd(81, 1, 0x3f8),
                        new SPEEmulator.OpCodes.sfi(1, 1, 8),
                        new SPEEmulator.OpCodes.sf(1, 81, 80)
                    };
                case Mono.Cecil.Cil.Code.Mul:
                    return new SPEEmulator.OpCodes.Bases.Instruction[]
                    {
                        new SPEEmulator.OpCodes.lqd(80, 1, 0),
                        new SPEEmulator.OpCodes.lqd(81, 1, 0x3f8),
                        new SPEEmulator.OpCodes.sfi(1, 1, 8),
                        new SPEEmulator.OpCodes.mpy(1, 81, 80)
                    };
                case Mono.Cecil.Cil.Code.Call:               
                    int jump = -1 * instr.Count();
                    return new SPEEmulator.OpCodes.Bases.Instruction[]
                    {
                        new SPEEmulator.OpCodes.br(1, (uint)jump)
                    };

                case Mono.Cecil.Cil.Code.Ret:
                    return new SPEEmulator.OpCodes.Bases.Instruction[]
                    {
                    };
                default:
                    throw new Exception("Unknown opcode: " + x.OpCode.Code.ToString());
            }
        }


    }
}
