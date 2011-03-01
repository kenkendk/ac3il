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
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            /*ulong z = 0xFFFFFFFFFFFFFFFB;
            ulong f = 0xa;

            if (umul(z, f) != 0xFFFFFFFFFFFFFFCE)
                Console.WriteLine("Broken"); 

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
            
            unchecked
            {
                if ((0xffffffffu * (ulong)-5) != umul(0xffffffffu, (ulong)-5))
                    Console.WriteLine("Broken");
            }*/

            try
            {
                AccCIL.IAccellerator virtualSPE = new SPEJIT.CellSPEEmulatorAccelerator();
                //long result = virtualSPE.Accelerate<long,long>(CILFac.Fac.Factorial, 10);
                //virtualSPE.Accelerate(CILFac.Fac.SPE_Main);
                //var test = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                //var sum = virtualSPE.Accelerate<byte[], byte, byte[]>(CILArray.ArrayTest.mult, test, 4);

                TestSuite();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void TestSuite()
        {
            AccCIL.IAccellerator virtualSPE = new SPEJIT.CellSPEEmulatorAccelerator();
            ((SPEJIT.CellSPEEmulatorAccelerator)virtualSPE).ShowGUI = false;

            var bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var sbytes = new sbyte[] { 0, -1, 2, -3, 4, -5, 6, -7, 8, -9 };
            var shorts = new short[] { 0, -1, 2, -3, 4, -5, 6, -7, 8, -9 }; ;
            var ushorts = new ushort[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var ints = new int[] { 0, -1, 2, -3, 4, -5, 6, -7, 8, -9 }; ;
            var uints = new uint[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var longs = new long[] { 0, -1, 2, -3, 4, -5, 6, -7, 8, -9 }; ;
            var ulongs = new ulong[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var floats = new float[] { 0, -1, 2, -3, 4, -5, 6, -7, 8, -9 }; ;
            var doubles = new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            long result = virtualSPE.Accelerate<long, long>(CILFac.Fac.Factorial, 10);
            if (result != CILFac.Fac.Factorial(10))
                throw new Exception("Failed to run fac");


            var bytesum = virtualSPE.Accelerate<byte[], byte>(CILArray.ArrayTest.sum, bytes);
            if (bytesum != CILArray.ArrayTest.sum(bytes))
                throw new Exception("Sum of bytes failed");

            var sbytesum = virtualSPE.Accelerate<sbyte[], sbyte>(CILArray.ArrayTest.sum, sbytes);
            if (sbytesum != CILArray.ArrayTest.sum(sbytes))
                throw new Exception("Sum of sbytes failed");
            
            var shortsum = virtualSPE.Accelerate<short[], short>(CILArray.ArrayTest.sum, shorts);
            if (shortsum != CILArray.ArrayTest.sum(shorts))
                throw new Exception("Sum of shorts failed");

            var ushortsum = virtualSPE.Accelerate<ushort[], ushort>(CILArray.ArrayTest.sum, ushorts);
            if (ushortsum != CILArray.ArrayTest.sum(ushorts))
                throw new Exception("Sum of ushorts failed");

            var intsum = virtualSPE.Accelerate<int[], int>(CILArray.ArrayTest.sum, ints);
            if (intsum != CILArray.ArrayTest.sum(ints))
                throw new Exception("Sum of ints failed");

            var uintsum = virtualSPE.Accelerate<uint[], uint>(CILArray.ArrayTest.sum, uints);
            if (uintsum != CILArray.ArrayTest.sum(uints))
                throw new Exception("Sum of uints failed");


            var longsum = virtualSPE.Accelerate<long[], long>(CILArray.ArrayTest.sum, longs);
            if (longsum != CILArray.ArrayTest.sum(longs))
                throw new Exception("Sum of longs failed");

            var ulongsum = virtualSPE.Accelerate<ulong[], ulong>(CILArray.ArrayTest.sum, ulongs);
            if (ulongsum != CILArray.ArrayTest.sum(ulongs))
                throw new Exception("Sum of ulongs failed");

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
            uint a0 = (uint)((a >> 48) & 0xffff);

            uint b3 = (uint)(b & 0xffff);
            uint b2 = (uint)((b >> 16) & 0xffff);
            uint b1 = (uint)((b >> 32) & 0xffff);
            uint b0 = (uint)((b >> 48) & 0xffff);

            ulong r = a3 * b3;
            r += ((ulong)(a3 * b2) + (ulong)(a2 * b3)) << 16;
            r += ((ulong)(a3 * b1) + (ulong)(a2 * b2) + (ulong)(a1 * b3)) << 32;
            r += ((ulong)(a2 * b1) + (ulong)(a1 * b2) + (ulong)(a3 * b0) + (ulong)(a0 * b3)) << 48;

            return r;
        }

        
    }
}
