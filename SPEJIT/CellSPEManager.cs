using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPEJIT
{
    /// <summary>
    /// Implementation of an SPE controller for a physical SPE device
    /// </summary>
    class CellSPEManager : AccCIL.IAccellerator
    {
        #region ISPEManager Members

        public void LoadProgram(IEnumerable<AccCIL.ICompiledMethod> methods)
        {
            throw new NotImplementedException();
        }

        public object Execute(params object[] arguments)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
