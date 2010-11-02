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

        public static int sum(int[] test)
        {
            int sum = 0;
            foreach (int i in test)
                sum += i;
            return sum;
        }
    }
}
