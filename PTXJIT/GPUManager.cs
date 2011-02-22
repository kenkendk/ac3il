using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace PTXJIT
{
    public class GPUManager : AccCIL.AccelleratorBase
    {

        protected override T DoAccelerate<T>(string assembly, string methodName, params object[] args)
        {
            throw new NotImplementedException();
        }

        public override void LoadProgram(IEnumerable<AccCIL.ICompiledMethod> methods)
        {
            throw new NotImplementedException();
        }

        public override object Execute(params object[] arguments)
        {
            throw new NotImplementedException();
        }
    }
}
