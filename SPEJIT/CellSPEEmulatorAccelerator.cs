using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace SPEJIT
{
    /// <summary>
    /// Implementation of an SPE manager for the SPE emulator
    /// </summary>
    public class CellSPEEmulatorAccelerator : AccCIL.AccelleratorBase
    {
        private SPEJIT.SPEJITCompiler m_compiler = new SPEJITCompiler();
        private string m_elf = null;
        private Dictionary<int, Mono.Cecil.MethodReference> m_callpoints = null;

        private System.Threading.ManualResetEvent m_workingEvent;
        private uint m_exitCode;

        public bool ShowGUI { get; set; }

        public override void LoadProgram(IEnumerable<AccCIL.ICompiledMethod> methods)
        {
            string startPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string program = System.IO.Path.Combine(startPath, methods.First().Method.Method.DeclaringType.Module.Assembly.Name.Name);

            // Create ELF
            string elffile = System.IO.Path.Combine(startPath, program + ".elf");
#if DEBUG
            using (System.IO.FileStream outfile = new System.IO.FileStream(elffile, System.IO.FileMode.Create))
            using (System.IO.TextWriter sw = new System.IO.StreamWriter(System.IO.Path.Combine(startPath, program + ".asm")))
            {
                m_callpoints = m_compiler.EmitELFStream(outfile, sw, methods);
                Console.WriteLine("Converted output size in bytes: " + outfile.Length);
            }
#else
            using (System.IO.FileStream outfile = new System.IO.FileStream(elffile, System.IO.FileMode.Create))
                m_callpoints = m_compiler.EmitELFStream(outfile, null, methods); 
#endif
            m_elf = elffile;
        }

        protected override AccCIL.IJITCompiler Compiler
        {
            get { return m_compiler; }
        }

        private static long CalculateArraySize(Type t, object el)
        {
            if (!t.IsArray)
                throw new Exception("Type is not array: " + t.FullName);

            Type elType = t.GetElementType();
            AccCIL.KnownObjectTypes objtype = AccCIL.AccCIL.GetObjType(elType);
            uint elementsize = 1u << (int)BuiltInSPEMethods.get_array_elem_len_mult((uint)objtype);

            Array arr = (Array)el;
            return arr.Length * elementsize;
        }

        private static void TransferObjectToLS(Type t, uint offset, object el, SPEEmulator.EndianBitConverter conv)
        {
            byte[] localbuffer = SerializeObject(t, el);

            //Copy data over
            Array.Copy(localbuffer, 0, conv.Data, offset, localbuffer.Length);
        }

        private static object DeserializeObject(Type t, uint size, byte[] localdata, object storage)
        {
            if (t == typeof(string))
            {
                storage = System.Text.Encoding.UTF8.GetString(localdata, 0, (int)size);
            }
            else if (t.IsArray)
            {
                Array data = (Array)storage;
                AccCIL.KnownObjectTypes objt = AccCIL.AccCIL.GetObjType(t.GetElementType());
                SPEEmulator.EndianBitConverter c = new SPEEmulator.EndianBitConverter(localdata);

                for (int i = 0; i < data.Length; i++)
                {
                    switch (objt)
                    {
                        case AccCIL.KnownObjectTypes.Boolean:
                            data.SetValue(c.ReadByte() == 1 ? true : false, i);
                            break;
                        case AccCIL.KnownObjectTypes.SByte:
                            data.SetValue((sbyte)c.ReadByte(), i);
                            break;
                        case AccCIL.KnownObjectTypes.Byte:
                            data.SetValue(c.ReadByte(), i);
                            break;
                        case AccCIL.KnownObjectTypes.UShort:
                            data.SetValue(c.ReadUShort(), i);
                            break;
                        case AccCIL.KnownObjectTypes.Short:
                            data.SetValue((short)c.ReadUShort(), i);
                            break;
                        case AccCIL.KnownObjectTypes.UInt:
                            data.SetValue(c.ReadUInt(), i);
                            break;
                        case AccCIL.KnownObjectTypes.Int:
                            data.SetValue((int)c.ReadUInt(), i);
                            break;
                        case AccCIL.KnownObjectTypes.ULong:
                            data.SetValue(c.ReadULong(), i);
                            break;
                        case AccCIL.KnownObjectTypes.Long:
                            data.SetValue((long)c.ReadULong(), i);
                            break;
                        case AccCIL.KnownObjectTypes.Float:
                            data.SetValue(c.ReadFloat(), i);
                            break;
                        case AccCIL.KnownObjectTypes.Double:
                            data.SetValue(c.ReadDouble(), i);
                            break;
                        default:
                            throw new Exception("Unexpected data type: " + objt.ToString());
                    }
                }
            }
            else
            {
                throw new Exception("Unexpected object type: " + t.FullName);
            }

            return storage;
        }

        private static byte[] SerializeObject(Type t, object o)
        {
            if (o == null)
                throw new ArgumentNullException("o");

            if (t.IsPrimitive)
                throw new Exception("Cannot serialize primitives");
            
            if (t.IsArray)
            {
                AccCIL.KnownObjectTypes objtype = AccCIL.AccCIL.GetObjType(t.GetElementType());

                long size = CalculateArraySize(t, o);
                size += ((16 - size % 16) % 16);

                //Build contents in local memory
                SPEEmulator.EndianBitConverter ebc = new SPEEmulator.EndianBitConverter(new byte[size]);
                Array arr = (Array)o;

                for (int j = 0; j < arr.Length; j++)
                {
                    switch (objtype)
                    {
                        case AccCIL.KnownObjectTypes.Boolean:
                            ebc.WriteByte((byte)((bool)arr.GetValue(j) ? 1 : 0));
                            break;
                        case AccCIL.KnownObjectTypes.SByte:
                            ebc.WriteByte((byte)((sbyte)arr.GetValue(j)));
                            break;
                        case AccCIL.KnownObjectTypes.Byte:
                            ebc.WriteByte((byte)((byte)arr.GetValue(j)));
                            break;
                        case AccCIL.KnownObjectTypes.UShort:
                            ebc.WriteUShort((ushort)((ushort)arr.GetValue(j)));
                            break;
                        case AccCIL.KnownObjectTypes.Short:
                            ebc.WriteUShort((ushort)((short)arr.GetValue(j)));
                            break;
                        case AccCIL.KnownObjectTypes.UInt:
                            ebc.WriteUInt((uint)((uint)arr.GetValue(j)));
                            break;
                        case AccCIL.KnownObjectTypes.Int:
                            ebc.WriteUInt((uint)((int)arr.GetValue(j)));
                            break;
                        case AccCIL.KnownObjectTypes.ULong:
                            ebc.WriteULong((ulong)((ulong)arr.GetValue(j)));
                            break;
                        case AccCIL.KnownObjectTypes.Long:
                            ebc.WriteULong((ulong)((long)arr.GetValue(j)));
                            break;
                        case AccCIL.KnownObjectTypes.Float:
                            ebc.WriteFloat((float)arr.GetValue(j));
                            break;
                        case AccCIL.KnownObjectTypes.Double:
                            ebc.WriteDouble((double)arr.GetValue(j));
                            break;
                        default:
                            throw new InvalidProgramException();
                    }
                }

                return ebc.Data;
            }
            else if (t == typeof(string))
            {
                string s = (string)o;
                int size = System.Text.Encoding.UTF8.GetByteCount(s);
                size += ((16 - size % 16) % 16);

                byte[] localbuffer = new byte[size];

                System.Text.Encoding.UTF8.GetBytes(s, 0, s.Length, localbuffer, 0);
                
                return localbuffer;
            }
            else if (o != null && o.GetType().IsPrimitive)
            {
                //Boxed element, just copy it
                AccCIL.KnownObjectTypes objtype = AccCIL.AccCIL.GetObjType(o.GetType());
                SPEEmulator.EndianBitConverter ebc = new SPEEmulator.EndianBitConverter(new byte[1u << (int)BuiltInSPEMethods.get_array_elem_len_mult((uint)objtype)]);

                switch (objtype)
                {
                    case AccCIL.KnownObjectTypes.Boolean:
                        ebc.WriteByte((byte)((bool)o ? 1 : 0));
                        break;
                    case AccCIL.KnownObjectTypes.SByte:
                        ebc.WriteByte((byte)((sbyte)o));
                        break;
                    case AccCIL.KnownObjectTypes.Byte:
                        ebc.WriteByte((byte)((byte)o));
                        break;
                    case AccCIL.KnownObjectTypes.UShort:
                        ebc.WriteUShort((ushort)((ushort)o));
                        break;
                    case AccCIL.KnownObjectTypes.Short:
                        ebc.WriteUShort((ushort)((short)o));
                        break;
                    case AccCIL.KnownObjectTypes.UInt:
                        ebc.WriteUInt((uint)((uint)o));
                        break;
                    case AccCIL.KnownObjectTypes.Int:
                        ebc.WriteUInt((uint)((int)o));
                        break;
                    case AccCIL.KnownObjectTypes.ULong:
                        ebc.WriteULong((ulong)((ulong)o));
                        break;
                    case AccCIL.KnownObjectTypes.Long:
                        ebc.WriteULong((ulong)((long)o));
                        break;
                    case AccCIL.KnownObjectTypes.Float:
                        ebc.WriteFloat((float)o);
                        break;
                    case AccCIL.KnownObjectTypes.Double:
                        ebc.WriteDouble((double)o);
                        break;
                    default:
                        throw new InvalidProgramException();
                }

                return ebc.Data;
            }
            else
            {
                throw new Exception("Serialization not supported for type: " + t.FullName);
            }
        }

        private static object TransferObjectFromLS(ObjectTableWrapper objtable, uint objindex, SPEEmulator.EndianBitConverter conv, object storage)
        {
            //Get offset and verify index etc
            uint offset = objtable[objindex].Offset;
            uint size = objtable[objindex].Size;
            AccCIL.KnownObjectTypes objt = objtable[objindex].KnownType;
            uint typestring_index = objtable[objindex].Type;
            Type t;

            if (typestring_index == 0)
            {
                switch (objt)
                {
                    case AccCIL.KnownObjectTypes.Boolean:
                    case AccCIL.KnownObjectTypes.Byte:
                    case AccCIL.KnownObjectTypes.SByte:
                    case AccCIL.KnownObjectTypes.Short:
                    case AccCIL.KnownObjectTypes.UShort:
                    case AccCIL.KnownObjectTypes.Int:
                    case AccCIL.KnownObjectTypes.UInt:
                    case AccCIL.KnownObjectTypes.Long:
                    case AccCIL.KnownObjectTypes.ULong:
                    case AccCIL.KnownObjectTypes.Float:
                    case AccCIL.KnownObjectTypes.Double:
                        t = AccCIL.AccCIL.GetObjType(objt).MakeArrayType();
                        break;
                    case AccCIL.KnownObjectTypes.String:
                        t = typeof(string);
                        break;
                    default:
                        throw new Exception("Unexpected object type: " + objt.ToString());
                }
            }
            else
                t = Type.GetType((string)TransferObjectFromLS(objtable, typestring_index, conv, null));

            if (t.IsPrimitive)
                return ReadValue(t, conv, offset);

            byte[] localdata = new byte[size + (((16 - size % 16) % 16))];

            if (storage == null)
            {
                if (t.IsArray)
                    storage = Array.CreateInstance(t.GetElementType(), size / BuiltInSPEMethods.get_array_elem_len_mult((uint)objt));
                else if (t == typeof(string))
                    storage = null;
                else
                    throw new Exception("Unable to create type: " + t.FullName);
            }

            Array.Copy(conv.Data, offset, localdata, 0, localdata.Length);
            SPEEmulator.EndianBitConverter c = new SPEEmulator.EndianBitConverter(localdata);

            return DeserializeObject(t, size, localdata, storage);
        }

        private static object ReadValue(Type rtype, SPEEmulator.EndianBitConverter conv, uint offset)
        {

            if (rtype == typeof(uint))
                return Convert.ChangeType(conv.ReadUInt(offset), typeof(uint));
            else if (rtype == typeof(int))
                return Convert.ChangeType((int)conv.ReadUInt(offset), typeof(int));
            else if (rtype == typeof(short))
                return Convert.ChangeType((short)(conv.ReadUInt(offset) & 0xffff), typeof(short));
            else if (rtype == typeof(ushort))
                return Convert.ChangeType((ushort)(conv.ReadUInt(offset) & 0xffff), typeof(ushort));
            else if (rtype == typeof(byte))
                return Convert.ChangeType((byte)(conv.ReadUInt(offset) & 0xff), typeof(byte));
            else if (rtype == typeof(sbyte))
                return Convert.ChangeType((sbyte)(conv.ReadUInt(offset) & 0xff), typeof(sbyte));
            else if (rtype == typeof(ulong))
                return Convert.ChangeType(conv.ReadULong(offset), typeof(ulong));
            else if (rtype == typeof(long))
                return Convert.ChangeType((long)conv.ReadULong(offset), typeof(long));
            else if (rtype == typeof(float))
                return Convert.ChangeType(conv.ReadFloat(offset), typeof(float));
            else if (rtype == typeof(double))
                return Convert.ChangeType(conv.ReadDouble(offset), typeof(double));
            else if (!rtype.IsPrimitive)
                return conv.ReadUInt(offset);
            else
                throw new Exception("Return type not supported: " + rtype.FullName);
        }

        private static void WriteValue(Type t, SPEEmulator.EndianBitConverter conv, uint offset, object value)
        {
            if (t == typeof(int) || t == typeof(uint) || t == typeof(short) || t == typeof(ushort) || t == typeof(byte) || t == typeof(sbyte))
            {
                conv.WriteUInt(offset, (uint)Convert.ToInt32(value));
                conv.WriteUInt(offset + 4, (uint)Convert.ToInt32(value));
                conv.WriteUInt(offset + 8, (uint)Convert.ToInt32(value));
                conv.WriteUInt(offset + 12, (uint)Convert.ToInt32(value));
            }
            else if (t == typeof(long) || t == typeof(ulong))
            {
                conv.WriteULong(offset, (ulong)Convert.ToInt64(value));
                conv.WriteULong(offset + 8, (ulong)Convert.ToInt64(value));
            }
            else if (t == typeof(float))
            {
                conv.WriteFloat(offset, (float)value);
                conv.WriteFloat(offset + 4, (float)value);
                conv.WriteFloat(offset + 8, (float)value);
                conv.WriteFloat(offset + 12, (float)value);
            }
            else if (t == typeof(double))
            {
                conv.WriteDouble(offset, (double)value);
                conv.WriteDouble(offset + 8, (double)value);
            }
            else if (t == typeof(bool))
            {
                conv.WriteUInt(offset, (uint)(((bool)value) ? 1 : 0));
                conv.WriteUInt(offset + 4, (uint)(((bool)value) ? 1 : 0));
                conv.WriteUInt(offset + 8, (uint)(((bool)value) ? 1 : 0));
                conv.WriteUInt(offset + 12, (uint)(((bool)value) ? 1 : 0));
            }
            else if (!t.IsPrimitive)
            {
                conv.WriteUInt(offset, (uint)value);
                conv.WriteUInt(offset + 4, (uint)value);
                conv.WriteUInt(offset + 8, (uint)value);
                conv.WriteUInt(offset + 12, (uint)value);
            }
            else
                throw new Exception("Unsupported argument type: " + t.FullName);
        }

        private static uint CreateObjectOnLS(ObjectTableWrapper objtable, Type t, object element)
        {
            if (element == null)
                return 0;
            else if (t.IsPrimitive)
                throw new Exception("Unexpected primitive?");
            else if (element.GetType().IsPrimitive)
            {
                //Boxed object transfer
                return objtable.AddObject(AccCIL.KnownObjectTypes.Object, 1u << (int)BuiltInSPEMethods.get_array_elem_len_mult((uint)AccCIL.AccCIL.GetObjType(element.GetType())), element.GetType().FullName);
            }
            else if (t == typeof(string))
                return objtable.AddObject(AccCIL.KnownObjectTypes.String, (uint)System.Text.Encoding.UTF8.GetByteCount((string)element), null);
            else if (t.IsArray)
                return objtable.AddObject(AccCIL.AccCIL.GetObjType(t.GetElementType()), (uint)CalculateArraySize(t, element), null);
            else
                throw new Exception("Unexpected type: " + t.FullName);
        }


        public override T Execute<T>(params object[] args)
        {
            bool showGui = this.ShowGUI;

            SPEEmulator.SPEProcessor spe;
            SPEEmulatorTestApp.Simulator sx = null;

            if (showGui)
            {
                // Run program
                sx = new SPEEmulatorTestApp.Simulator(new string[] { m_elf });
                sx.StartAndPause();
                spe = sx.SPE;
            }
            else
            {
                spe = new SPEEmulator.SPEProcessor();
                spe.LoadELF(m_elf);
                m_workingEvent = new System.Threading.ManualResetEvent(false);
            }

            SPEEmulator.EndianBitConverter conv = new SPEEmulator.EndianBitConverter(spe.LS);

            conv.WriteUInt(0, 0);
            conv.WriteUInt(4, (uint)args.Length);

            uint lsoffset = (uint)spe.LS.Length - (16 * 8); 
            //The -(16 * 8) is used to prevent the bootloader stack setup from overwriting the arguments

            ObjectTableWrapper objtable = ReadObjectTable(conv);

            Type[] types = m_loadedMethodTypes;
            Dictionary<uint, int> addedObjects = new Dictionary<uint, int>();
            Dictionary<object, uint> objref = new Dictionary<object, uint>();
            Dictionary<uint, uint> nonSerialized = new Dictionary<uint, uint>();

            for (int i = args.Length - 1; i >= 0; i--)
            {
                lsoffset -= 16;

                if (!types[i].IsPrimitive)
                {
                    uint objindex;
                    if (args[i] == null)
                        objindex = 0;
                    else
                    {
                        if (objref.ContainsKey(args[i]))
                        {
                            objindex = objref[args[i]];
                            if (nonSerialized.ContainsKey(objindex))
                            {
                                nonSerialized.Remove(objindex);
                                TransferObjectToLS(types[i], objtable[objindex].Offset, args[i], conv);
                            }
                        }
                        else
                        {
                            objindex = CreateObjectOnLS(objtable, types[i], args[i]);
                            objtable[objindex].Refcount = 1;
                            if (m_typeSerializeIn[i])
                                TransferObjectToLS(types[i], objtable[objindex].Offset, args[i], conv);
                            else
                                nonSerialized.Add(objindex, 0);

                            addedObjects.Add(objindex, i);
                            objref[args[i]] = objindex;

                        }
                    }

                    WriteValue(typeof(uint), conv, lsoffset, objindex);
                }
                else
                {
                    WriteValue(types[i], conv, lsoffset, args[i]);
                }
            }

            conv.WriteUInt(8, lsoffset);
            conv.WriteUInt(12, 0);

            //Write back the object table
            WriteObjectTable(conv, objtable);

            spe.RegisterCallbackHandler(SPEJITCompiler.STOP_METHOD_CALL & 0xff, spe_MissingMethodCallback);

            if (showGui)
                System.Windows.Forms.Application.Run(sx);
            else
            {
                m_exitCode = 0;
                m_workingEvent.Reset();
                spe.SPEStopped += new SPEEmulator.StatusEventDelegate(spe_SPEStopped);
                spe.Exit += new SPEEmulator.ExitEventDelegate(spe_Exit);
                spe.Start();
                m_workingEvent.WaitOne();
                spe.SPEStopped -= new SPEEmulator.StatusEventDelegate(spe_SPEStopped);
                spe.Exit -= new SPEEmulator.ExitEventDelegate(spe_Exit);
                if (m_exitCode != SPEJITCompiler.STOP_SUCCESSFULL)
                    throw new Exception("Invalid exitcode: " + m_exitCode);

            }

            spe.UnregisterCallbackHandler(SPEJITCompiler.STOP_METHOD_CALL & 0xff);

            objtable = ReadObjectTable(conv);

            //Now extract data back into the objects that are byref
            foreach (KeyValuePair<uint, int> k in addedObjects)
            {
                if (m_typeSerializeOut[k.Value])
                    TransferObjectFromLS(objtable, k.Key, conv, args[k.Value]);

                //Remove the entry from the LS object table
                objtable.RemoveObject(k.Key);
            }


            //Write back the object table
            WriteObjectTable(conv, objtable);

            Type rtype = typeof(T);

            if (rtype == typeof(ReturnTypeVoid))
                return default(T);
            else if (!rtype.IsPrimitive)
            {
                uint objindex = (uint)ReadValue(rtype, conv, 0);
                if (objindex == 0)
                    return default(T);

                if (!m_typeSerializeOut[addedObjects[objindex]])
                    throw new InvalidOperationException("Cannot return an object that was marked as [In] only");
                return (T)args[addedObjects[objindex]];
            }
            else
                return (T)ReadValue(rtype, conv, 0);
        }

        private static ObjectTableWrapper ReadObjectTable(SPEEmulator.EndianBitConverter conv)
        {
            uint object_table_size = conv.ReadUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET);
            SPEEmulator.EndianBitConverter obj_tb_tmp = new SPEEmulator.EndianBitConverter(new byte[(object_table_size + 1) * 16]);
            Array.Copy(conv.Data, SPEJITCompiler.OBJECT_TABLE_OFFSET, obj_tb_tmp.Data, 0, obj_tb_tmp.Data.Length);
            uint[] object_table = new uint[(object_table_size + 1) * 4];
            for (int i = 0; i < object_table.Length; i++)
                object_table[i] = obj_tb_tmp.ReadUInt();
            
            ObjectTableWrapper objtable = new ObjectTableWrapper(object_table);

            foreach (var v in objtable.Where(c => c.KnownType == AccCIL.KnownObjectTypes.String))
            {
                byte[] localdata = new byte[v.AlignedSize];
                Array.Copy(conv.Data, v.Offset, localdata, 0, localdata.Length);

                objtable.Strings.Add(System.Text.Encoding.UTF8.GetString(localdata, 0, (int)v.Size), v.Index);
            }

            return objtable;
        }

        private static void WriteObjectTable(SPEEmulator.EndianBitConverter conv, ObjectTableWrapper objtable)
        {
            SPEEmulator.EndianBitConverter obj_tb_tmp = new SPEEmulator.EndianBitConverter(new byte[objtable.Data.Length * 4]);
            foreach (uint u in objtable.Data)
                obj_tb_tmp.WriteUInt(u);
            Array.Copy(obj_tb_tmp.Data, 0, conv.Data, SPEJITCompiler.OBJECT_TABLE_OFFSET, obj_tb_tmp.Data.Length);

            foreach (KeyValuePair<string, uint> k in objtable.Strings)
            {
                ObjectTableEntry e = objtable[k.Value];

                System.Diagnostics.Debug.Assert(e.KnownType == AccCIL.KnownObjectTypes.String);
                System.Diagnostics.Debug.Assert(e.Size == k.Key.Length);

                byte[] localdata = new byte[e.AlignedSize];
                System.Text.Encoding.UTF8.GetBytes(k.Key, 0, k.Key.Length, localdata, 0);
                
                Array.Copy(localdata, 0, conv.Data, e.Offset, localdata.Length);
            }
        }


        void spe_Exit(SPEEmulator.SPEProcessor sender, uint exitcode)
        {
            m_exitCode = exitcode;
        }

        void spe_SPEStopped(SPEEmulator.SPEProcessor sender)
        {
            m_workingEvent.Set();
        }

        bool spe_MissingMethodCallback(byte[] ls, uint offset)
        {
            SPEEmulator.EndianBitConverter c = new SPEEmulator.EndianBitConverter(ls);
            uint sp = c.ReadUInt(offset);

            //Stack size is 77 elements, and return address is next next instruction
            uint call_address = (c.ReadUInt(sp + (16 * 77)) - 4) / 4;

            Mono.Cecil.MethodReference calledmethod;
            m_callpoints.TryGetValue((int)call_address, out calledmethod);

            if (calledmethod == null)
                throw new Exception("No method call registerd at " + call_address);


            //All good, we have a real function, now load all required arguments onto PPE
            object[] arguments = new object[calledmethod.Parameters.Count];

            Dictionary<uint, object> transferred = new Dictionary<uint,object>();

            System.Reflection.MethodInfo m = AccCIL.AccCIL.FindReflectionMethod(calledmethod);
            if (m == null)
                throw new Exception("Unable to find function called: " + calledmethod.DeclaringType.FullName + "." + calledmethod.Name);

            uint arg_base = sp + 32;
            uint sp_offset = arg_base;
            object @this = null;

            ObjectTableWrapper objtable = ReadObjectTable(c);
            
            if (!m.IsStatic)
            {
                Type argtype = m.DeclaringType;
                uint objindex = (uint)ReadValue(argtype, c, sp_offset);
                @this = TransferObjectFromLS(objtable, objindex, c, null);

                transferred.Add(objindex, @this);

                sp_offset += 16;
            }

            for (int i = 0; i < arguments.Length; i++)
            {
                Type argtype = Type.GetType(calledmethod.Parameters[i].ParameterType.FullName);
                arguments[i] = ReadValue(argtype, c, sp_offset);
                if (!argtype.IsPrimitive)
                {
                    uint objindx = (uint)arguments[i];
                    if (objindx == 0)
                        arguments[i] = null;
                    else if (transferred.ContainsKey(objindx))
                        arguments[i] = transferred[objindx];
                    else
                    {
                        arguments[i] = TransferObjectFromLS(objtable, objindx, c, null);
                        transferred.Add(objindx, arguments[i]);
                    }
                }

                sp_offset += 16;
            }

            object result = m.Invoke(@this, arguments);
            int resultIndex = result == null ? 0 : -1;

            foreach (KeyValuePair<uint, object> t in transferred)
                if (t.Value != null)
                {
                    Type vt = objtable[t.Key].KnownType == AccCIL.KnownObjectTypes.Object ? typeof(object) : AccCIL.AccCIL.GetObjType(objtable[t.Key].KnownType);
                    //Strings are imutable, os there is no reason to transfer them back
                    if (vt == typeof(string))
                        continue;

                    uint lsoffset = objtable[t.Key].Offset;
                    TransferObjectToLS(vt, lsoffset, t.Value, c);

                    if (t.Value == result)
                        resultIndex = (int)t.Key;
                }

            if (m.ReturnType != null)
            {
                if (m.ReturnType.IsPrimitive)
                    WriteValue(m.ReturnType, c, arg_base, result);
                else
                {
                    if (resultIndex < 0)
                    {

                        resultIndex = (int)CreateObjectOnLS(objtable, m.ReturnType, result);
                        objtable[(uint)resultIndex].Refcount = 1;
                        TransferObjectToLS(m.ReturnType, objtable[(uint)resultIndex].Offset, result, c);
                        WriteObjectTable(c, objtable);
                    }

                    WriteValue(m.ReturnType, c, arg_base, (uint)resultIndex);
                }
            }
            
            return true;
        }

        /*private static string ReadString(SPEEmulator.EndianBitConverter conv, uint objindex, uint[] object_table)
        {
            uint size = object_table[objindex * 4 + 1];
            uint offset = object_table[objindex * 4 + 2];
            if ((AccCIL.KnownObjectTypes)(object_table[objindex * 4] & 0xff) != AccCIL.KnownObjectTypes.String)
                throw new Exception("Attempted to read string from object which was not string: " + ((AccCIL.KnownObjectTypes)(object_table[objindex * 4] & 0xff)).ToString());

            byte[] localdata = new byte[(size + 15 >> 4) << 4];
            Array.Copy(conv.Data, offset, localdata, 0, size);

            return System.Text.Encoding.UTF8.GetString(localdata, 0, (int)size);
        }*/
    }
}
