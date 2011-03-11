using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccCIL.IR
{
    /// <summary>
    /// Represents an operand to an instruction
    /// </summary>
    public class VirtualRegister
    {
        private static object _lock = new object();
        private static int _nextRegNo = -1;

        /// <summary>
        /// The physical register assigned to the virtual register, negative numbers means no register,
        /// and thus must use stack for storing
        /// </summary>
        public int RegisterNumber;

        public VirtualRegister()
        {
            lock (_lock)
                RegisterNumber = _nextRegNo--;
        }

        public VirtualRegister(uint number)
        {
            RegisterNumber = (int)number;
        }

        public override string ToString()
        {
            return "Register $" + RegisterNumber.ToString();
        }
    }
}
