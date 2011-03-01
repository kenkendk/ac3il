using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public static byte sum(byte[] test)
        {
            byte sum = 0;
            foreach (byte i in test)
                sum += i;
            return sum;
        }

        public static sbyte sum(sbyte[] test)
        {
            sbyte sum = 0;
            foreach (sbyte i in test)
                sum += i;
            return sum;
        }

        public static short sum(short[] test)
        {
            short sum = 0;
            foreach (short i in test)
                sum += i;
            return sum;
        }

        public static ushort sum(ushort[] test)
        {
            ushort sum = 0;
            foreach (ushort i in test)
                sum += i;
            return sum;
        }

        public static uint sum(uint[] test)
        {
            uint sum = 0;
            foreach (uint i in test)
                sum += i;
            return sum;
        }


        public static int sum(int[] test)
        {
            int sum = 0;
            foreach (int i in test)
                sum += i;
            return sum;
        }

        public static long sum(long[] test)
        {
            long sum = 0;
            foreach (long i in test)
                sum += i;
            return sum;
        }

        public static ulong sum(ulong[] test)
        {
            ulong sum = 0;
            foreach (ulong i in test)
                sum += i;
            return sum;
        }


        public static float sum(float[] test)
        {
            float sum = 0;
            foreach (float i in test)
                sum += i;
            return sum;
        }

        public static double sum(double[] test)
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
    }
}
