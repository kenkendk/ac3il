using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JITManager
{
    public interface IJITCompiler
    {
        ICompiledMethod JIT(IR.MethodEntry method);
    }
}
