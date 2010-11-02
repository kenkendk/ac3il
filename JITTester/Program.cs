using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JITTester
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {

                SPEJIT.SPEJITCompiler compiler = new SPEJIT.SPEJITCompiler();
                List<JITManager.ICompiledMethod> methods = JITManager.JITManager.JIT(new SPEJIT.SPEJITCompiler(), "CILFac.dll");

                //For the test setup, we want the main method to be at index 0
                int ix = methods.FindIndex(x => x.Method.Method.Name == "SPE_Main");
                if (ix < 0)
                    throw new Exception("Unable to find the startup function \"SPE_Main()\"");
                else if (ix != 0)
                {
                    JITManager.ICompiledMethod cm = methods[ix];
                    methods.RemoveAt(ix);
                    methods.Insert(0, cm);
                }

                using (System.IO.FileStream outfile = new System.IO.FileStream(@"D:\Documents\spe-emulator\test-apps\cil-fac.elf", System.IO.FileMode.Create))
                using (System.IO.TextWriter sw = new System.IO.StreamWriter(@"D:\Documents\spe-emulator\test-apps\cil-fac.asm"))
                {
                    compiler.EmitELFStream(outfile, sw, methods);

                    Console.WriteLine("Converted output size in bytes: " + outfile.Length);

                    /*outfile.Position = 0;
                    SPEEmulator.ELFReader r = new SPEEmulator.ELFReader(outfile);
                    using (System.IO.StringWriter sw2 = new System.IO.StringWriter())
                    {
                        r.Disassemble(sw2);
                        //Console.WriteLine(sw2.ToString());
                    }*/
                }

                SPEEmulatorTestApp.Program.Main(new string[] { @"D:\Documents\spe-emulator\test-apps\cil-fac.elf" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }
    }
}
