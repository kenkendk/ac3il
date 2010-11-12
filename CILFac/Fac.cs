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
                test = (long)umul(0xffff, 0xffffa);
                if (test != 0xFFFEA0006L)
                    test = Math.Max(test, 0xFFFEA0006L);

                test = mul(0xffffffff, -5);
                if (test != (long)0xFFFFFFFB00000005)
                    test = Math.Max(test, (long)0xFFFFFFFB00000005);

                test = mul(0xffff, 0xa);
                if (test != 0x9FFF6L)
                    test = Math.Max(test, 0x9FFF6L);

                test = mul(0xffffffff, 0xafffa);
                if (test != 0xAFFF9FFF50006L)
                    test = Math.Max(test, 0xAFFF9FFF50006L);

                test = mul(0xffffffff, 0xffffffff);
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
                return mul(number, Factorial(number - 1));
        }


        /// <summary>
        /// Emulated 64 bit multiplication with 16 bit multiply
        /// </summary>
        /// <param name="a">Operand a</param>
        /// <param name="b">Operand b</param>
        /// <returns>The result of multiplying a with b</returns>
        private static long mul(long a, long b)
        {
            bool isANegative = a < 0;
            bool isBNegative = b < 0;
            bool invertResult = isANegative != isBNegative;

            if (isANegative)
                a = ~(a - 1);
            if (isBNegative)
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
