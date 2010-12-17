using System;

namespace AccCIL
{
    public interface ICompiledMethod
    {
        IR.MethodEntry Method { get; }
    }
}
