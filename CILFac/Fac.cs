using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CILFac
{
    public static class Fac
    {
        public static long Factorial(long number)
        {
            if (number == 0)
                return 1;
            else
                return number * Factorial(number - 1);
        }
    }
}
