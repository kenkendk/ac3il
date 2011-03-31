using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccCIL
{
    /// <summary>
    /// Defines type IDs for built-in types
    /// </summary>
    public enum KnownObjectTypes : uint
    {
        Free = 0x0,
        ObjectTable,
        Bootloader,
        Code,
        Boolean,
        Byte,
        SByte,
        Short,
        UShort,
        Int,
        UInt,
        Long,
        ULong,
        Float,
        Double,
        String,
        Object,
    }

    public enum OptimizationLevel
    {
        /// <summary>
        /// No optimizations should be done
        /// </summary>
        None = 0,
        /// <summary>
        /// Low impact optimizations should be done
        /// </summary>
        Low,
        /// <summary>
        /// Optimizations that can be executed in linear time should be performed
        /// </summary>
        Medium,
        /// <summary>
        /// Most optimizations should be performed, including those running in non-linear time
        /// </summary>
        High,
        /// <summary>
        /// All optimizations should be performed as agressively as possible
        /// </summary>
        Full,
        /// <summary>
        /// Perform all optimizations and remove all security checks, such as array bounds, null pointers, etc.
        /// </summary>
        Extreme
    }
}
