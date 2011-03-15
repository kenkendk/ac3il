using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPEJIT
{
    internal class BuiltInSPEMethods
    {
        /// <summary>
        /// Emulated 64 bit multiplication with 16 bit multiply
        /// </summary>
        /// <param name="a">Operand a</param>
        /// <param name="b">Operand b</param>
        /// <returns>The result of multiplying a with b</returns>
        private static ulong umul(ulong a, ulong b)
        {
            uint a3 = (uint)(a & 0xffff);
            uint a2 = (uint)((a >> 16) & 0xffff);
            uint a1 = (uint)((a >> 32) & 0xffff);
            uint a0 = (uint)((a >> 48) & 0xffff);

            uint b3 = (uint)(b & 0xffff);
            uint b2 = (uint)((b >> 16) & 0xffff);
            uint b1 = (uint)((b >> 32) & 0xffff);
            uint b0 = (uint)((b >> 48) & 0xffff);

            ulong r = a3 * b3;
            r += ((ulong)(a3 * b2) + (ulong)(a2 * b3)) << 16;
            r += ((ulong)(a3 * b1) + (ulong)(a2 * b2) + (ulong)(a1 * b3)) << 32;
            r += ((ulong)(a2 * b1) + (ulong)(a1 * b2) + (ulong)(a3 * b0) + (ulong)(a0 * b3)) << 48;

            return r;
        }

        /// <summary>
        /// Gets the size factor of an array element as a factor of two.
        /// i.e. the element size is 2^n where n is the returned value
        /// 
        /// Even though this function is loaded onto the SPE,
        /// it is also used when performing the JIT compilation and
        /// when transfering objects.
        /// </summary>
        /// <param name="objtype">The array element type</param>
        /// <returns>The size of the array element as a factor of two</returns>
        internal static uint get_array_elem_len_mult(uint t)
        {
            AccCIL.KnownObjectTypes objtype = (AccCIL.KnownObjectTypes)(t & 0xff);

            switch (objtype)
            {
                case AccCIL.KnownObjectTypes.Boolean:
                case AccCIL.KnownObjectTypes.Byte:
                case AccCIL.KnownObjectTypes.SByte:
                    return 0;
                case AccCIL.KnownObjectTypes.Short:
                case AccCIL.KnownObjectTypes.UShort:
                    return 1;
                case AccCIL.KnownObjectTypes.Int:
                case AccCIL.KnownObjectTypes.UInt:
                case AccCIL.KnownObjectTypes.Float:
                case AccCIL.KnownObjectTypes.Object:
                case AccCIL.KnownObjectTypes.String:
                    return 2;
                case AccCIL.KnownObjectTypes.Long:
                case AccCIL.KnownObjectTypes.ULong:
                case AccCIL.KnownObjectTypes.Double:
                    return 3;
                default:
                    //TODO: Throw exception?
                    return 0;
            }
        }

        internal static uint malloc(uint[] objtable, AccCIL.KnownObjectTypes type, uint size, uint typestring_index)
        {
            if (objtable[1] >= objtable[0])
            {
                //TODO: Do garbage collection and re-check space
                //NOTE: When GC is done, re-visit the ldelema instruction
                //TODO: throw new OutOfMemoryException();
                return 0;
            }

            uint nextoffset = objtable[2];
            uint requiredSpace = (size + 15 >> 4) << 4;

            //TODO: Do not rely on SPE mem size, but rather $SP
            if (nextoffset + requiredSpace >= objtable[3])
            {
                //TODO: Do garbage collection and re-check space
                //NOTE: When GC is done, re-visit the ldelema instruction
                //TODO: throw new OutOfMemoryException();
                return 0;
            }

            uint nextel = objtable[1];
            objtable[2] = nextoffset + requiredSpace;

            uint realtype;

            //For objects, we use the upper 16 bits to encode the typestring index
            if (typestring_index == 0)
                realtype = (uint)type & 0xffu;
            else
            {
                realtype = (typestring_index << 16) | ((uint)type & 0xffu);
                objtable[typestring_index * 4 + 3]++; //Increment refcount
            }

            uint n = nextel * 4;

            objtable[1] = objtable[n + 3];

            objtable[n] = realtype;
            objtable[n + 1] = size;
            objtable[n + 2] = nextoffset;
            objtable[n + 3] = 1;

            return nextel;
        }

        internal static void free(uint[] objtable, uint index)
        {
            if (index >= objtable[0])
            {
                //TODO: throw new InvalidPointerException();
                return;
            }

            uint elindex = (index + 1) * 4;
            uint size = objtable[elindex + 1];
            uint requiredSpace = size + ((16 - size % 16) % 16);
            //If this is the element created most recently, we can simply reclaim the space here
            if (objtable[elindex + 0] == objtable[2] - requiredSpace)
                objtable[2] = objtable[elindex + 0];

            //TODO: Enable this once the ref-counts work
            //if (objtable[elindex + 3] != 0)
            //    throw new InvalidOperationException();

            objtable[elindex + 0] = (uint)AccCIL.KnownObjectTypes.Free;
            objtable[elindex + 1] = (uint)AccCIL.KnownObjectTypes.Free;
            objtable[elindex + 2] = (uint)AccCIL.KnownObjectTypes.Free;
            objtable[elindex + 0] = objtable[1];
            objtable[1] = index;
        }
    }
}
