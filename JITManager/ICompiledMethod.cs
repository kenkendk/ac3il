using System;
namespace JITManager
{
    public interface ICompiledMethod
    {
        IR.MethodEntry Method { get; }
    }
}
