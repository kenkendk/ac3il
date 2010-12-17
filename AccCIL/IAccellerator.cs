using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccCIL
{
    public interface IAccellerator
    {
        /// <summary>
        /// Prepares the SPE by loading compiled methods and control code onto it
        /// </summary>
        /// <param name="methods">The list of methods to place on the SPE, the first method in the list MUST be the entry method</param>
        void LoadProgram(IEnumerable<ICompiledMethod> methods);

        /// <summary>
        /// Executed the entry method on the SPE with the given arguments
        /// </summary>
        /// <param name="arguments">The arguments for the entry method</param>
        /// <returns>The result of running the method on the SPE</returns>
        object Execute(params object[] arguments);
    }
}
