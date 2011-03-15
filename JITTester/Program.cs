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
                AccCIL.IAccellerator acc = new SPEJIT.CellSPEEmulatorAccelerator();
                ((SPEJIT.CellSPEEmulatorAccelerator)acc).ShowGUI = true;


                ((SPEJIT.CellSPEEmulatorAccelerator)acc).ShowGUI = false;

                //acc.Accelerate(CILFac.Fac.WritelineTest3, 42);
                
                TestSuite(acc);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static byte[] TestBytes { get { return new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }; } }
        private static sbyte[] TestSBytes { get { return new sbyte[] { 0, -1, 2, -3, 4, -5, 6, -7, 8, -9 }; } }
        private static short[] TestShorts { get { return new short[] { 0, -1, 2, -3, 4, -5, 6, -7, 8, -9 }; } }
        private static ushort[] TestUShorts { get { return new ushort[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }; } }
        private static int[] TestInts { get { return new int[] { 0, -1, 2, -3, 4, -5, 6, -7, 8, -9 }; } }
        private static uint[] TestUInts { get { return new uint[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }; } }
        private static long[] TestLongs { get { return new long[] { 0, -1, 2, -3, 4, -5, 6, -7, 8, -9 }; } }
        private static ulong[] TestULongs { get { return new ulong[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }; } }
        private static float[] TestFloats { get { return new float[] { 0, -1, 2, -3, 4, -5, 6, -7, 8, -9 }; } }
        private static double[] TestDoubles { get { return new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }; } }

        public static void TestSuite(AccCIL.IAccellerator acc)
        {
            acc.Accelerate(CILFac.Fac.TestLogicals);
            acc.Accelerate(CILFac.Fac.TestArithmetics);
            acc.Accelerate(CILFac.Fac.TestMultiply);
            int [] n = new int[] { 4 };
            acc.Accelerate(CILArray.ArrayTest.testRef, n, n);

            n = new int[] { 42 };
            acc.Accelerate(CILArray.ArrayTest.testRef2, n, n);
            
            if (acc.Accelerate<object, int>(CILFac.Fac.UnboxTest, (object)42) != 42)
                throw new Exception("Unbox failed");

            acc.Accelerate(CILFac.Fac.WritelineTest, 42);
            acc.Accelerate(CILFac.Fac.WritelineTest2, (object)42);
            //acc.Accelerate(CILFac.Fac.WritelineTest3, 42);

            long result = acc.Accelerate<long, long>(CILFac.Fac.Factorial, 10);
            if (result != CILFac.Fac.Factorial(10))
                throw new Exception("Failed to run fac");

            var bytesum = acc.Accelerate<byte[], byte>(CILArray.ArrayTest.sum, TestBytes);
            if (bytesum != CILArray.ArrayTest.sum(TestBytes))
                throw new Exception("Sum of bytes failed");

            var sbytesum = acc.Accelerate<sbyte[], sbyte>(CILArray.ArrayTest.sum, TestSBytes);
            if (sbytesum != CILArray.ArrayTest.sum(TestSBytes))
                throw new Exception("Sum of sbytes failed");
            
            var shortsum = acc.Accelerate<short[], short>(CILArray.ArrayTest.sum, TestShorts);
            if (shortsum != CILArray.ArrayTest.sum(TestShorts))
                throw new Exception("Sum of shorts failed");

            var ushortsum = acc.Accelerate<ushort[], ushort>(CILArray.ArrayTest.sum, TestUShorts);
            if (ushortsum != CILArray.ArrayTest.sum(TestUShorts))
                throw new Exception("Sum of ushorts failed");

            var intsum = acc.Accelerate<int[], int>(CILArray.ArrayTest.sum, TestInts);
            if (intsum != CILArray.ArrayTest.sum(TestInts))
                throw new Exception("Sum of ints failed");

            var uintsum = acc.Accelerate<uint[], uint>(CILArray.ArrayTest.sum, TestUInts);
            if (uintsum != CILArray.ArrayTest.sum(TestUInts))
                throw new Exception("Sum of uints failed");

            var longsum = acc.Accelerate<long[], long>(CILArray.ArrayTest.sum, TestLongs);
            if (longsum != CILArray.ArrayTest.sum(TestLongs))
                throw new Exception("Sum of longs failed");

            var ulongsum = acc.Accelerate<ulong[], ulong>(CILArray.ArrayTest.sum, TestULongs);
            if (ulongsum != CILArray.ArrayTest.sum(TestULongs))
                throw new Exception("Sum of ulongs failed");

            var floatsum = acc.Accelerate<float[], float>(CILArray.ArrayTest.sum, TestFloats);
            if (floatsum != CILArray.ArrayTest.sum(TestFloats))
                throw new Exception("Sum of floats failed");

            var doublesum = acc.Accelerate<double[], double>(CILArray.ArrayTest.sum, TestDoubles);
            if (doublesum != CILArray.ArrayTest.sum(TestDoubles))
                throw new Exception("Sum of doubles failed");


            var outbytes = acc.Accelerate<byte[], byte, byte[]>(CILArray.ArrayTest.mult, TestBytes, 4);
            var cmpbytes = CILArray.ArrayTest.mult(TestBytes, 4);
            for (int i = 0; i < cmpbytes.Length; i++) if (cmpbytes[i] != outbytes[i]) throw new Exception("Error in byte multiply");

            var outsbytes = acc.Accelerate<sbyte[], sbyte, sbyte[]>(CILArray.ArrayTest.mult, TestSBytes, 4);
            var cmpsbytes = CILArray.ArrayTest.mult(TestSBytes, 4);
            for (int i = 0; i < cmpsbytes.Length; i++) if (cmpsbytes[i] != outsbytes[i]) throw new Exception("Error in sbyte multiply");

            var outshorts = acc.Accelerate<short[], short, short[]>(CILArray.ArrayTest.mult, TestShorts, 4);
            var cmpshorts = CILArray.ArrayTest.mult(TestShorts, 4);
            for (int i = 0; i < cmpshorts.Length; i++) if (cmpshorts[i] != outshorts[i]) throw new Exception("Error in short multiply");

            var outushorts = acc.Accelerate<ushort[], ushort, ushort[]>(CILArray.ArrayTest.mult, TestUShorts, 4);
            var cmpushorts = CILArray.ArrayTest.mult(TestUShorts, 4);
            for (int i = 0; i < cmpushorts.Length; i++) if (cmpushorts[i] != outushorts[i]) throw new Exception("Error in ushort multiply");

            var outints = acc.Accelerate<int[], int, int[]>(CILArray.ArrayTest.mult, TestInts, 4);
            var cmpints = CILArray.ArrayTest.mult(TestInts, 4);
            for (int i = 0; i < cmpints.Length; i++) if (cmpints[i] != outints[i]) throw new Exception("Error in int multiply");

            var outuints = acc.Accelerate<uint[], uint, uint[]>(CILArray.ArrayTest.mult, TestUInts, 4);
            var cmpuints = CILArray.ArrayTest.mult(TestUInts, 4);
            for (int i = 0; i < cmpuints.Length; i++) if (cmpuints[i] != outuints[i]) throw new Exception("Error in uints multiply");

            var outlongs = acc.Accelerate<long[], long, long[]>(CILArray.ArrayTest.mult, TestLongs, 4);
            var cmplongs = CILArray.ArrayTest.mult(TestLongs, 4);
            for (int i = 0; i < cmplongs.Length; i++) if (cmplongs[i] != outlongs[i]) throw new Exception("Error in long multiply");

            var outulongs = acc.Accelerate<ulong[], ulong, ulong[]>(CILArray.ArrayTest.mult, TestULongs, 4);
            var cmpulongs = CILArray.ArrayTest.mult(TestULongs, 4);
            for (int i = 0; i < cmpulongs.Length; i++) if (cmpulongs[i] != outulongs[i]) throw new Exception("Error in ulong multiply");

            var outfloats = acc.Accelerate<float[], float, float[]>(CILArray.ArrayTest.mult, TestFloats, 4);
            var cmpfloats = CILArray.ArrayTest.mult(TestFloats, 4);
            for (int i = 0; i < cmpfloats.Length; i++) if (cmpfloats[i] != outfloats[i]) throw new Exception("Error in float multiply");

            var outdoubles = acc.Accelerate<double[], double, double[]>(CILArray.ArrayTest.mult, TestDoubles, 4);
            var cmpdoubles = CILArray.ArrayTest.mult(TestDoubles, 4);
            for (int i = 0; i < cmpdoubles.Length; i++) if (cmpdoubles[i] != outdoubles[i]) throw new Exception("Error in double multiply");


            acc.Accelerate<byte[], byte[], byte[]>(CILArray.ArrayTest.add, TestBytes, TestBytes, outbytes);
            CILArray.ArrayTest.add(TestBytes, TestBytes, cmpbytes);
            for (int i = 0; i < cmpbytes.Length; i++) if (cmpbytes[i] != outbytes[i]) throw new Exception("Error in byte add");

            acc.Accelerate<sbyte[], sbyte[], sbyte[]>(CILArray.ArrayTest.add, TestSBytes, TestSBytes, outsbytes);
            CILArray.ArrayTest.add(TestSBytes, TestSBytes, cmpsbytes);
            for (int i = 0; i < cmpsbytes.Length; i++) if (cmpsbytes[i] != outsbytes[i]) throw new Exception("Error in sbyte add");

            acc.Accelerate<short[], short[], short[]>(CILArray.ArrayTest.add, TestShorts, TestShorts, outshorts);
            CILArray.ArrayTest.add(TestShorts, TestShorts, cmpshorts);
            for (int i = 0; i < cmpshorts.Length; i++) if (cmpshorts[i] != outshorts[i]) throw new Exception("Error in short add");

            acc.Accelerate<ushort[], ushort[], ushort[]>(CILArray.ArrayTest.add, TestUShorts, TestUShorts, outushorts);
            CILArray.ArrayTest.add(TestUShorts, TestUShorts, cmpushorts);
            for (int i = 0; i < cmpushorts.Length; i++) if (cmpushorts[i] != outushorts[i]) throw new Exception("Error in ushort add");

            acc.Accelerate<int[], int[], int[]>(CILArray.ArrayTest.add, TestInts, TestInts, outints);
            CILArray.ArrayTest.add(TestInts, TestInts, cmpints);
            for (int i = 0; i < cmpints.Length; i++) if (cmpints[i] != outints[i]) throw new Exception("Error in int add");

            acc.Accelerate<uint[], uint[], uint[]>(CILArray.ArrayTest.add, TestUInts, TestUInts, outuints);
            CILArray.ArrayTest.add(TestUInts, TestUInts, cmpuints);
            for (int i = 0; i < cmpuints.Length; i++) if (cmpuints[i] != outuints[i]) throw new Exception("Error in uint add");

            acc.Accelerate<long[], long[], long[]>(CILArray.ArrayTest.add, TestLongs, TestLongs, outlongs);
            CILArray.ArrayTest.add(TestLongs, TestLongs, cmplongs);
            for (int i = 0; i < cmplongs.Length; i++) if (cmplongs[i] != outlongs[i]) throw new Exception("Error in long add");

            acc.Accelerate<ulong[], ulong[], ulong[]>(CILArray.ArrayTest.add, TestULongs, TestULongs, outulongs);
            CILArray.ArrayTest.add(TestULongs, TestULongs, cmpulongs);
            for (int i = 0; i < cmpulongs.Length; i++) if (cmpulongs[i] != outulongs[i]) throw new Exception("Error in ulong add");

            acc.Accelerate<float[], float[], float[]>(CILArray.ArrayTest.add, TestFloats, TestFloats, outfloats);
            CILArray.ArrayTest.add(TestFloats, TestFloats, cmpfloats);
            for (int i = 0; i < cmpfloats.Length; i++) if (cmpfloats[i] != outfloats[i]) throw new Exception("Error in float add");

            acc.Accelerate<double[], double[], double[]>(CILArray.ArrayTest.add, TestDoubles, TestDoubles, outdoubles);
            CILArray.ArrayTest.add(TestDoubles, TestDoubles, cmpdoubles);
            for (int i = 0; i < cmpdoubles.Length; i++) if (cmpdoubles[i] != outdoubles[i]) throw new Exception("Error in double add");
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
