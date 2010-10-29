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

                SPEJIT jitter = new SPEJIT();
                List<CompiledMethod> methods = new List<CompiledMethod>();

                foreach (MethodDefinition mdef in tref.Methods)
                {
                    IR.MethodEntry root = BuildIRTree(mdef);
                    //RegisterAllocator(root);
                    methods.Add(jitter.JIT(root));
                }

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                using (System.IO.StringWriter sw = new System.IO.StringWriter())
                {
                    jitter.EmitInstructionStream(ms, sw, methods);

                    Console.WriteLine("Converted output size in bytes: " + ms.Length);
                    Console.Write(sw.ToString());
                }
            }
            else
                throw new Exception("Could not find program file");
        }

        private static void RegisterAllocator(IR.MethodEntry root)
        {
            //Iterate over the independent sub trees
            foreach (IR.InstructionElement el in root.Childnodes)
            {
                //We assume that the output of a tree is a value
                switch (el.Instruction.OpCode.Code)
                {
                    case  Mono.Cecil.Cil.Code.Stloc:
                    case Mono.Cecil.Cil.Code.Stloc_0:
                    case Mono.Cecil.Cil.Code.Stloc_1:
                    case Mono.Cecil.Cil.Code.Stloc_2:
                    case Mono.Cecil.Cil.Code.Stloc_3:
                    case Mono.Cecil.Cil.Code.Stloc_S:
                    case Mono.Cecil.Cil.Code.Ret:
                        //We now assign a virtual register for that value,
                        // and pass down the register in the tree to allow sub-operations
                        // inject values into it
                        el.Registers = new IR.VirtualRegister[] { new IR.VirtualRegister() };

                        break;
                    default:
                        throw new Exception("Unexpected code fragment");
                }
            }
        }

        private static void RecursiveRegisterAssigner(IR.InstructionElement el)
        {
            if (el.Childnodes.Length == 1 && NumberOfElementsPoped(el.Instruction.OpCode.StackBehaviourPop) == 1)
            {
            }

            foreach (IR.InstructionElement sel in el.Childnodes)
            {
            }
        }

        private static int NumberOfElementsPoped(Mono.Cecil.Cil.StackBehaviour s)
        {
            switch (s)
            {
                case Mono.Cecil.Cil.StackBehaviour.Pop0:
                    return 0;
                case Mono.Cecil.Cil.StackBehaviour.Pop1:
                case Mono.Cecil.Cil.StackBehaviour.Popi:
                case Mono.Cecil.Cil.StackBehaviour.Varpop: //TODO: Varpop?
                    return 1;
                case Mono.Cecil.Cil.StackBehaviour.Pop1_pop1:
                case Mono.Cecil.Cil.StackBehaviour.Popi_popi:
                case Mono.Cecil.Cil.StackBehaviour.Popi_popi8:
                case Mono.Cecil.Cil.StackBehaviour.Popi_popr4:
                case Mono.Cecil.Cil.StackBehaviour.Popi_popr8:
                case Mono.Cecil.Cil.StackBehaviour.Popref:
                case Mono.Cecil.Cil.StackBehaviour.Popref_pop1:
                case Mono.Cecil.Cil.StackBehaviour.Popref_popi:
                    return 2;
                case Mono.Cecil.Cil.StackBehaviour.Popi_popi_popi:
                case Mono.Cecil.Cil.StackBehaviour.Popref_popi_popi8:
                case Mono.Cecil.Cil.StackBehaviour.Popref_popi_popr8:
                case Mono.Cecil.Cil.StackBehaviour.Popref_popi_popr4:
                case Mono.Cecil.Cil.StackBehaviour.Popref_popi_popref:
                    return 3;
                default:
                    throw new InvalidProgramException();
            }
        }

        private static IR.MethodEntry BuildIRTree(MethodDefinition mdef)
        {
            Stack<IR.InstructionElement> stack = new Stack<IR.InstructionElement>();
            List<IR.InstructionElement> roots = new List<IR.InstructionElement>();

            foreach (Mono.Cecil.Cil.Instruction x in mdef.Body.Instructions)
            {
                IR.InstructionElement[] childnodes = new IR.InstructionElement[NumberOfElementsPoped(x.OpCode.StackBehaviourPop)];
                for (int i = 0; i < childnodes.Length; i++)
                    childnodes[i] = stack.Pop();

                if (x.OpCode.StackBehaviourPush == Mono.Cecil.Cil.StackBehaviour.Push0)
                {
                    if (stack.Count != 0 && x.OpCode.FlowControl != Mono.Cecil.Cil.FlowControl.Next && x.OpCode.FlowControl != Mono.Cecil.Cil.FlowControl.Call)
                        throw new InvalidProgramException();

                    roots.Add(new IR.InstructionElement(childnodes, x));
                }
                else
                    stack.Push(new IR.InstructionElement(childnodes, x));
            }

            if (stack.Count != 0)
                throw new InvalidProgramException();
            return new IR.MethodEntry(mdef) { Childnodes = roots.ToArray() };
        }
    }
}
