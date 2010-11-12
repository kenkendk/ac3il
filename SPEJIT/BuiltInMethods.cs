using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPEJIT
{
    internal class BuiltInMethods
    {
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
