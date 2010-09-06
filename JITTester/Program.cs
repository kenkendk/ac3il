using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JITTester
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                SPEJIT.TestJIT.AttemptJIT();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey(true);
        }
    }
}
