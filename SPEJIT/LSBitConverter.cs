using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPEJIT
{
    /// <summary>
    /// Represents a bitconverter that writes directly into the SPE LS area.
    /// In this class we do not worry about endianness because it will
    /// always be executed on a PPC which has is big endian just as the SPEs
    /// </summary>
    internal class LSBitConverter : SPEEmulator.IEndianBitConverter, IDisposable
    {
        private uint m_position = 0;
        private IntPtr m_lsArea;
        private IntPtr m_buffer;

        private byte[] m_tmp = new byte[8];

        private const int MAX_MEMCPY_SIZE = 4 * 1024;

        internal LSBitConverter(IntPtr ls)
        {
            if (ls == IntPtr.Zero)
                throw new ArgumentNullException("ls");
            m_lsArea = ls;
            m_buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(8);
        }

        #region IEndianBitConverter Members

        public byte[] Data
        {
            get { throw new NotImplementedException(); }
        }

        public uint Position
        {
            get
            {
                return m_position;
            }
            set
            {
                m_position = 0;
            }
        }

        public byte ReadByte()
        {
            byte b = ReadByte(m_position);
            m_position++;
            return b;
        }

        public byte ReadByte(uint offset)
        {
            return System.Runtime.InteropServices.Marshal.ReadByte(m_lsArea, (int)offset);
        }

        public double ReadDouble()
        {
            double d = ReadDouble(m_position);
            m_position += 8;
            return d;
        }

        public double ReadDouble(uint offset)
        {
            IntPtr adr = IntPtr.Add(m_lsArea, (int)offset);
            System.Runtime.InteropServices.Marshal.Copy(adr, m_tmp, 0, 8);
            return BitConverter.ToDouble(m_tmp, 0);
        }

        public float ReadFloat()
        {
            float f = ReadFloat(m_position);
            m_position += 4;
            return m_position;
        }

        public float ReadFloat(uint offset)
        {
            IntPtr adr = IntPtr.Add(m_lsArea, (int)offset);
            System.Runtime.InteropServices.Marshal.Copy(adr, m_tmp, 0, 4);
            return BitConverter.ToSingle(m_tmp, 0);
        }

        public uint ReadUInt()
        {
            uint i = ReadUInt(m_position);
            m_position += 4;
            return i;
        }

        public uint ReadUInt(uint offset)
        {
            return (uint)System.Runtime.InteropServices.Marshal.ReadInt32(m_lsArea, (int)offset);
        }

        public ulong ReadULong()
        {
            ulong l = ReadULong(m_position);
            m_position += 8;
            return l;
        }

        public ulong ReadULong(uint offset)
        {
            return (ulong)System.Runtime.InteropServices.Marshal.ReadInt64(m_lsArea, (int)offset);
        }

        public ushort ReadUShort()
        {
            ushort s = ReadUShort(m_position);
            m_position += 2;
            return s;
        }

        public ushort ReadUShort(uint offset)
        {
            return (ushort)System.Runtime.InteropServices.Marshal.ReadInt16(m_lsArea, (int)offset);
        }

        public void WriteByte(byte value)
        {
            WriteByte(m_position, value);
            m_position++;
        }

        public void WriteByte(uint offset, byte value)
        {
            System.Runtime.InteropServices.Marshal.WriteByte(m_lsArea, (int)offset, value);
        }

        public void WriteDouble(double value)
        {
            WriteDouble(m_position, value);
            m_position += 8;
        }

        public void WriteDouble(uint offset, double value)
        {
            byte[] data = BitConverter.GetBytes(value);
            System.Runtime.InteropServices.Marshal.Copy(data, 0, m_lsArea, 8);
        }

        public void WriteFloat(float value)
        {
            WriteFloat(m_position, value);
            m_position += 4;
        }

        public void WriteFloat(uint offset, float value)
        {
            byte[] data = BitConverter.GetBytes(value);
            System.Runtime.InteropServices.Marshal.Copy(data, 0, m_lsArea, 4);
        }

        public void WriteUInt(uint offset, uint value)
        {
            System.Runtime.InteropServices.Marshal.WriteInt32(m_lsArea, (int)offset, (int)value);
        }

        public void WriteUInt(uint value)
        {
            WriteUInt(m_position, value);
            m_position += 4;
        }

        public void WriteULong(uint offset, ulong value)
        {
            System.Runtime.InteropServices.Marshal.WriteInt64(m_lsArea, (int)offset, (long)value);
        }

        public void WriteULong(ulong value)
        {
            WriteULong(m_position, value);
            m_position+= 8;
        }

        public void WriteUShort(ushort value)
        {
            WriteUShort(m_position, value);
            m_position += 2;
        }

        public void WriteUShort(uint offset, ushort value)
        {
            System.Runtime.InteropServices.Marshal.WriteInt16(m_lsArea, (int)offset, (short)value);
        }

        public void WriteBytes(uint offset, byte[] data)
        {
            //TODO: DMA transfer
            if (data.Length > MAX_MEMCPY_SIZE)
                throw new Exception(string.Format("Unsupported transfer, size is {0} and must be less than {1}", data.Length, MAX_MEMCPY_SIZE));

            IntPtr adr = IntPtr.Add(m_lsArea, (int)offset);
            System.Runtime.InteropServices.Marshal.Copy(data, 0, adr, data.Length);
        }

        public void WriteBytes(byte[] data)
        {
            WriteBytes(m_position, data);
            m_position += (uint)data.Length;
        }

        public void ReadBytes(uint offset, byte[] data)
        {
            //TODO: DMA transfer
            if (data.Length > MAX_MEMCPY_SIZE)
                throw new Exception(string.Format("Unsupported transfer, size is {0} and must be less than {1}", data.Length, MAX_MEMCPY_SIZE));

            IntPtr adr = IntPtr.Add(m_lsArea, (int)offset);
            System.Runtime.InteropServices.Marshal.Copy(adr, data, 0, data.Length);
        }

        public void ReadBytes(byte[] data)
        {
            ReadBytes(m_position, data);
            m_position += (uint)data.Length;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (m_buffer != IntPtr.Zero)
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(m_buffer);
                m_buffer = IntPtr.Zero;
            }
        }

        #endregion
    }
}
