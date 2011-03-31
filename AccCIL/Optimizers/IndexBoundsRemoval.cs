using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccCIL.Optimizers
{
    public class IndexBoundsRemoval : IOptimizer
    {
        #region IOptimizer Members

        public string Name
        {
            get { return "IndexBoundsCheckRemoval"; }
        }

        public string Description
        {
            get { return "Removes all index bounds checks"; }
        }

        public OptimizationLevel IncludeLevel
        {
            get { return OptimizationLevel.Extreme; }
        }

        public void Optimize(IR.MethodEntry method, OptimizationLevel level)
        {
            foreach (var op in method.FlatInstructionList)
                op.IsIndexChecked = true;
        }

        #endregion
    }
}
