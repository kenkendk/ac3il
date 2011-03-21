using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPEJIT
{
    /// <summary>
    /// Interface for registering custom type serializers
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Serializes an object to a byte format suitable for the SPE
        /// </summary>
        /// <param name="data">The element to serialize</param>
        /// <returns>The serialized data</returns>
        byte[] Serialize(object element, out uint size);

        /// <summary>
        /// Deserializes an object from SPE byte format
        /// </summary>
        /// <param name="conv">The conversion helper to write data into</param>
        /// <returns>The deserialized object</returns>
        object Deserialize(SPEEmulator.IEndianBitConverter conv, ObjectTableEntry e, object storage);
    }

    /// <summary>
    /// This class is responsible for managing objects that exist on the SPE
    /// It does caching to transparently support aliasing of pointers.
    /// </summary>
    public class SPEObjectManager
    {
        public ObjectTableWrapper ObjectTable;
        public SPEEmulator.IEndianBitConverter Converter;
        public Dictionary<object, uint> KnownObjectsByObj;
        public Dictionary<uint, object> KnownObjectsById;
        public Dictionary<Type, ISerializer> Serializers;

        /// <summary>
        /// Simple class that ensures that items in the dictionary are compared by pointer ref, rather than by object.Equals()
        /// </summary>
        private class PointerComparer : IEqualityComparer<object>
        {
            public bool Equals(object x, object y) { return x == y; }
            public int GetHashCode(object obj) { return obj.GetHashCode(); }
        }

        public SPEObjectManager(SPEEmulator.IEndianBitConverter conv)
        {
            this.Converter = conv;
            this.ObjectTable = ReadObjectTable(conv);

            KnownObjectsById = new Dictionary<uint, object>();
            KnownObjectsByObj = new Dictionary<object, uint>(new PointerComparer());

            Serializers = GetDefaultSerializers();
        }

        public void SaveObjectTable()
        {
            WriteObjectTable(this.Converter, this.ObjectTable);
        }

        public object ReadObjectFromLS(uint objectIndex)
        {
            return ReadObjectFromLS(objectIndex, null);
        }

        public object ReadObjectFromLS(uint objectIndex, object storage)
        {
            if (objectIndex == 0)
                return null;

            if (!KnownObjectsById.ContainsKey(objectIndex))
            {
                ObjectTableEntry e = ObjectTable[objectIndex];
                Type eltype = FindElementType(objectIndex);
                ISerializer s;
                Serializers.TryGetValue(eltype, out s);
                if (s == null && eltype.IsArray)
                    Serializers.TryGetValue(typeof(Array), out s);
                if (s == null)
                    throw new Exception("Unable to deserialize element of type: " + eltype.FullName);

                SPEEmulator.EndianBitConverter c = new SPEEmulator.EndianBitConverter(new byte[e.AlignedSize]);
                Converter.ReadBytes(e.Offset, c.Data);

                object el = s.Deserialize(c, e, storage);

                KnownObjectsById[objectIndex] = el;
                KnownObjectsByObj[el] = objectIndex;
            }
                
            return KnownObjectsById[objectIndex];
        }

        public object ReadRegisterPrimitiveValue(Type t, uint offset)
        {
            if (!t.IsPrimitive)
                throw new ArgumentException("t");

            Converter.Position = offset;

            AccCIL.KnownObjectTypes objt = AccCIL.AccCIL.GetObjType(t);
            switch (objt)
            {
                case AccCIL.KnownObjectTypes.Boolean:
                    return ((uint)Serializers[typeof(uint)].Deserialize(Converter, null, null)) == 0 ? false : true;
                case AccCIL.KnownObjectTypes.SByte:
                    return (sbyte)(int)Serializers[typeof(int)].Deserialize(Converter, null, null);
                case AccCIL.KnownObjectTypes.Short:
                    return (short)(int)Serializers[typeof(int)].Deserialize(Converter, null, null);
                case AccCIL.KnownObjectTypes.Byte:
                    return (byte)(uint)Serializers[typeof(uint)].Deserialize(Converter, null, null);
                case AccCIL.KnownObjectTypes.UShort:
                    return (ushort)(uint)Serializers[typeof(uint)].Deserialize(Converter, null, null);
                default:
                    return Serializers[t].Deserialize(Converter, null, null);
            }
        }

        private uint WritePrimitiveValue(object data, uint offset)
        {
            Type t = data.GetType();
            if (!t.IsPrimitive)
                throw new ArgumentException("data");
            
            uint s;
            byte[] tmp = Serializers[t].Serialize(data, out s);
            Converter.WriteBytes(offset, tmp);
            return (uint)tmp.Length;
        }

        public void WriteRegisterPrimitiveValue(object data, uint offset)
        {
            uint size = 0;
            while (size < 16)
                size += WritePrimitiveValue(data, offset + size);
        }

        public void DeleteObject(uint objectindex)
        {
            this.ObjectTable.RemoveObject(objectindex);
        }

        public uint CreateObjectOnLS(object data)
        {
            if (data == null)
                return 0;

            if (!KnownObjectsByObj.ContainsKey(data))
            {
                Type eltype = data.GetType();
                ISerializer s;
                Serializers.TryGetValue(eltype, out s);
                if (s == null && eltype.IsArray)
                    Serializers.TryGetValue(typeof(Array), out s);
                if (s == null)
                    throw new Exception("Unable to serialize element of type: " + eltype.FullName);

                uint size;
                byte[] buffer = s.Serialize(data, out size);

                uint objindex;

                if (eltype.IsArray && eltype.GetElementType().IsPrimitive)
                    objindex = ObjectTable.AddObject(AccCIL.AccCIL.GetObjType(eltype.GetElementType()), size, null);
                else if (eltype == typeof(string))
                    objindex = ObjectTable.AddObject(AccCIL.KnownObjectTypes.String, size, null);
                else
                    objindex = ObjectTable.AddObject(AccCIL.KnownObjectTypes.Object, size, eltype.FullName);

                ObjectTableEntry e = ObjectTable[objindex];
                if (e.AlignedSize != buffer.Length)
                {
                    byte[] tmp = new byte[e.AlignedSize];
                    Array.Copy(buffer, tmp, buffer.Length);
                    buffer = tmp;
                }

                Converter.WriteBytes(e.Offset, buffer);

                KnownObjectsById[objindex] = data;
                KnownObjectsByObj[data] = objindex;
            }

            return KnownObjectsByObj[data];
        }


        internal void WriteObjectToLS(uint objectindex, object data)
        {
            Type eltype = data.GetType();
            ISerializer s;
            Serializers.TryGetValue(eltype, out s);
            if (s == null && eltype.IsArray)
                Serializers.TryGetValue(typeof(Array), out s);
            if (s == null)
                throw new Exception("Unable to serialize element of type: " + eltype.FullName);

            uint size;
            byte[] buffer = s.Serialize(data, out size);

            ObjectTableEntry e = ObjectTable[objectindex];

            if (size != e.Size)
                throw new Exception("Bad stuff happened");

            if (e.AlignedSize != buffer.Length)
            {
                byte[] tmp = new byte[e.AlignedSize];
                Array.Copy(buffer, tmp, buffer.Length);
                buffer = tmp;
            }

            Converter.WriteBytes(e.Offset, buffer);
        }

        private Type FindElementType(uint objectIndex)
        {
            ObjectTableEntry e = ObjectTable[objectIndex];
            if (e.KnownType == AccCIL.KnownObjectTypes.String)
                return typeof(string);
            if (e.Type != 0)
                return Type.GetType((string)ReadObjectFromLS(e.Type));
            else
                return AccCIL.AccCIL.GetObjType(e.KnownType).MakeArrayType();
        }

        private static ObjectTableWrapper ReadObjectTable(SPEEmulator.IEndianBitConverter conv)
        {
            uint object_table_size = conv.ReadUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET);
            SPEEmulator.EndianBitConverter obj_tb_tmp = new SPEEmulator.EndianBitConverter(new byte[(object_table_size + 1) * 16]);
            conv.ReadBytes(SPEJITCompiler.OBJECT_TABLE_OFFSET, obj_tb_tmp.Data);
            uint[] object_table = new uint[(object_table_size + 1) * 4];
            for (int i = 0; i < object_table.Length; i++)
                object_table[i] = obj_tb_tmp.ReadUInt();

            ObjectTableWrapper objtable = new ObjectTableWrapper(object_table);

            foreach (var v in objtable.Where(c => c.KnownType == AccCIL.KnownObjectTypes.String))
            {
                byte[] localdata = new byte[v.AlignedSize];
                conv.ReadBytes(v.Offset, localdata);

                objtable.Strings.Add(System.Text.Encoding.UTF8.GetString(localdata, 0, (int)v.Size), v.Index);
            }

            return objtable;
        }

        private static void WriteObjectTable(SPEEmulator.IEndianBitConverter conv, ObjectTableWrapper objtable)
        {
            SPEEmulator.EndianBitConverter obj_tb_tmp = new SPEEmulator.EndianBitConverter(new byte[objtable.Data.Length * 4]);
            foreach (uint u in objtable.Data)
                obj_tb_tmp.WriteUInt(u);

            conv.WriteBytes(SPEJITCompiler.OBJECT_TABLE_OFFSET, obj_tb_tmp.Data);

            foreach (KeyValuePair<string, uint> k in objtable.Strings)
            {
                ObjectTableEntry e = objtable[k.Value];

                System.Diagnostics.Debug.Assert(e.KnownType == AccCIL.KnownObjectTypes.String);
                System.Diagnostics.Debug.Assert(e.Size == k.Key.Length);

                byte[] localdata = new byte[e.AlignedSize];
                System.Text.Encoding.UTF8.GetBytes(k.Key, 0, k.Key.Length, localdata, 0);

                conv.WriteBytes(e.Offset, localdata);
            }
        }

        private Dictionary<Type, ISerializer> GetDefaultSerializers()
        {
            Dictionary<Type, ISerializer> tmp = new Dictionary<Type, ISerializer>();
            tmp[typeof(byte)] = new ByteSerializer();
            tmp[typeof(sbyte)] = new SByteSerializer();
            tmp[typeof(bool)] = new BooleanSerializer();
            tmp[typeof(short)] = new ShortSerializer();
            tmp[typeof(ushort)] = new UShortSerializer();
            tmp[typeof(int)] = new IntSerializer();
            tmp[typeof(uint)] = new UIntSerializer();
            tmp[typeof(long)] = new LongSerializer();
            tmp[typeof(ulong)] = new ULongSerializer();
            tmp[typeof(float)] = new FloatSerializer();
            tmp[typeof(double)] = new DoubleSerializer();
            tmp[typeof(string)] = new StringSerializer(this);
            tmp[typeof(Array)] = new ArraySerializer(this);

            return tmp;
        }


        private class ByteSerializer : ISerializer
        {
            private SPEEmulator.EndianBitConverter m_conv = new SPEEmulator.EndianBitConverter(new byte[1]);
            public byte[] Serialize(object element, out uint size) { m_conv.WriteByte(0, (byte)element); size = 1; return m_conv.Data; }
            public object Deserialize(SPEEmulator.IEndianBitConverter conv, ObjectTableEntry e, object storage) { return conv.ReadByte(); }
        }

        private class SByteSerializer : ISerializer
        {
            private SPEEmulator.EndianBitConverter m_conv = new SPEEmulator.EndianBitConverter(new byte[1]);
            public byte[] Serialize(object element, out uint size) { m_conv.WriteByte(0, (byte)(sbyte)element); size = 1; return m_conv.Data; }
            public object Deserialize(SPEEmulator.IEndianBitConverter conv, ObjectTableEntry e, object storage) { return (sbyte)conv.ReadByte(); }
        }

        private class BooleanSerializer : ISerializer
        {
            private SPEEmulator.EndianBitConverter m_conv = new SPEEmulator.EndianBitConverter(new byte[1]);
            public byte[] Serialize(object element, out uint size) { m_conv.WriteByte(0, (byte)((bool)element ? 1 : 0)); size = 1; return m_conv.Data; }
            public object Deserialize(SPEEmulator.IEndianBitConverter conv, ObjectTableEntry e, object storage) { return conv.ReadByte() == 0 ? false : true; }
        }

        private class ShortSerializer : ISerializer
        {
            private SPEEmulator.EndianBitConverter m_conv = new SPEEmulator.EndianBitConverter(new byte[2]);
            public byte[] Serialize(object element, out uint size) { m_conv.WriteUShort(0, (ushort)(short)element); size = 2; return m_conv.Data; }
            public object Deserialize(SPEEmulator.IEndianBitConverter conv, ObjectTableEntry e, object storage) { return (short)conv.ReadUShort(); }
        }

        private class UShortSerializer : ISerializer
        {
            private SPEEmulator.EndianBitConverter m_conv = new SPEEmulator.EndianBitConverter(new byte[2]);
            public byte[] Serialize(object element, out uint size) { m_conv.WriteUShort(0, (ushort)element); size = 2; return m_conv.Data; }
            public object Deserialize(SPEEmulator.IEndianBitConverter conv, ObjectTableEntry e, object storage) { return conv.ReadUShort(); }
        }

        private class IntSerializer : ISerializer
        {
            private SPEEmulator.EndianBitConverter m_conv = new SPEEmulator.EndianBitConverter(new byte[4]);
            public byte[] Serialize(object element, out uint size) { m_conv.WriteUInt(0, (uint)(int)element); size = 4; return m_conv.Data; }
            public object Deserialize(SPEEmulator.IEndianBitConverter conv, ObjectTableEntry e, object storage) { return (int)conv.ReadUInt(); }
        }

        private class UIntSerializer : ISerializer
        {
            private SPEEmulator.EndianBitConverter m_conv = new SPEEmulator.EndianBitConverter(new byte[4]);
            public byte[] Serialize(object element, out uint size) { m_conv.WriteUInt(0, (uint)element); size = 4; return m_conv.Data; }
            public object Deserialize(SPEEmulator.IEndianBitConverter conv, ObjectTableEntry e, object storage) { return conv.ReadUInt(); }
        }

        private class LongSerializer : ISerializer
        {
            private SPEEmulator.EndianBitConverter m_conv = new SPEEmulator.EndianBitConverter(new byte[8]);
            public byte[] Serialize(object element, out uint size) { m_conv.WriteULong(0, (ulong)(long)element); size = 8; return m_conv.Data; }
            public object Deserialize(SPEEmulator.IEndianBitConverter conv, ObjectTableEntry e, object storage) { return (long)conv.ReadULong(); }
        }

        private class ULongSerializer : ISerializer
        {
            private SPEEmulator.EndianBitConverter m_conv = new SPEEmulator.EndianBitConverter(new byte[8]);
            public byte[] Serialize(object element, out uint size) { m_conv.WriteULong(0, (ulong)element); size = 8; return m_conv.Data; }
            public object Deserialize(SPEEmulator.IEndianBitConverter conv, ObjectTableEntry e, object storage) { return conv.ReadULong(); }
        }

        private class FloatSerializer : ISerializer
        {
            private SPEEmulator.EndianBitConverter m_conv = new SPEEmulator.EndianBitConverter(new byte[4]);
            public byte[] Serialize(object element, out uint size) { m_conv.WriteFloat(0, (float)element); size = 4; return m_conv.Data; }
            public object Deserialize(SPEEmulator.IEndianBitConverter conv, ObjectTableEntry e, object storage) { return conv.ReadFloat(); }
        }

        private class DoubleSerializer : ISerializer
        {
            private SPEEmulator.EndianBitConverter m_conv = new SPEEmulator.EndianBitConverter(new byte[8]);
            public byte[] Serialize(object element, out uint size) { m_conv.WriteDouble(0, (double)element); size = 8; return m_conv.Data; }
            public object Deserialize(SPEEmulator.IEndianBitConverter conv, ObjectTableEntry e, object storage) { return conv.ReadDouble(); }
        }

        private class StringSerializer : ISerializer
        {
            private SPEObjectManager m_parent;
            public StringSerializer(SPEObjectManager parent) { m_parent = parent; }

            public byte[] Serialize(object element, out uint size) 
            {
                string s = (string)element;
                size = (uint)System.Text.Encoding.UTF8.GetByteCount(s);
                uint alignedSize = ((size + 15) >> 4) << 4;

                byte[] data = new byte[alignedSize];
                System.Text.Encoding.UTF8.GetBytes(s, 0, s.Length, data, 0);
                return data; 
            }
            public object Deserialize(SPEEmulator.IEndianBitConverter conv, ObjectTableEntry e, object storage) 
            {
                byte[] data = new byte[e.AlignedSize];
                conv.ReadBytes(data); 
                return System.Text.Encoding.UTF8.GetString(data, 0, (int)e.Size); 
            }
        }

        private class ArraySerializer : ISerializer
        {
            private SPEObjectManager m_parent;
            private SPEEmulator.IEndianBitConverter m_conv = new SPEEmulator.EndianBitConverter(new byte[4]);
            public ArraySerializer(SPEObjectManager parent) { m_parent = parent; }

            public byte[] Serialize(object element, out uint size) 
            {
                if (element == null)
                    throw new ArgumentNullException("element");

                Array arr = (Array)element;
                Type eltype = element.GetType().GetElementType();
                
                AccCIL.KnownObjectTypes objt;
                uint elsize;
                string typename;

                if (eltype.IsPrimitive)
                {
                    objt = AccCIL.AccCIL.GetObjType(eltype);
                    elsize = 1u << (int)BuiltInSPEMethods.get_array_elem_len_mult((uint)objt);
                    typename = null;
                }
                else
                {
                    elsize = 4;
                    objt = AccCIL.KnownObjectTypes.Object;
                    typename = element.GetType().FullName;
                }

                size = (uint)arr.Length * elsize;
                uint alignedSize = ((size + 15) >> 4) << 4;

                SPEEmulator.EndianBitConverter c = new SPEEmulator.EndianBitConverter(new byte[alignedSize]);

                if (eltype.IsPrimitive)
                {
                    ISerializer elserializer;
                    m_parent.Serializers.TryGetValue(eltype, out elserializer);
                    if (elserializer == null)
                        throw new Exception("Unsupported inner type: " + eltype.FullName);

                    for (int i = 0; i < arr.Length; i++)
                    {
                        //TODO: This is inefficient, it should write directly into the target buffer
                        uint s;
                        byte[] localdata = elserializer.Serialize(arr.GetValue(i), out s);
                        Array.Copy(localdata, 0, c.Data, c.Position, localdata.Length);
                        c.Position += s;
                    }

                }
                else
                {
                    for (int i = 0; i < arr.Length; i++)
                    {
                        object value = arr.GetValue(i);
                        if (value == null)
                            c.WriteUInt(0);
                        else
                        {
                            //If we are writing back the array, write back the element as well
                            if (m_parent.KnownObjectsByObj.ContainsKey(element) && m_parent.KnownObjectsByObj.ContainsKey(value))
                                m_parent.WriteObjectToLS(m_parent.KnownObjectsByObj[value], value);
                            else
                                c.WriteUInt(m_parent.CreateObjectOnLS(value));
                        }
                    }
                }

                return c.Data; 
            }

            public object Deserialize(SPEEmulator.IEndianBitConverter conv, ObjectTableEntry e, object storage) 
            {
                Type arrtype;
                uint elsize;
                if (e.KnownType == AccCIL.KnownObjectTypes.String)
                    throw new InvalidProgramException("Something is wrong here");

                if (e.Type == 0)
                {
                    arrtype = AccCIL.AccCIL.GetObjType(e.KnownType).MakeArrayType();
                    elsize = 1u << (int)BuiltInSPEMethods.get_array_elem_len_mult((uint)e.KnownType);
                }
                else
                {
                    arrtype = Type.GetType((string)m_parent.ReadObjectFromLS(e.Type));
                    elsize = 4;
                }

                Type eltype = arrtype.GetElementType();
                uint arraylen = e.Size / elsize;
                
                Array arr;

                if (storage == null)
                    arr = Array.CreateInstance(eltype, arraylen);
                else
                {
                    arr = (Array)storage;
                    if (storage.GetType().GetElementType() != eltype || arr.Length != arraylen)
                        throw new Exception("Unexpected difference in storage object and actual object");
                }

                if (eltype.IsPrimitive)
                {
                    ISerializer elserializer;
                    m_parent.Serializers.TryGetValue(eltype, out elserializer);
                    if (elserializer == null)
                        throw new Exception("Unsupported inner type: " + eltype.FullName);

                    for (int i = 0; i < arr.Length; i++)
                        arr.SetValue(elserializer.Deserialize(conv, null, arr.GetValue(i)), i);
                }
                else
                {
                    //In this case elements may have a different type than what the array states,
                    //because the array elements can be interface or object type, and the actual 
                    //instance type is unknown
                    ISerializer uintd = m_parent.Serializers[typeof(uint)];
                    for (int i = 0; i < arr.Length; i++)
                        arr.SetValue(m_parent.ReadObjectFromLS((uint)uintd.Deserialize(conv, null, null) , arr.GetValue(i)), i);

                }


                return arr;
            }
        }
    }
}
