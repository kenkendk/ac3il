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
        private SPEJIT.SPEJITCompiler m_compiler = new SPEJITCompiler();
        private string m_elf = null;

        public override void LoadProgram(IEnumerable<AccCIL.ICompiledMethod> methods)
        {
            string startPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string program = System.IO.Path.Combine(startPath, methods.First().Method.Method.DeclaringType.Module.Assembly.Name.Name);

#if DEBUG
            // Create ELF
            string elffile = System.IO.Path.Combine(startPath, program + ".elf");
            using (System.IO.FileStream outfile = new System.IO.FileStream(elffile, System.IO.FileMode.Create))
            using (System.IO.TextWriter sw = new System.IO.StreamWriter(System.IO.Path.Combine(startPath, program + ".asm")))
            {
                m_compiler.EmitELFStream(outfile, sw, methods);
                Console.WriteLine("Converted output size in bytes: " + outfile.Length);
            }
#else
            using (System.IO.FileStream outfile = new System.IO.FileStream(elffile, System.IO.FileMode.Create))
                m_compiler.EmitELFStream(outfile, null, methods); 
#endif
            m_elf = elffile;
        }

        protected override AccCIL.IJITCompiler Compiler
        {
            get { return m_compiler; }
        }


        public override T Execute<T>(params object[] args)
        {
            // Run program
            SPEEmulatorTestApp.Simulator s = new SPEEmulatorTestApp.Simulator(new string[] { m_elf });
            s.StartAndPause();

            s.SPE.WriteLSWord(0, 0);
            s.SPE.WriteLSWord(4, (uint)args.Length);

            uint lsoffset = (uint)s.SPE.LS.Length - (16 * 8);

            for (int i = args.Length - 1; i >= 0; i--)
            {
                lsoffset -= 16;

                if (args[i] is int || args[i] is uint || args[i] is short || args[i] is ushort || args[i] is byte || args[i] is sbyte)
                {
                    s.SPE.WriteLSWord(lsoffset, (uint)Convert.ToInt32(args[i]));
                    s.SPE.WriteLSWord(lsoffset + 4, (uint)Convert.ToInt32(args[i]));
                    s.SPE.WriteLSWord(lsoffset + 8, (uint)Convert.ToInt32(args[i]));
                    s.SPE.WriteLSWord(lsoffset + 12, (uint)Convert.ToInt32(args[i]));
                }
                else if (args[i] is long || args[i] is ulong)
                {
                    s.SPE.WriteLSDWord(lsoffset, (ulong)Convert.ToInt64(args[i]));
                    s.SPE.WriteLSDWord(lsoffset + 8, (ulong)Convert.ToInt64(args[i]));
                }
                else if (args[i] is float)
                {
                    s.SPE.WriteLSFloat(lsoffset, (float)args[i]);
                    s.SPE.WriteLSFloat(lsoffset + 4, (float)args[i]);
                    s.SPE.WriteLSFloat(lsoffset + 8, (float)args[i]);
                    s.SPE.WriteLSFloat(lsoffset + 12, (float)args[i]);
                }
                else if (args[i] is double)
                {
                    s.SPE.WriteLSDouble(lsoffset, (double)args[i]);
                    s.SPE.WriteLSDouble(lsoffset + 8, (double)args[i]);
                }
                else if (args[i] is bool)
                {
                    s.SPE.WriteLSWord(lsoffset, (uint)(((bool)args[i]) ? 1 : 0));
                    s.SPE.WriteLSWord(lsoffset + 4, (uint)(((bool)args[i]) ? 1 : 0));
                    s.SPE.WriteLSWord(lsoffset + 8, (uint)(((bool)args[i]) ? 1 : 0));
                    s.SPE.WriteLSWord(lsoffset + 12, (uint)(((bool)args[i]) ? 1 : 0));
                }
                else
                    throw new Exception("Unsupported argument type: " + args[i].GetType());
            }

            s.SPE.WriteLSWord(8, lsoffset);
            s.SPE.WriteLSWord(12, 0);

            System.Windows.Forms.Application.Run(s);
            //s.ShowDialog();

            Type rtype = typeof(T);

            if (rtype == typeof(int) || rtype == typeof(uint))
                return (T)Convert.ChangeType(s.SPE.ReadLSWord(0), typeof(T));
            else if (rtype == typeof(short) || rtype == typeof(ushort))
                return (T)Convert.ChangeType(s.SPE.ReadLSWord(0) & 0xffff, typeof(T));
            else if (rtype == typeof(byte) || rtype == typeof(sbyte))
                return (T)Convert.ChangeType(s.SPE.ReadLSWord(0) & 0xff, typeof(T));
            else if (rtype == typeof(long) || rtype == typeof(ulong))
                return (T)Convert.ChangeType(s.SPE.ReadLSDWord(0), typeof(T));
            else if (rtype == typeof(float))
                return (T)Convert.ChangeType(s.SPE.ReadLSFloat(0), typeof(T));
            else if (rtype == typeof(double))
                return (T)Convert.ChangeType(s.SPE.ReadLSDouble(0), typeof(T));
            else if (rtype == typeof(ReturnTypeVoid))
                return default(T);
            else
                throw new Exception("Return type not supported: " + rtype.FullName);
        }

    }
}
