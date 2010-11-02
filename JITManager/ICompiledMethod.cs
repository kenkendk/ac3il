using System;
namespace JITManager
{
    public interface ICompiledMethod
    {
        void EndFunction();
        void EndInstruction();
        uint MaxStackDepth { get; }
        void PatchCalls(System.Collections.Generic.Dictionary<Mono.Cecil.MethodDefinition, int> methodOffsets, int callhandlerOffset);
        void RegisterBranch(Mono.Cecil.Cil.Instruction target);
        void RegisterCall(Mono.Cecil.MethodDefinition t);
        void RegisterLabel(string label, int offset);
        int StackDepth { get; set; }
        void StartFunction();
        void StartInstruction(Mono.Cecil.Cil.Instruction instr);
        IR.MethodEntry Method { get; }
    }
}
