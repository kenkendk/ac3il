using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CILFac
{
    public static class Fac
    {
        public static void SPE_Main()
        {
            long test;

            //TestLogicals();

            //Unchecked required because we overflow a long with the constants
            unchecked
            {
                long a;
                long b;

                a = 0xffffffff;
                b = -5;
                test = a * b;
                if (test != (long)0xFFFFFFFB00000005)
                    test = Math.Max(test, (long)0xFFFFFFFB00000005);

                a = 0xffff;
                b = 0xffffa;

                test = a * b;
                if (test != 0xFFFEA0006L)
                    test = Math.Max(test, 0xFFFEA0006L);

                
                a = 0xffff;
                b = 0xa;
                test = a * b;
                if (test != 0x9FFF6L)
                    test = Math.Max(test, 0x9FFF6L);

                a = 0xffffffff;
                b = 0xafffa;
                test = a * b;
                if (test != 0xAFFF9FFF50006L)
                    test = Math.Max(test, 0xAFFF9FFF50006L);

                a = 0xffffffff;
                b = 0xffffffff;
                test = a * b;
                if (test != (long)(0xFFFFFFFE00000001L))
                    test = Math.Max(test, (long)0xFFFFFFFE00000001L);
            }

            long x = Factorial(10);
            Console.WriteLine(x);
        }

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

        public static long Factorial(long number)
        {
            if (number == 0)
                return 1;
            else
                return number * Factorial(number - 1);
        }
    }
}
