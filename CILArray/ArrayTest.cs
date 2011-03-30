using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace CILArray
{
    public static class ArrayTest
    {
        public static void SPE_Main()
        {
            int x = sum(GetTestArray());
        }

        public static int[] GetTestArray()
        {
            int[] test = new int[100];
            for (int i = 0; i < test.Length; i++)
                test[i] = i;
            return test;
        }

        public static byte sum([In] byte[] test)
        {
            byte sum = 0;
            foreach (byte i in test)
                sum += i;
            return sum;
        }

        public static sbyte sum([In] sbyte[] test)
        {
            sbyte sum = 0;
            foreach (sbyte i in test)
                sum += i;
            return sum;
        }

        public static short sum([In] short[] test)
        {
            short sum = 0;
            foreach (short i in test)
                sum += i;
            return sum;
        }

        public static ushort sum([In] ushort[] test)
        {
            ushort sum = 0;
            foreach (ushort i in test)
                sum += i;
            return sum;
        }

        public static uint sum([In] uint[] test)
        {
            uint sum = 0;
            foreach (uint i in test)
                sum += i;
            return sum;
        }


        public static int sum([In] int[] test)
        {
            int sum = 0;
            foreach (int i in test)
                sum += i;
            return sum;
        }

        public static long sum([In] long[] test)
        {
            long sum = 0;
            foreach (long i in test)
                sum += i;
            return sum;
        }

        public static ulong sum([In] ulong[] test)
        {
            ulong sum = 0;
            foreach (ulong i in test)
                sum += i;
            return sum;
        }


        public static float sum([In] float[] test)
        {
            float sum = 0;
            foreach (float i in test)
                sum += i;
            return sum;
        }

        public static double sum([In] double[] test)
        {
            double sum = 0;
            foreach (double i in test)
                sum += i;
            return sum;
        }

        public static float[] mult(float[] input, float scalar)
        {
            for (int i = 0; i < input.Length; i++)
                input[i] *= scalar;
            return input;
        }

        public static double[] mult(double[] input, double scalar)
        {
            for (int i = 0; i < input.Length; i++)
                input[i] *= scalar;
            return input;
        }

        public static long[] mult(long[] input, long scalar)
        {
            for (int i = 0; i < input.Length; i++)
                input[i] *= scalar;
            return input;
        }

        public static int[] mult(int[] input, int scalar)
        {
            for (int i = 0; i < input.Length; i++)
                input[i] *= scalar;
            return input;
        }

        public static short[] mult(short[] input, short scalar)
        {
            for (int i = 0; i < input.Length; i++)
                input[i] *= scalar;
            return input;
        }

        public static byte[] mult(byte[] input, byte scalar)
        {
            for (int i = 0; i < input.Length; i++)
                input[i] *= scalar;
            return input;
        }

        public static ulong[] mult(ulong[] input, ulong scalar)
        {
            for (int i = 0; i < input.Length; i++)
                input[i] *= scalar;
            return input;
        }

        public static uint[] mult(uint[] input, uint scalar)
        {
            for (int i = 0; i < input.Length; i++)
                input[i] *= scalar;
            return input;
        }

        public static ushort[] mult(ushort[] input, ushort scalar)
        {
            for (int i = 0; i < input.Length; i++)
                input[i] *= scalar;
            return input;
        }

        public static sbyte[] mult(sbyte[] input, sbyte scalar)
        {
            for (int i = 0; i < input.Length; i++)
                input[i] *= scalar;
            return input;
        }

        public static void add([In] byte[] a, [In] byte[] b, [Out] byte[] c)
        {
            for (int i = 0; i < c.Length; i++)
                c[i] = (byte)(a[i] + b[i]);
        }

        public static void add([In] sbyte[] a, [In] sbyte[] b, [Out] sbyte[] c)
        {
            for (int i = 0; i < c.Length; i++)
                c[i] = (sbyte)(a[i] + b[i]);
        }

        public static void add([In] short[] a, [In] short[] b, [Out] short[] c)
        {
            for (int i = 0; i < c.Length; i++)
                c[i] = (short)(a[i] + b[i]);
        }

        public static void add([In] ushort[] a, [In] ushort[] b, [Out] ushort[] c)
        {
            for (int i = 0; i < c.Length; i++)
                c[i] = (ushort)(a[i] + b[i]);
        }

        public static void add([In] int[] a, [In] int[] b, [Out] int[] c)
        {
            for (int i = 0; i < c.Length; i++)
                c[i] = a[i] + b[i];
        }

        public static void add([In] uint[] a, [In] uint[] b, [Out] uint[] c)
        {
            for (int i = 0; i < c.Length; i++)
                c[i] = a[i] + b[i];
        }

        public static void add([In] long[] a, [In] long[] b, [Out] long[] c)
        {
            for (int i = 0; i < c.Length; i++)
                c[i] = a[i] + b[i];
        }

        public static void add([In] ulong[] a, [In]  ulong[] b, [Out] ulong[] c)
        {
            for (int i = 0; i < c.Length; i++)
                c[i] = a[i] + b[i];
        }

        public static void add([In] float[] a, [In] float[] b, [Out] float[] c)
        {
            for (int i = 0; i < c.Length; i++)
                c[i] = a[i] + b[i];
        }

        public static void add([In] double[] a, [In] double[] b, [Out] double[] c)
        {
            for (int i = 0; i < c.Length; i++)
                c[i] = a[i] + b[i];
        }

        /// <summary>
        /// The two arrays are a reference to the same element so any change to a should be immediately visible in b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static void testRef([Out] int[] a, [In] int[] b)
        {
            if (a != b)
                Math.Max(10, 10);
            if (a[0] != b[0])
                Math.Max(11, 11);
            a[0]++;
            if (a[0] != b[0])
                Math.Max(12, 12);
            b[0]--;
            if (a[0] != b[0])
                Math.Max(12, 12);
        }

        /// <summary>
        /// The two arrays are a reference to the same element so any change to a should be immediately visible in b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static void testRef2([In] int[] a, [Out] int[] b)
        {
            if (a != b)
                Math.Max(10, 10);
            if (a[0] != b[0])
                Math.Max(11, 11);
            if (a[0] != 42)
                Math.Max(12, 12);
            a[0]++;
            if (a[0] != b[0])
                Math.Max(13, 13);
            b[0]--;
            if (a[0] != b[0])
                Math.Max(14, 14);
        }

        public static byte[] byteArrayGenerateTest()
        {
            var x = new byte[100];
            for (int i = 0; i < x.Length; i++)
                x[i] = (byte)i;

            return x;
        }


        public static uint[] uintArrayGenerateTest()
        {
            var x = new uint[100];
            for (int i = 0; i < x.Length; i++)
                x[i] = (uint)i;

            return x;
        }

        public static int[] intArrayGenerateTest()
        {
            var x = new int[100];
            for (int i = 0; i < x.Length; i++)
                x[i] = i;

            return x;
        }

        public static double[] doubleArrayGenerateTest()
        {
            var x = new double[100];
            for (int i = 0; i < x.Length; i++)
                x[i] = i;

            return x;
        }

        public static float[] floatArrayGenerateTest()
        {
            var x = new float[100];
            for (int i = 0; i < x.Length; i++)
                x[i] = i;

            return x;
        }

        public static object[] boxedArrayGenerateTest()
        {
            var x = new object[6];
            x[0] = (byte)0;
            x[1] = (short)-5;
            x[2] = (int)-1;
            x[3] = (double)5.5;
            x[4] = (bool)false;
            x[5] = (bool)true;

            return x;
        }

    }
}
