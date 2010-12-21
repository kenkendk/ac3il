using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccCIL
{
    /// <summary>
    /// Simple register allocator that merely assigns registers until it runs out
    /// </summary>
    public class SimpleAllocator : IRegisterAllocator
    {
        #region IRegisterAllocator Members

        public void AllocateRegistersRecursive(IR.InstructionElement current, Stack<int> registers, IR.MethodEntry method, List<int> used, Dictionary<int, int> usageCount)
        {
            //Does this instruction have an output?
            if (current.Register != null)
            {
                //TODO: If the child node has a register assigned (eg. from a function call)
                // it should be used here instead of a new assignment

                //Have we already assigned a register?
                if (current.Register.RegisterNumber < 0 && registers.Count > 0)
                {
                    int regno = registers.Pop();
                    used.Add(regno);

                    usageCount.Add(regno, 0);

                    current.Register.RegisterNumber = regno;
                }

                if (current.Register.RegisterNumber > 0 && usageCount.ContainsKey(current.Register.RegisterNumber))
                    usageCount[current.Register.RegisterNumber]++;

                //Assign the target register to the child node
                if (current.Register.RegisterNumber > 0 && current.Childnodes.Length != 0 && current.Childnodes[0].Register != null)
                    current.Childnodes[0].Register = current.Register;
            }

            foreach (IR.InstructionElement i in current.Childnodes)
                AllocateRegistersRecursive(i, registers, method, used, usageCount);

            if (current.Register != null && current.Register.RegisterNumber > 0 && usageCount.ContainsKey(current.Register.RegisterNumber))
            {
                usageCount[current.Register.RegisterNumber]--;
                if (usageCount[current.Register.RegisterNumber] == 0)
                {
                    usageCount.Remove(current.Register.RegisterNumber);
                    registers.Push(current.Register.RegisterNumber);
                }
            }
        }

        public List<int> AllocateRegisters(Stack<int> registers, IR.MethodEntry method)
        {
            List<int> used = new List<int>();
            Dictionary<int, int> usageCount = new Dictionary<int, int>();

            foreach (IR.InstructionElement i in method.Childnodes)
                AllocateRegistersRecursive(i, registers, method, used, usageCount);

            return used;
        }

        #endregion
    }
}
