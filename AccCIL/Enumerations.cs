using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccCIL
{
    /// <summary>
    /// Defines type IDs for built-in types
    /// </summary>
    public enum KnownObjectTypes : int
    {
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
        Bootloader,
        Code,
        TypeTable
    }
}
