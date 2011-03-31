using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccCIL
{
    public interface IJITCompiler
    {
        /// <summary>
        /// Gets a list of active optimizers
        /// </summary>
        IList<IOptimizer> Optimizers { get; }

        /// <summary>
        /// Gets or sets the current optimization level
        /// </summary>
        OptimizationLevel OptimizationLevel { get; set; }

        /// <summary>
        /// Compiles a single method
        /// </summary>
        /// <param name="method">The method to compile</param>
        /// <returns>The compiled method</returns>
        ICompiledMethod JIT(IR.MethodEntry method);
    }
}
