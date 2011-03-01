using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPEJIT
{
    public class EndianBitConverter
    {
        private byte[] m_storage;
        private uint m_position = 0;

        public EndianBitConverter(byte[] storage)
        {
            m_storage = storage;
        }


        public uint Position
        {
            get { return m_position; }
            set { m_position = value; }
        }

        /// <summary>
        /// Reads a byte value from storage, starting at the specified offset
        /// </summary>
        /// <param name="offset">The starting offset</param>
        /// <returns>The byte value read</returns>
        public byte ReadByte(uint offset)
        {
            return m_storage[offset];
        }

        /// <summary>
        /// Reads a byte value from storage, starting at the current position
        /// </summary>
        /// <param name="offset">The starting offset</param>
        /// <returns>The byte value read</returns>
        public byte ReadByte()
        {
            byte b = ReadByte(m_position);
            m_position++;
            return b;
        }

        /// <summary>
        /// Reads the short value from storage, starting at the specified offset
        /// </summary>
        /// <param name="offset">The starting offset</param>
        /// <returns>The short value read</returns>
        public ushort ReadUShort(uint offset)
        {
            return
                (ushort)((m_storage[offset] << (8 * 1)) |
                ((ushort)m_storage[offset + 1] << (8 * 0)));
        }

                /// <summary>
        /// Reads the short value from storage, starting at the current position
        /// </summary>
        /// <returns>The short value read</returns>
        public ushort ReadUShort()
        {
            ushort s = ReadUShort(m_position);
            m_position += 2;
            return s;
        }

        /// <summary>
        /// Reads the int value from storage, starting at the specified offset
        /// </summary>
        /// <param name="offset">The starting offset</param>
        /// <returns>The int value read</returns>
        public uint ReadUInt(uint offset)
        {
            return
                (uint)(m_storage[offset] << (8 * 3)) |
                ((uint)m_storage[offset + 1] << (8 * 2)) |
                ((uint)m_storage[offset + 2] << (8 * 1)) |
                ((uint)m_storage[offset + 3] << (8 * 0));
        }

        /// <summary>
        /// Reads the int value from storage, starting at the current position
        /// </summary>
        /// <returns>The int value read</returns>
        public uint ReadUInt()
        {
            uint i = ReadUInt(m_position);
            m_position += 4;
            return i;
        }

        /// <summary>
        /// Reads the long value from storage, starting at the specified offset
        /// </summary>
        /// <param name="offset">The starting offset</param>
        /// <returns>The long value read</returns>
        public ulong ReadULong(uint offset)
        {
            return
                (ulong)((ulong)m_storage[offset] << (8 * 7)) |
                ((ulong)m_storage[offset + 1] << (8 * 6)) |
                ((ulong)m_storage[offset + 2] << (8 * 5)) |
                ((ulong)m_storage[offset + 3] << (8 * 4)) |
                ((ulong)m_storage[offset + 4] << (8 * 3)) |
                ((ulong)m_storage[offset + 5] << (8 * 2)) |
                ((ulong)m_storage[offset + 6] << (8 * 1)) |
                ((ulong)m_storage[offset + 7] << (8 * 0));
        }

        /// <summary>
        /// Reads the long value from storage, starting at the current position
        /// </summary>
        /// <returns>The long value read</returns>
        public ulong ReadULong()
        {
            ulong l = ReadULong(m_position);
            m_position += 4;
            return l;
        }

        public float ReadFloat()
        {
            float f;
            if (BitConverter.IsLittleEndian)
            {
                byte[] tmp = new byte[4];
                Array.Copy(m_storage, m_position, tmp, 0, 4);
                Array.Reverse(tmp);
                f = BitConverter.ToSingle(tmp, 0);
            }
            else
                f = BitConverter.ToSingle(m_storage, (int)m_position);

            m_position += 4;
            return f;
        }

        public double ReadDouble()
        {
            double f;
            if (BitConverter.IsLittleEndian)
            {
                byte[] tmp = new byte[8];
                Array.Copy(m_storage, m_position, tmp, 0, 8);
                Array.Reverse(tmp);
                f = BitConverter.ToDouble(tmp, 0);
            }
            else
                f = BitConverter.ToDouble(m_storage, (int)m_position);

            m_position += 8;
            return f;
        }

        /// <summary>
        /// Writes a byte value to storage, starting at the specified offset
        /// </summary>
        /// <param name="offset">The starting offset</param>
        /// <param name="value">The value to write</param>
        public void WriteByte(uint offset, byte value)
        {
            m_storage[offset] = value;
        }

        /// <summary>
        /// Writes a byte value to storage, starting at the current position
        /// </summary>
        /// <param name="value">The value to write</param>
        public void WriteByte(byte value)
        {
            WriteByte(m_position, value);
            m_position++;
        }

        /// <summary>
        /// Writes a short value to storage, starting at the specified offset
        /// </summary>
        /// <param name="offset">The starting offset</param>
        /// <param name="value">The value to write</param>
        public void WriteUShort(uint offset, ushort value)
        {
            m_storage[offset] = (byte)((value >> (8 * 1)) & 0xff);
            m_storage[offset + 1] = (byte)((value >> (8 * 0)) & 0xff);
        }

                /// <summary>
        /// Writes a short value to storage, starting at the current position
        /// </summary>
        /// <param name="value">The value to write</param>
        public void WriteUShort(ushort value)
        {
            WriteUShort(m_position, value);
            m_position += 2;
        }

        /// <summary>
        /// Writes a int value to storage, starting at the specified offset
        /// </summary>
        /// <param name="offset">The starting offset</param>
        /// <param name="value">The value to write</param>
        public void WriteUInt(uint offset, uint value)
        {
            m_storage[offset] = (byte)((value >> (8 * 3)) & 0xff);
            m_storage[offset + 1] = (byte)((value >> (8 * 2)) & 0xff);
            m_storage[offset + 2] = (byte)((value >> (8 * 1)) & 0xff);
            m_storage[offset + 3] = (byte)((value >> (8 * 0)) & 0xff);
        }

        /// <summary>
        /// Writes a int value to storage, starting at the current position
        /// </summary>
        /// <param name="value">The value to write</param>
        public void WriteUInt(uint value)
        {
            WriteUInt(m_position, value);
            m_position += 4;
        }

        /// <summary>
        /// Writes a long value to storage, starting at the specified offset
        /// </summary>
        /// <param name="offset">The starting offset</param>
        /// <param name="value">The value to write</param>
        public void WriteULong(uint offset, ulong value)
        {
            m_storage[offset] = (byte)((value >> (8 * 7)) & 0xff);
            m_storage[offset + 1] = (byte)((value >> (8 * 6)) & 0xff);
            m_storage[offset + 2] = (byte)((value >> (8 * 5)) & 0xff);
            m_storage[offset + 3] = (byte)((value >> (8 * 4)) & 0xff);
            m_storage[offset + 4] = (byte)((value >> (8 * 3)) & 0xff);
            m_storage[offset + 5] = (byte)((value >> (8 * 2)) & 0xff);
            m_storage[offset + 6] = (byte)((value >> (8 * 1)) & 0xff);
            m_storage[offset + 7] = (byte)((value >> (8 * 0)) & 0xff);
        }

        /// <summary>
        /// Writes a int value to storage, starting at the current position
        /// </summary>
        /// <param name="value">The value to write</param>
        public void WriteULong(ulong value)
        {
            WriteULong(m_position, value);
            m_position += 8;
        }

        public void WriteFloat(float value)
        {
            byte[] d = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(d);
            Array.Copy(d, 0, m_storage, m_position, 4);
            m_position += 4;
        }

        public void WriteDouble(double value)
        {
            byte[] d = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(d);
            Array.Copy(d, 0, m_storage, m_position, 8);
            m_position += 8;
        }
    }
}
