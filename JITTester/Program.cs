using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JITTester
{
    class Program
    {

        private static void TestLogicals()
        {
            long x = -5;
            long y = -6;

            long a = 5;
            long b = 6;

            bool isXLessThanY = x < y;
            bool isYLessThanX = y < x;

            bool isXGreaterThanY = x > y;
            bool isYGreaterThanX = y > x;

            bool isALessThanB = a < b;
            bool isBLessThanA = b < a;
            bool isAGreaterThanB = a > b;
            bool isBGreaterThanA = b > a;

            bool isAGreaterThanX = a > x;
            bool isXGreaterThanA = x > a;
            bool isALessThanX = a < x;
            bool isXLessThanA = x < a;

            bool isAGreaterThanY = a > y;
            bool isYGreaterThanA = y > a;
            bool isALessThanY = a < y;
            bool isYLessThanA = y < a;

            ulong n = 2;
            ulong m = 3;

            bool isNGreaterThanM = n > m;
            bool isMGreaterThanN = m > n;
            bool isNLessThanM = n < m;
            bool isMLessThanN = m < n;

            if (
                isXLessThanY | !isYLessThanX | !isXGreaterThanY | isYGreaterThanX |
                !isALessThanB | isBLessThanA | isAGreaterThanB | !isBGreaterThanA |
                !isAGreaterThanX | isXGreaterThanA | isALessThanX | !isXLessThanA |
                !isAGreaterThanY | isYGreaterThanA | isALessThanY | !isYLessThanA | 
                isNGreaterThanM | !isMGreaterThanN | !isNLessThanM | isMLessThanN 
                )
                Console.WriteLine(y);
        }

        [STAThread]
        static void Main(string[] args)
        {
            if (umul(0xffff, 0xffffa) != 0xffffL * 0xffffa)
                Console.WriteLine("Broken");

            TestLogicals();
            if (mul(0xffffffff, 0xa) != 0xffffffffL * 0xa)
                Console.WriteLine("Broken");

            
            if (mul(0xffffffff, 0xafffa) != 0xffffffffL * 0xafffa)
                Console.WriteLine("Broken");

            long n = 0xffffffffL;
            n *= n;

            if (n != mul(0xffffffff, 0xffffffff))
                Console.WriteLine("Broken");

            if (0xffffffff * -5 != mul(0xffffffff, -5))
                Console.WriteLine("Broken");

            try
            {
                string startPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                SPEJIT.SPEJITCompiler compiler = new SPEJIT.SPEJITCompiler();
                List<JITManager.ICompiledMethod> methods = JITManager.JITManager.JIT(new SPEJIT.SPEJITCompiler(), System.IO.Path.Combine(startPath, "CILFac.dll"));

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

                string elffile = System.IO.Path.Combine(startPath, "cil-fac.elf");
                using (System.IO.FileStream outfile = new System.IO.FileStream(elffile, System.IO.FileMode.Create))
                using (System.IO.TextWriter sw = new System.IO.StreamWriter(System.IO.Path.Combine(startPath, "cil-fac.asm")))
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

                SPEEmulatorTestApp.Program.Main(new string[] {elffile });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        /// <summary>
        /// Emulated 64 bit multiplication with 16 bit multiply
        /// </summary>
        /// <param name="a">Operand a</param>
        /// <param name="b">Operand b</param>
        /// <returns>The result of multiplying a with b</returns>
        private static long mul(long a, long b)
        {
            bool invertResult = a < 0 != b < 0;

            if (a < 0)
                a = ~(a - 1);
            if (b < 0)
                b = ~(b - 1);

            ulong result = umul((ulong)a, (ulong)b);
            
            if (invertResult)
                return (long)~(result - 1);
            else
                return (long)result;
        }

        /// <summary>
        /// Emulated 64 bit multiplication with 16 bit multiply
        /// </summary>
        /// <param name="a">Operand a</param>
        /// <param name="b">Operand b</param>
        /// <returns>The result of multiplying a with b</returns>
        private static ulong umul(ulong a, ulong b)
        {
            uint a3 = (uint)(a & 0xffff);
            uint a2 = (uint)((a >> 16) & 0xffff);
            uint a1 = (uint)((a >> 32) & 0xffff);
            //uint a0 = (uint)((a & 0xffff)) >> 48;

            uint b3 = (uint)(b & 0xffff);
            uint b2 = (uint)((b >> 16) & 0xffff);
            uint b1 = (uint)((b >> 32) & 0xffff);
            //int b0 = (int)((b & 0xffff)) >> 48;

            ulong r = a3 * b3;
            r += ((ulong)(a3 * b2) + (ulong)(a2 * b3)) << 16;
            r += ((ulong)(a3 * b1) + (ulong)(a2 * b2) + (ulong)(a1 * b3)) << 32;
            r += ((ulong)(a2 * b1) + (ulong)(a1 * b2)) << 48;

            return r;
        }

        
    }
}
