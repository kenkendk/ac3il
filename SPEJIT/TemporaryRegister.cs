using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPEJIT
{
    /// <summary>
    /// Marker class for temporary registers
    /// </summary>
    internal class TemporaryRegister : AccCIL.IR.VirtualRegister
    {
        public TemporaryRegister(uint register)
            : base(register)
        {
        }
    }
}
