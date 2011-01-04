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
    public class VirtualSPEManager : AccCIL.AccelleratorBase
    {
        public override void LoadProgram(IEnumerable<AccCIL.ICompiledMethod> methods)
        {
            throw new NotImplementedException();
        }

        public override object Execute(params object[] arguments)
        {
            throw new NotImplementedException();
        }

        protected override T DoAccelerate<T>(string assembly, string methodName, params object[] args)
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

            return default(T);
        }
    }
}
