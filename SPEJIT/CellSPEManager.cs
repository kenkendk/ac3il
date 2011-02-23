using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPEJIT
{
    /// <summary>
    /// Implementation of an SPE controller for a physical SPE device
    /// </summary>
    class CellSPEManager : AccCIL.AccelleratorBase
    {
        private SPEJIT.SPEJITCompiler m_compiler = new SPEJITCompiler();

        public override void LoadProgram(IEnumerable<AccCIL.ICompiledMethod> methods)
        {
            throw new NotImplementedException();
        }

        public override T Execute<T>(params object[] arguments)
        {
            throw new NotImplementedException();
        }

        protected override AccCIL.IJITCompiler Compiler
        {
            get { return m_compiler; }
        }
    }
}
