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
            int x = Factorial(10);
            Console.WriteLine(x);
        }

        public static int Factorial(int number)
        {
            if (number == 0)
                return 1;
            else
                return number * Factorial(number - 1);
        }
    }
}
