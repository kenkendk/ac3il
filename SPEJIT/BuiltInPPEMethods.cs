using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPEJIT
{
    internal class BuiltInPPEMethods
    {
        private static object Box_Boolean(byte b) { return b; }
        private static object Box_Byte(byte b) { return b; }
        private static object Box_SByte(sbyte b) { return b; }
        private static object Box_Short(short b) { return b; }
        private static object Box_UShort(ushort b) { return b; }
        private static object Box_Int(int b) { return b; }
        private static object Box_UInt(uint b) { return b; }
        private static object Box_Long(long b) { return b; }
        private static object Box_ULong(ulong b) { return b; }
        private static object Box_Float(float b) { return b; }
        private static object Box_Double(double b) { return b; }
    }
}
