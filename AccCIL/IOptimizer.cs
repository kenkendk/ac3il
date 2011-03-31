using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccCIL
{
    /// <summary>
    /// An interface for an optimizer that works on the IL/CIL level of the code
    /// </summary>
    public interface IOptimizer
    {
        /// <summary>
        /// Gets the name/key for the optimization algorithm
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Gets a human readable description of the optimization algorithm
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the minimum optimization level that should enable the optimization automatically
        /// </summary>
        OptimizationLevel IncludeLevel { get; }

        /// <summary>
        /// Work method that performs the actual optimization
        /// </summary>
        /// <param name="method">The IR representation of code</param>
        /// <param name="level">The agressiveness of the optimization to perform</param>
        void Optimize(IR.MethodEntry method, OptimizationLevel level);
    }
}
