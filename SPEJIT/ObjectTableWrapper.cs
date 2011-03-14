using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPEJIT
{
    internal class ObjectTableEntry
    {
        private uint m_index;
        private uint m_offset;
        private uint[] m_data;

        public AccCIL.KnownObjectTypes KnownType { get { return (AccCIL.KnownObjectTypes)(m_data[m_offset] & 0xff); } set { m_data[m_offset] = (m_data[m_offset] & ~0xffu) | (uint)value; } }
        public uint Index { get { return m_index; } set { m_index = value; } }
        public uint Size { get { return m_data[m_offset + 1]; } set { m_data[m_offset + 1] = value; } }
        public uint Offset { get { return m_data[m_offset + 2]; } set { m_data[m_offset + 2] = value; } }
        public uint Refcount { get { return m_data[m_offset + 3]; } set { m_data[m_offset + 3] = value; } }
        public uint Type { get { return m_data[m_offset] >> 16; } set { m_data[m_offset] = (m_data[m_offset] & 0xff) | (value << 16); } }

        public uint AlignedSize { get { return (this.Size + 15 >> 4) << 4; } }

        public ObjectTableEntry(uint index, uint[] data)
        {
            m_index = index;
            m_offset = m_index * 4;
            m_data = data;
        }

        public override string ToString()
        {
            if (KnownType == AccCIL.KnownObjectTypes.Free)
                return string.Format("# [{0}] Free, next {1}", Index, Refcount);
            else
                return string.Format("# [{0}] Type {1}:{2}, size: {3}, offset: 0x{4:x4}, refCount: {5}", Index, KnownType, Type, Size, Offset, Refcount);
        }
    }

    /// <summary>
    /// A helper class that provides typesafe access to the SPE object table
    /// </summary>
    internal class ObjectTableWrapper : IEnumerable<ObjectTableEntry>
    {
        private Dictionary<string, uint> m_strings;
        private uint[] m_data;

        private ObjectTableEntry[] m_items;

        public uint Size { get { return m_data[0]; } }
        public uint NextFree { get { return m_data[1]; } set { m_data[1] = value; } }
        public uint NextOffset { get { return m_data[2]; } set { m_data[2] = value; } }
        public uint Memsize { get { return m_data[3]; } }
        public uint[] Data { get { return m_data; } }

        public ObjectTableWrapper(uint size, uint memSize)
        {
            m_data = new uint[size * 4];
            m_data[0] = size;
            this.NextFree = 1;
            this.NextOffset = SPEJITCompiler.OBJECT_TABLE_OFFSET;
            m_data[3] = memSize;

            m_items = new ObjectTableEntry[this.Size];
            for (uint i = 1; i < size; i++)
            {
                var r = new ObjectTableEntry(i, m_data);
                r.Refcount = i + 1;
                r.KnownType = AccCIL.KnownObjectTypes.Free;
                r.Size = 0;
                r.Offset = 0;
                m_items[i - 1] = r;
            }

            m_strings = new Dictionary<string, uint>();
        }

        public ObjectTableWrapper(uint[] data)
        {
            m_data = data;
            m_strings = new Dictionary<string, uint>();
            m_items = new ObjectTableEntry[this.Size];

            for (uint i = 1; i < this.Size; i++)
                m_items[i - 1] = new ObjectTableEntry(i, m_data);
        }


        public IDictionary<string, uint> Strings { get { return m_strings; } }

        public ObjectTableEntry this[uint index]
        {
            get
            {
                return m_items[index - 1];
            }
        }

        public uint AddObject(AccCIL.KnownObjectTypes type, uint size, string typestring)
        {
            if (this.NextFree == this.Size)
                throw new Exception("Out of space in object table");

            ObjectTableEntry e = this[this.NextFree];
            System.Diagnostics.Debug.Assert(e.KnownType == AccCIL.KnownObjectTypes.Free);

            uint alignedSize = (size + 15 >> 4) << 4;

            if (alignedSize + this.NextOffset > this.Memsize)
                throw new Exception("Out of memory one SPE");

            uint n = e.Refcount;

            e.KnownType = type;
            e.Type = 0;

            e.Offset = this.NextOffset;
            e.Size = size;
            e.Refcount = 0;

            this.NextOffset += alignedSize;
            this.NextFree = n;

            if (typestring != null)
            {
                if (!m_strings.ContainsKey(typestring))
                    m_strings.Add(typestring, AddObject(AccCIL.KnownObjectTypes.String, (uint)System.Text.Encoding.UTF8.GetByteCount(typestring), null));

                e.Type = m_strings[typestring];
                this[e.Type].Refcount++;
            }

            return e.Index;
        }


        public void RemoveObject(uint index)
        {
            if (index == 0)
                throw new ArgumentNullException();

            if (index >= this.Size)
                throw new Exception("Invalid pointer");

            ObjectTableEntry e = this[index];

            if (e.KnownType == AccCIL.KnownObjectTypes.String)
            {
                foreach(KeyValuePair<string, uint> s in this.Strings)
                    if (s.Value == e.Index)
                    {
                        this.Strings.Remove(s.Key);
                        break;
                    }
            }

            if (e.Type != 0)
            {
                this[e.Type].Refcount--;
                if (this[e.Type].Refcount == 0)
                    RemoveObject(e.Type);
            }
            
            //Reclaim space if possible
            if (this.NextOffset == e.Offset + e.AlignedSize)
                this.NextOffset = e.Offset;

            e.KnownType = AccCIL.KnownObjectTypes.Free;
            e.Offset = 0;
            e.Size = 0;
            e.Type = 0;
            e.Refcount = this.NextFree;
            this.NextFree = e.Index;

        }

        #region IEnumerable<ObjectTableEntry> Members

        public IEnumerator<ObjectTableEntry> GetEnumerator()
        {
            return new EntryEnumerator(this);
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new EntryEnumerator(this);
        }

        #endregion

        private class EntryEnumerator : IEnumerator<ObjectTableEntry>, System.Collections.IEnumerator
        {
            private ObjectTableWrapper m_owner;
            private uint m_index;
            public EntryEnumerator(ObjectTableWrapper owner)
            {
                m_owner = owner;
            }

            #region IEnumerator<ObjectTableEntry> Members

            public ObjectTableEntry Current
            {
                get { return m_owner[m_index]; }
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
            }

            #endregion

            #region IEnumerator Members

            object System.Collections.IEnumerator.Current
            {
                get { return m_owner[m_index]; }
            }

            public bool MoveNext()
            {
                m_index++;
                return m_index < m_owner.Size;
            }

            public void Reset()
            {
                m_index = 0;
            }

            #endregion
        }
    }
}
