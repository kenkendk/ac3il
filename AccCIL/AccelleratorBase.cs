using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace AccCIL
{
    public abstract class AccelleratorBase : IAccellerator
    {
        protected FunctionFilterDelegate m_functionFilter = new FunctionFilterDelegate(SameAssemblyFilter);
        protected Dictionary<MethodDefinition, List<ICompiledMethod>> m_compilationCache = new Dictionary<MethodDefinition, List<ICompiledMethod>>();
        protected MethodDefinition m_loadedMethod = null;
        protected Type[] m_loadedMethodTypes;
        protected bool[] m_typeSerializeOut;
        protected bool[] m_typeSerializeIn;

        private static bool SameAssemblyFilter(MethodReference initial, MethodReference parent, MethodReference current)
        {
            return AccCIL.FindAssemblyName(current) == AccCIL.FindAssemblyName(initial);
        }

        public FunctionFilterDelegate FunctionFilter
        {
            get { return m_functionFilter; }
            set { m_functionFilter = value; }
        }

        /// <summary>
        /// Internal helper function that ensures that all book keeping variables are set when loading a method
        /// </summary>
        /// <param name="methodset">The list of compiled methods that comprises the kernel</param>
        /// <param name="types">The set of types that the method signature dictates</param>
        /// <param name="method">The method to invoke</param>
        private void LoadProgramInternal(List<ICompiledMethod> methodset, MethodDefinition method, Type[] types)
        {
            if (m_loadedMethod != method)
            {
                m_loadedMethod = method;
                m_loadedMethodTypes = types;

                m_typeSerializeIn = new bool[method.Parameters.Count];
                m_typeSerializeOut = new bool[method.Parameters.Count];
                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    m_typeSerializeIn[i] = m_typeSerializeOut[i] = true;
                    if (method.Parameters[i].Attributes.HasFlag(Mono.Cecil.ParameterAttributes.In) && !method.Parameters[i].Attributes.HasFlag(Mono.Cecil.ParameterAttributes.Out))
                        m_typeSerializeOut[i] = false;
                    else if (method.Parameters[i].Attributes.HasFlag(Mono.Cecil.ParameterAttributes.Out) && !method.Parameters[i].Attributes.HasFlag(Mono.Cecil.ParameterAttributes.In))
                        m_typeSerializeIn[i] = false;
                }

                LoadProgram(methodset);
            }
        }

        protected T DoAccelerate<T>(string assemblyfile, string methodName, Type[] types, params object[] args)
        {
            if (!System.IO.File.Exists(assemblyfile))
                throw new Exception("Could not find program file: " + assemblyfile);

            AssemblyDefinition asm = AccCIL.LoadAssemblyFile(assemblyfile);
            MethodDefinition initialMethod = AccCIL.FindMethod(asm, methodName, types);
            if (initialMethod == null)
                throw new Exception("Unable to find a match for " + methodName + " in assembly " + assemblyfile);

            List<ICompiledMethod> methodset;
            m_compilationCache.TryGetValue(initialMethod, out methodset);
            if (methodset == null)
                m_compilationCache.Add(initialMethod, methodset = CompileMethod(initialMethod));

            LoadProgramInternal(methodset, initialMethod, types);

            return Execute<T>(args);
        }

        protected List<ICompiledMethod> CompileMethod(MethodDefinition initialMethod)
        {
            Dictionary<MethodReference, string> visitedMethods = new Dictionary<MethodReference, string>();
            List<ICompiledMethod> methods = new List<ICompiledMethod>();
            ICompiledMethod initialCm = AccCIL.JIT(this.Compiler, initialMethod);
            methods.Add(initialCm);
            visitedMethods.Add(initialMethod, null);

            Queue<ICompiledMethod> work = new Queue<ICompiledMethod>();
            work.Enqueue(initialCm);

            while (work.Count > 0)
            {
                ICompiledMethod cur = work.Dequeue();
                var calls = from x in cur.Method.FlatInstructionList
                           let code = x.Instruction.OpCode.Code
                           where code == Mono.Cecil.Cil.Code.Call || code == Mono.Cecil.Cil.Code.Calli || code == Mono.Cecil.Cil.Code.Callvirt
                           select ((Mono.Cecil.MethodReference)x.Instruction.Operand);

                foreach (MethodReference mr in calls)
                {
                    if (visitedMethods.ContainsKey(mr))
                        continue;

                    visitedMethods.Add(mr, null);

                    if (m_functionFilter(initialMethod, cur.Method.Method, mr))
                    {
                        MethodDefinition md = AccCIL.FindMethod(mr);
                        if (md == null)
                            throw new Exception("Unable to locate the method " + mr.DeclaringType.FullName + "::" + mr.Name + " in assembly " + mr.DeclaringType.Module.Assembly.Name.FullName);
                        ICompiledMethod cm = AccCIL.JIT(this.Compiler, md);
                        work.Enqueue(cm);
                        methods.Add(cm);
                    }
                }
            }

            return methods;
        }

        protected abstract IJITCompiler Compiler { get; }

        /// <summary>
        /// Internal marker class for returntype &quot;void&quot;
        /// </summary>
        protected class ReturnTypeVoid { }

        #region IAccellerator Members

        public abstract void LoadProgram(IEnumerable<ICompiledMethod> methods);

        public abstract T Execute<T>(params object[] arguments);

        public OutType Accelerate<OutType>(Func<OutType> func)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.DeclaringType.FullName + "::" + func.Method.Name, new Type[] { });
        }

        public OutType Accelerate<InType, OutType>(Func<InType, OutType> func, InType arg)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.DeclaringType.FullName + "::" + func.Method.Name, new Type[] { typeof(InType) }, arg);
        }

        public OutType Accelerate<InType1, InType2, OutType>(Func<InType1, InType2, OutType> func, InType1 arg1, InType2 arg2)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.DeclaringType.FullName + "::" + func.Method.Name, new Type[] { typeof(InType1), typeof(InType2) }, arg1, arg2);
        }

        public OutType Accelerate<InType1, InType2, InType3, OutType>(Func<InType1, InType2, InType3, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.DeclaringType.FullName + "::" + func.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3) }, arg1, arg2, arg3);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, OutType>(Func<InType1, InType2, InType3, InType4, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.DeclaringType.FullName + "::" + func.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4) }, arg1, arg2, arg3, arg4);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, OutType>(Func<InType1, InType2, InType3, InType4, InType5, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.DeclaringType.FullName + "::" + func.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5) }, arg1, arg2, arg3, arg4, arg5);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.DeclaringType.FullName + "::" + func.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6) }, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.DeclaringType.FullName + "::" + func.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6), typeof(InType7) }, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.DeclaringType.FullName + "::" + func.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6), typeof(InType7), typeof(InType8) }, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.DeclaringType.FullName + "::" + func.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6), typeof(InType7), typeof(InType8), typeof(InType9) }, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.DeclaringType.FullName + "::" + func.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6), typeof(InType7), typeof(InType8), typeof(InType9), typeof(InType10) }, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.DeclaringType.FullName + "::" + func.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6), typeof(InType7), typeof(InType8), typeof(InType9), typeof(InType10), typeof(InType11) }, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.DeclaringType.FullName + "::" + func.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6), typeof(InType7), typeof(InType8), typeof(InType9), typeof(InType10), typeof(InType11), typeof(InType12) }, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.DeclaringType.FullName + "::" + func.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6), typeof(InType7), typeof(InType8), typeof(InType9), typeof(InType10), typeof(InType11), typeof(InType12), typeof(InType13) }, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.DeclaringType.FullName + "::" + func.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6), typeof(InType7), typeof(InType8), typeof(InType9), typeof(InType10), typeof(InType11), typeof(InType12), typeof(InType13), typeof(InType14) }, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14, InType15 arg15)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.DeclaringType.FullName + "::" + func.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6), typeof(InType7), typeof(InType8), typeof(InType9), typeof(InType10), typeof(InType11), typeof(InType12), typeof(InType13), typeof(InType14), typeof(InType15) }, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, InType16, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, InType16, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14, InType15 arg15, InType16 arg16)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.DeclaringType.FullName + "::" + func.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6), typeof(InType7), typeof(InType8), typeof(InType9), typeof(InType10), typeof(InType11), typeof(InType12), typeof(InType13), typeof(InType14), typeof(InType15), typeof(InType16) }, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
        }

        public void Accelerate(Action action)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.DeclaringType.FullName + "::" + action.Method.Name, new Type[] { });
        }
        public void Accelerate<InType1>(Action<InType1> action, InType1 arg1)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.DeclaringType.FullName + "::" + action.Method.Name, new Type[] { typeof(InType1) }, arg1);
        }

        public void Accelerate<InType1, InType2>(Action<InType1, InType2> action, InType1 arg1, InType2 arg2)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.DeclaringType.FullName + "::" + action.Method.Name, new Type[] { typeof(InType1), typeof(InType2) }, arg1, arg2);
        }

        public void Accelerate<InType1, InType2, InType3>(Action<InType1, InType2, InType3> action, InType1 arg1, InType2 arg2, InType3 arg3)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.DeclaringType.FullName + "::" + action.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3) }, arg1, arg2, arg3);
        }

        public void Accelerate<InType1, InType2, InType3, InType4>(Action<InType1, InType2, InType3, InType4> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.DeclaringType.FullName + "::" + action.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4) }, arg1, arg2, arg3, arg4);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5>(Action<InType1, InType2, InType3, InType4, InType5> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.DeclaringType.FullName + "::" + action.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5) }, arg1, arg2, arg3, arg4, arg5);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6>(Action<InType1, InType2, InType3, InType4, InType5, InType6> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.DeclaringType.FullName + "::" + action.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6) }, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.DeclaringType.FullName + "::" + action.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6), typeof(InType7) }, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.DeclaringType.FullName + "::" + action.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6), typeof(InType7), typeof(InType8) }, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.DeclaringType.FullName + "::" + action.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6), typeof(InType7), typeof(InType8), typeof(InType9) }, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.DeclaringType.FullName + "::" + action.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6), typeof(InType7), typeof(InType8), typeof(InType9), typeof(InType10)}, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.DeclaringType.FullName + "::" + action.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6), typeof(InType7), typeof(InType8), typeof(InType9), typeof(InType10), typeof(InType11) }, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.DeclaringType.FullName + "::" + action.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6), typeof(InType7), typeof(InType8), typeof(InType9), typeof(InType10), typeof(InType11), typeof(InType12) }, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.DeclaringType.FullName + "::" + action.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6), typeof(InType7), typeof(InType8), typeof(InType9), typeof(InType10), typeof(InType11), typeof(InType12), typeof(InType13) }, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.DeclaringType.FullName + "::" + action.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6), typeof(InType7), typeof(InType8), typeof(InType9), typeof(InType10), typeof(InType11), typeof(InType12), typeof(InType13), typeof(InType14) }, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14, InType15 arg15)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.DeclaringType.FullName + "::" + action.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6), typeof(InType7), typeof(InType8), typeof(InType9), typeof(InType10), typeof(InType11), typeof(InType12), typeof(InType13), typeof(InType14), typeof(InType15) }, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, InType16>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, InType16> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14, InType15 arg15, InType16 arg16)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.DeclaringType.FullName + "::" + action.Method.Name, new Type[] { typeof(InType1), typeof(InType2), typeof(InType3), typeof(InType4), typeof(InType5), typeof(InType6), typeof(InType7), typeof(InType8), typeof(InType9), typeof(InType10), typeof(InType11), typeof(InType12), typeof(InType13), typeof(InType14), typeof(InType15), typeof(InType16) }, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
        }
        #endregion

        #region IDisposable Members

        public abstract void Dispose();

        #endregion
    }
}
