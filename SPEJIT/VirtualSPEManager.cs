using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace SPEJIT
{
    /// <summary>
    /// Implementation of an SPE manager for the SPE emulator
    /// </summary>
    public class VirtualSPEManager : AccCIL.IAccellerator
    {
        #region ISPEManager Members

        public void LoadProgram(IEnumerable<AccCIL.ICompiledMethod> methods)
        {
            throw new NotImplementedException();
        }

        public object Execute(params object[] arguments)
        {
            throw new NotImplementedException();
        }

        public OutType Accelerate<OutType>(Func<OutType> func)
        {
            return (OutType)DoAccelerate(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, typeof(OutType));
        }

        public OutType Accelerate<InType, OutType>(Func<InType, OutType> func, InType arg)
        {
            return (OutType)DoAccelerate(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, typeof(OutType), arg);
        }

        public OutType Accelerate<InType1, InType2, OutType>(Func<InType1, InType2, OutType> func, InType1 arg1, InType2 arg2)
        {
            return (OutType)DoAccelerate(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, typeof(OutType), arg1, arg2);
        }

        public OutType Accelerate<InType1, InType2, InType3, OutType>(Func<InType1, InType2, InType3, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3)
        {
            return (OutType)DoAccelerate(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, typeof(OutType), arg1, arg2, arg3);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, OutType>(Func<InType1, InType2, InType3, InType4, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4)
        {
            return (OutType)DoAccelerate(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, typeof(OutType), arg1, arg2, arg3, arg4);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, OutType>(Func<InType1, InType2, InType3, InType4, InType5, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5)
        {
            return (OutType)DoAccelerate(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, typeof(OutType), arg1, arg2, arg3, arg4, arg5);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6)
        {
            return (OutType)DoAccelerate(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, typeof(OutType), arg1, arg2, arg3, arg4, arg5, arg6);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7)
        {
            return (OutType)DoAccelerate(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, typeof(OutType), arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8)
        {
            return (OutType)DoAccelerate(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, typeof(OutType), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9)
        {
            return (OutType)DoAccelerate(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, typeof(OutType), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10)
        {
            return (OutType)DoAccelerate(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, typeof(OutType), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11)
        {
            return (OutType)DoAccelerate(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, typeof(OutType), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12)
        {
            return (OutType)DoAccelerate(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, typeof(OutType), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13)
        {
            return (OutType)DoAccelerate(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, typeof(OutType), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14)
        {
            return (OutType)DoAccelerate(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, typeof(OutType), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14, InType15 arg15)
        {
            return (OutType)DoAccelerate(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, typeof(OutType), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, InType16, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, InType16, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14, InType15 arg15, InType16 arg16)
        {
            return (OutType)DoAccelerate(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, typeof(OutType), arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
        }

        public void Accelerate(Action action)
        {
            DoAccelerate(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name);
        }
        public void Accelerate<InType1>(Action<InType1> action, InType1 arg1)
        {
            DoAccelerate(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, null, arg1);
        }

        public void Accelerate<InType1, InType2>(Action<InType1, InType2> action, InType1 arg1, InType2 arg2)
        {
            DoAccelerate(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, null, arg1, arg2);
        }

        public void Accelerate<InType1, InType2, InType3>(Action<InType1, InType2, InType3> action, InType1 arg1, InType2 arg2, InType3 arg3)
        {
            DoAccelerate(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, null, arg1, arg2, arg3);
        }

        public void Accelerate<InType1, InType2, InType3, InType4>(Action<InType1, InType2, InType3, InType4> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4)
        {
            DoAccelerate(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, null, arg1, arg2, arg3, arg4);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5>(Action<InType1, InType2, InType3, InType4, InType5> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5)
        {
            DoAccelerate(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, null, arg1, arg2, arg3, arg4, arg5);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6>(Action<InType1, InType2, InType3, InType4, InType5, InType6> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6)
        {
            DoAccelerate(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, null, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7)
        {
            DoAccelerate(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, null, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8)
        {
            DoAccelerate(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, null, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9)
        {
            DoAccelerate(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, null, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10)
        {
            DoAccelerate(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, null, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11)
        {
            DoAccelerate(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, null, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12)
        {
            DoAccelerate(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, null, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13)
        {
            DoAccelerate(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, null, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14)
        {
            DoAccelerate(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, null, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14, InType15 arg15)
        {
            DoAccelerate(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, null, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, InType16>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, InType16> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14, InType15 arg15, InType16 arg16)
        {
            DoAccelerate(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, null, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
        }
        #endregion

        private object DoAccelerate(string assembly, string methodName, Type returnType = null, params object[] args)
        {
            // Create LS

            // Compile code
            string startPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string program = System.IO.Path.Combine(startPath, assembly);

            SPEJIT.SPEJITCompiler compiler = new SPEJIT.SPEJITCompiler();
            List<AccCIL.ICompiledMethod> methods = AccCIL.AccCIL.JIT(compiler, program);

            // Find called method - Can this be done at an earlier state - perhaps the compiler.
            // Does the compiler already do this?!
            List<MethodReference> calledMethods = new List<MethodReference>();
            calledMethods.Add(methods[methods.FindIndex(x => x.Method.Method.Name == methodName)].Method.Method);

            // Find submethods
            for (int i = 0; i < calledMethods.Count; i++)
            {
                int ix = methods.FindIndex(x => x.Method.Method.DeclaringType + "." + x.Method.Method.Name == calledMethods[i].DeclaringType.FullName + "." + calledMethods[i].Name);

                // If ix is -1, the method is not found and therefor do not have any submethods -> continue
                if (ix == -1)
                    continue;

                // Add all methods that does not already exist in calledMethods
                foreach (MethodReference method in ((SPEJIT.CompiledMethod)(methods[ix])).CalledMethods)
                {
                    if (!calledMethods.Contains(method))
                        calledMethods.Add(method);
                }
            }

            // Remove unused methods
            for (int i = 0; i < methods.Count; i++)
            {
                if (calledMethods.FindIndex(x => x.DeclaringType.FullName + "." + x.Name == methods[i].Method.Method.DeclaringType.FullName + "." + methods[i].Method.Method.Name) == -1)
                {
                    methods.RemoveAt(i);
                    i--;
                }
            }

            // Create ELF
            string elffile = System.IO.Path.Combine(startPath, program + ".elf");
            using (System.IO.FileStream outfile = new System.IO.FileStream(elffile, System.IO.FileMode.Create))
            using (System.IO.TextWriter sw = new System.IO.StreamWriter(System.IO.Path.Combine(startPath, program + ".asm")))
            {
                compiler.EmitELFStream(outfile, sw, methods);
                Console.WriteLine("Converted output size in bytes: " + outfile.Length);
            }

            // Run program
            SPEEmulatorTestApp.Program.Main(new string[] { elffile });

            return null;
        }
    }
}
