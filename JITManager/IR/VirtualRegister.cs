using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JITManager.IR
{
    /// <summary>
    /// Represents an operand to an instruction
    /// </summary>
    public class VirtualRegister : InstructionElement
    {
        /// <summary>
        /// The physical register assigned to the virtual register, negative numbers means no register,
        /// and thus must use stack for storing
        /// </summary>
        public int RegisterNumber;
    }
}
