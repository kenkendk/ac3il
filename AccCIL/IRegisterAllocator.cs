using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccCIL
{
    /// <summary>
    /// Interface for a register allocator
    /// </summary>
    public interface IRegisterAllocator
    {
        /// <summary>
        /// Allocates registers for the given method
        /// </summary>
        /// <param name="registers">The available registers</param>
        /// <param name="method">The method that needs register allocations</param>
        /// <returns>The list of used registers</returns>
        List<int> AllocateRegisters(Stack<int> registers, IR.MethodEntry method);
    }
}
