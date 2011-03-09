using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace AccCIL
{
    public class AccCIL
    {
        private static object _cacheLock = new object();
        private static Dictionary<string, AssemblyDefinition> _assemblyDefinitionCache = new Dictionary<string, AssemblyDefinition>();
        private static Dictionary<AssemblyDefinition, Dictionary<string, List<MethodDefinition>>> _methodDefinitionCache = new Dictionary<AssemblyDefinition, Dictionary<string, List<MethodDefinition>>>();

        public static AssemblyDefinition LoadAssemblyFile(string filename)
        {
            AssemblyDefinition asm;
            string assemblyName = System.Reflection.Assembly.LoadFrom(filename).GetName().ToString();
            lock (_cacheLock)
            {
                _assemblyDefinitionCache.TryGetValue(assemblyName, out asm);
                if (asm == null)
                    _assemblyDefinitionCache.Add(assemblyName, asm = AssemblyFactory.GetAssembly(System.Reflection.Assembly.Load(new System.Reflection.AssemblyName(assemblyName)).Location));
            }

            return asm;
        }

        public static string FindAssemblyName(MethodReference mr)
        {
            IMetadataScope scope = mr.DeclaringType.Scope;

            if (scope is ModuleDefinition)
                scope = (AssemblyNameReference)((ModuleDefinition)scope).Assembly.Name;
            
            if (scope is AssemblyNameReference)
                return scope.ToString();
            else
                throw new Exception("Unexpected scope token: " + scope);
        }

        public static MethodDefinition FindMethod(MethodReference mr)
        {
            Type[] types = new Type[mr.Parameters.Count];
            for (int i = 0; i < types.Length; i++)
                types[i] = Type.GetType(mr.Parameters[i].ParameterType.FullName);

            AssemblyDefinition asm;
            string assemblyName = FindAssemblyName(mr);
            lock (_cacheLock)
            {
                _assemblyDefinitionCache.TryGetValue(assemblyName, out asm);
                if (asm == null)
                    _assemblyDefinitionCache.Add(assemblyName, asm = AssemblyFactory.GetAssembly(System.Reflection.Assembly.Load(new System.Reflection.AssemblyName(assemblyName)).Location));
            }

            return FindMethod(asm, mr.DeclaringType.FullName + "." + mr.Name, types);
        }

        public static MethodDefinition FindMethod(AssemblyDefinition asm, string name, Type[] args)
        {
            Dictionary<string, List<MethodDefinition>> methodLookup;
            lock (_cacheLock)
            {
                _methodDefinitionCache.TryGetValue(asm, out methodLookup);
                if (methodLookup == null)
                {
                    methodLookup = new Dictionary<string, List<MethodDefinition>>();

                    foreach (ModuleDefinition mod in asm.Modules)
                        foreach (TypeDefinition tref in mod.Types)
                            foreach (MethodDefinition mdef in tref.Methods)
                            {
                                string functioname = mdef.DeclaringType.FullName + "." + mdef.Name;
                                List<MethodDefinition> lm;
                                methodLookup.TryGetValue(functioname, out lm);
                                if (lm == null)
                                    methodLookup.Add(functioname, lm = new List<MethodDefinition>());
                                lm.Add(mdef);
                            }

                    _methodDefinitionCache.Add(asm, methodLookup);
                }
            }

            if (!methodLookup.ContainsKey(name))
                return null;

            foreach (MethodDefinition mdef in methodLookup[name])
                if (mdef.Parameters.Count == args.Length)
                {
                    bool match = true;
                    for (int i = 0; i < mdef.Parameters.Count; i++)
                        match &= Type.GetType(mdef.Parameters[i].ParameterType.FullName) == args[i];

                    if (match)
                        return mdef;
                }

            return null;
        }

        public static System.Reflection.MethodInfo FindReflectionMethod(MethodReference calledmethod)
        {
            string methodname = calledmethod.DeclaringType.FullName + "." + calledmethod.Name;
            string assemblyName = FindAssemblyName(calledmethod);
            System.Reflection.Assembly asm = System.Reflection.Assembly.Load(new System.Reflection.AssemblyName(assemblyName));
            foreach (var mod in asm.GetModules())
                foreach (var type in asm.GetTypes())
                    if (type.FullName == calledmethod.DeclaringType.FullName)
                        foreach (var method in type.GetMethods())
                            if (method.Name == calledmethod.Name)
                            {
                                var margs = method.GetParameters();
                                if (margs.Length == calledmethod.Parameters.Count)
                                {
                                    bool match = true;
                                    for (int i = 0; i < margs.Length; i++)
                                        if (margs[i].ParameterType != Type.GetType(calledmethod.Parameters[i].ParameterType.FullName))
                                        {
                                            match = false;
                                            break;
                                        }

                                    if (match)
                                        return method;
                                }
                            }

            return null;
        }

        /*public static List<ICompiledMethod> JIT(IJITCompiler compiler, string assemblyfile, string[] functionnames, FunctionFilterDelegate functionfilter)
        {
            if (functionfilter == null)
                functionfilter = SameAssemblyFunctionFilter;

            if (!System.IO.File.Exists(assemblyfile))
                throw new Exception("Could not find program file: " + assemblyfile);

            AssemblyDefinition asm = AssemblyFactory.GetAssembly(assemblyfile);
            
            //TODO: Should support function overloading
            Dictionary<string, MethodDefinition> functionLookup = new Dictionary<string, MethodDefinition>();
            foreach (ModuleDefinition mod in asm.Modules)
                foreach (TypeDefinition tref in mod.Types)
                    foreach (MethodDefinition mdef in tref.Methods)
                        functionLookup.Add(mdef.DeclaringType.FullName + "." + mdef.Name, mdef);

            Dictionary<MethodReference, string> visitedMethods = new Dictionary<MethodReference, string>();
            List<ICompiledMethod> methods = new List<ICompiledMethod>();

            Dictionary<Mono.Cecil.AssemblyDefinition, List<Mono.Cecil.MethodReference>> pendingWork = new Dictionary<AssemblyDefinition,List<MethodReference>>();

            foreach (string name in functionnames)
            {
                if (!functionLookup.ContainsKey(name))
                    throw new Exception("Unable to find function {0} in assembly {1}");

                MethodDefinition mainfunction = functionLookup[name];
                IR.MethodEntry m = BuildIRTree(mainfunction);
                m.ResetVirtualRegisters();
                methods.Add(compiler.JIT(m));
                visitedMethods.Add(mainfunction, null);

                var work = from x in m.FlatInstructionList
                           let code = x.Instruction.OpCode.Code
                           where code == Mono.Cecil.Cil.Code.Call || code == Mono.Cecil.Cil.Code.Calli || code == Mono.Cecil.Cil.Code.Callvirt
                           select ((Mono.Cecil.MethodReference)x.Instruction.Operand);

                foreach (MethodReference mdef in work)
                {
                    if (visitedMethods.ContainsKey(mdef))
                        continue;

                    visitedMethods.Add(mdef, null);

                    if (!functionfilter(null, mainfunction, mdef))
                        continue;


                    List<Mono.Cecil.MethodReference> mr;
                    pendingWork.TryGetValue(mdef.DeclaringType.Module.Assembly, out mr);
                    if (mr == null)
                        pendingWork.Add(mdef.DeclaringType.Module.Assembly, mr = new List<MethodReference>());
                    mr.Add(mdef);
                }

            }

            return methods;

        }*/

        /// <summary>
        /// Function that compiles all functions in an assembly
        /// </summary>
        /// <param name="compiler">The compiler that will do the work</param>
        /// <param name="assemblyfile">The assembly to compile all methods for</param>
        /// <returns>A list of compiled methods</returns>
        public static List<ICompiledMethod> JIT(IJITCompiler compiler, string assemblyfile)
        {
            if (System.IO.File.Exists(assemblyfile))
            {
                AssemblyDefinition asm = LoadAssemblyFile(assemblyfile);

                List<ICompiledMethod> methods = new List<ICompiledMethod>();
                foreach(ModuleDefinition mod in asm.Modules)
                    foreach(TypeDefinition tref in mod.Types)
                        foreach (MethodDefinition mdef in tref.Methods)
                        {
                            IR.MethodEntry root = BuildIRTree(mdef);
                            methods.Add(compiler.JIT(root));
                        }

                return methods;
            }
            else
                throw new Exception("Could not find program file: " + assemblyfile);
        }

        /// <summary>
        /// Compiles a single method
        /// </summary>
        /// <param name="compiler">The compiler that will do the work</param>
        /// <param name="mdef">The method to compile</param>
        /// <returns>The compiled method</returns>
        public static ICompiledMethod JIT(IJITCompiler compiler, Mono.Cecil.MethodDefinition mdef)
        {
            return compiler.JIT(BuildIRTree(mdef));
        }

        /// <summary>
        /// Gets the number of elements that the instruction pops
        /// </summary>
        /// <param name="i">The instruction to examine</param>
        /// <returns>The number of elements poped</returns>
        public static int NumberOfElementsPoped(IR.InstructionElement i)
        {
            if (i.Instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ret)
            {
                if (i.ParentMethod.ReturnType.ReturnType.FullName == "System.Void")
                    return 0;
                else
                    return 1;
            }
            return NumberOfElementsPoped(i.Instruction.OpCode.StackBehaviourPop, i.Instruction.Operand);
        }

        /// <summary>
        /// Gets the number of elements that the instruction pushes
        /// </summary>
        /// <param name="i">The instruction to examine</param>
        /// <returns>The number of elements pushed</returns>
        public static int NumberOfElementsPushed(IR.InstructionElement i)
        {
            return NumberOfElementsPushed(i.Instruction.OpCode.StackBehaviourPush, i.Instruction.Operand);
        }

        /// <summary>
        /// Gets the number of elements the stack changes after the instruction.
        /// If the instruction only pops elements, this number will be negative.
        /// </summary>
        /// <param name="i">The instruction to examine</param>
        /// <returns>The size of the stack change</returns>
        public static int StackChangeCount(IR.InstructionElement i)
        {
            return NumberOfElementsPushed(i) - NumberOfElementsPoped(i);
        }

        private static int NumberOfElementsPoped(Mono.Cecil.Cil.StackBehaviour s, object operand = null)
        {
            switch (s)
            {
                case Mono.Cecil.Cil.StackBehaviour.Pop0:
                    return 0;
                case Mono.Cecil.Cil.StackBehaviour.Pop1:
                case Mono.Cecil.Cil.StackBehaviour.Popi:
                case Mono.Cecil.Cil.StackBehaviour.Popref:
                    return 1;
                case Mono.Cecil.Cil.StackBehaviour.Pop1_pop1:
                case Mono.Cecil.Cil.StackBehaviour.Popi_pop1:
                case Mono.Cecil.Cil.StackBehaviour.Popi_popi:
                case Mono.Cecil.Cil.StackBehaviour.Popi_popi8:
                case Mono.Cecil.Cil.StackBehaviour.Popi_popr4:
                case Mono.Cecil.Cil.StackBehaviour.Popi_popr8:
                case Mono.Cecil.Cil.StackBehaviour.Popref_pop1:
                case Mono.Cecil.Cil.StackBehaviour.Popref_popi:
                    return 2;
                //case Mono.Cecil.Cil.StackBehaviour.Popref_popi_pop1: Hvorfor mangler denne?
                case Mono.Cecil.Cil.StackBehaviour.Popref_popi_popi:
                case Mono.Cecil.Cil.StackBehaviour.Popref_popi_popi8:
                case Mono.Cecil.Cil.StackBehaviour.Popref_popi_popr4:
                case Mono.Cecil.Cil.StackBehaviour.Popref_popi_popr8:
                case Mono.Cecil.Cil.StackBehaviour.Popref_popi_popref:
                    return 3;
                case Mono.Cecil.Cil.StackBehaviour.Varpop:
                    if (operand == null)
                        throw new Exception("This is an unexpected exception");
                    return ((Mono.Cecil.MethodReference)(operand)).Parameters.Count;
                default:
                    throw new InvalidProgramException();
            }
        }

        private static int NumberOfElementsPushed(Mono.Cecil.Cil.StackBehaviour s, object operand = null)
        {
            switch (s)
            {
                case Mono.Cecil.Cil.StackBehaviour.Push0:
                    return 0;
                case Mono.Cecil.Cil.StackBehaviour.Push1_push1:
                    return 2;
                case Mono.Cecil.Cil.StackBehaviour.Push1:
                case Mono.Cecil.Cil.StackBehaviour.Pushi:
                case Mono.Cecil.Cil.StackBehaviour.Pushi8:
                case Mono.Cecil.Cil.StackBehaviour.Pushr4:
                case Mono.Cecil.Cil.StackBehaviour.Pushr8:
                case Mono.Cecil.Cil.StackBehaviour.Pushref:
                    return 1;
                case Mono.Cecil.Cil.StackBehaviour.Varpush:
                    if (operand is Mono.Cecil.MethodReference)
                        return ((Mono.Cecil.MethodReference)(operand)).ReturnType.ReturnType.FullName == "System.Void" ? 0 : 1;
                    else
                        throw new Exception("This is an unexpected exception");
                default:
                    throw new InvalidProgramException();

            }
        }

        private static IR.MethodEntry BuildIRTree(MethodDefinition mdef)
        {
            Stack<IR.InstructionElement> stack = new Stack<IR.InstructionElement>();
            List<IR.InstructionElement> roots = new List<IR.InstructionElement>();

            int returnElements = 1;
            if (mdef.ReturnType.ReturnType.FullName == "System.Void")
                returnElements = 0;

            foreach (Mono.Cecil.Cil.Instruction x in mdef.Body.Instructions)
            {
                IR.InstructionElement[] childnodes;
                if (x.OpCode.Code == Mono.Cecil.Cil.Code.Ret)
                    childnodes = new IR.InstructionElement[returnElements];
                else
                    childnodes = new IR.InstructionElement[NumberOfElementsPoped(x.OpCode.StackBehaviourPop, x.Operand)];

                for (int i = childnodes.Length - 1; i >= 0; i--)
                {
                    System.Diagnostics.Trace.Assert(stack.Count > 0);
                    childnodes[i] = stack.Pop();
                }

                int elementsPushed = NumberOfElementsPushed(x.OpCode.StackBehaviourPush, x.Operand);

                if (elementsPushed == 0)
                {
                    if (stack.Count != 0 && x.OpCode.FlowControl != Mono.Cecil.Cil.FlowControl.Next && x.OpCode.FlowControl != Mono.Cecil.Cil.FlowControl.Call)
                        throw new InvalidProgramException();

                    roots.Add(new IR.InstructionElement(mdef, childnodes, x));
                }
                else
                {
                    IR.InstructionElement ins = new IR.InstructionElement(mdef, childnodes, x);
                    for(int i = 0; i < elementsPushed; i++)
                        stack.Push(ins);
                }
            }

            if (stack.Count != 0)
                throw new InvalidProgramException();

            IR.MethodEntry m = new IR.MethodEntry(mdef) { Childnodes = roots.ToArray() };
            m.ResetVirtualRegisters();
            return m;
        }

        public static KnownObjectTypes GetObjType(Type t)
        {
            if (t == typeof(bool))
                return KnownObjectTypes.Boolean;
            else if (t == typeof(byte))
                return KnownObjectTypes.Byte;
            else if (t == typeof(sbyte))
                return KnownObjectTypes.SByte;
            else if (t == typeof(short))
                return KnownObjectTypes.Short;
            else if (t == typeof(ushort))
                return KnownObjectTypes.UShort;
            else if (t == typeof(int))
                return KnownObjectTypes.Int;
            else if (t == typeof(uint))
                return KnownObjectTypes.UInt;
            else if (t == typeof(long))
                return KnownObjectTypes.Long;
            else if (t == typeof(ulong))
                return KnownObjectTypes.ULong;
            else if (t == typeof(float))
                return KnownObjectTypes.Float;
            else if (t == typeof(double))
                return KnownObjectTypes.Double;
            else
                throw new Exception("Unsupported type: " + t.FullName);

        }
    }
}
