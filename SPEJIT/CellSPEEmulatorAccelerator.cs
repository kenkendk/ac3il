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
        private Dictionary<uint, object> m_objectRefs = null;

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

        private static uint GetObjectLSAddress(uint objindex, SPEEmulator.EndianBitConverter conv)
        {
            uint tablecount = conv.ReadUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET - 16);
            uint tablesize = conv.ReadUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET - 16 + 4);

            if (objindex <= 0 || objindex >= tablecount)
                throw new Exception("Object not found in SPE object table");

            return conv.ReadUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET + (objindex * 16) + 8);
        }

        private static uint CreateObjectOnLS(Type t, object el, bool serialize, SPEEmulator.EndianBitConverter conv)
        {
            if (!t.IsArray)
                throw new Exception("Unsupported object type: " + t.FullName);

            //Register a new object in the object table
            uint tablecount = conv.ReadUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET - 16);
            uint tablesize = conv.ReadUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET - 16 + 4);

            if (tablecount == tablesize)
                throw new Exception("Not enough space in SPE object table");

            uint nextoffset = 0;
            for (uint j = 0; j < tablecount; j++)
            {
                uint objsize = conv.ReadUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET + (j * 16) + 4);
                uint objoffset = conv.ReadUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET + (j * 16) + 8);
                nextoffset = Math.Max(nextoffset, objoffset + objsize + ((16 - objsize % 16) % 16));
            }

            AccCIL.KnownObjectTypes objtype = AccCIL.AccCIL.GetObjType(t.GetElementType());

            long arraysize = CalculateObjectSize(t, el);
            long requiredSpace = arraysize + ((16 - arraysize % 16) % 16);

            if (nextoffset + requiredSpace > conv.Data.Length)
                throw new Exception(string.Format("Unable to fit array of size {0} onto spe at offset {1}", requiredSpace, nextoffset));

            //Write the object table description
            conv.WriteUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET + (tablecount * 16), (uint)objtype);
            conv.WriteUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET + (tablecount * 16) + 4, (uint)arraysize);
            conv.WriteUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET + (tablecount * 16) + 8, nextoffset);
            conv.WriteUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET + (tablecount * 16) + 12, 0);

            //Update the table counter
            conv.WriteUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET - 16, tablecount + 1);

            if (serialize)
                TransferObjectToLS(t, nextoffset, el, conv);

            return tablecount;
        }

        private static long CalculateObjectSize(Type t, object el)
        {
            if (!t.IsArray)
                throw new Exception("Unsupported object type: " + t.FullName);

            Type elType = t.GetElementType();
            AccCIL.KnownObjectTypes objtype = AccCIL.AccCIL.GetObjType(elType);
            uint elementsize = 1u << (int)BuiltInMethods.get_array_elem_len_mult(objtype);

            Array arr = (Array)el;
            return arr.Length * elementsize;
        }

        private static void TransferObjectToLS(Type t, uint offset, object el, SPEEmulator.EndianBitConverter conv)
        {
            if (!t.IsArray)
                throw new Exception("Unsupported object type: " + t.FullName);

            AccCIL.KnownObjectTypes objtype = AccCIL.AccCIL.GetObjType(t.GetElementType());

            long size = CalculateObjectSize(t, el);
            size += ((16 - size % 16) % 16);

            byte[] localbuffer = new byte[size];

            //Build contents in local memory
            SPEEmulator.EndianBitConverter ebc = new SPEEmulator.EndianBitConverter(localbuffer);
            Array arr = (Array)el;

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

            //Copy data over
            Array.Copy(localbuffer, 0, conv.Data, offset, localbuffer.Length);
        }

        private static void TransferObjectFromLS(SPEEmulator.EndianBitConverter conv, uint objindex, Type t, object storage)
        {
            AccCIL.KnownObjectTypes objtype = (AccCIL.KnownObjectTypes)conv.ReadUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET + (objindex * 16));
            uint arraysize = conv.ReadUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET + (objindex * 16) + 4);
            uint offset = conv.ReadUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET + (objindex * 16) + 8);

            uint elementsize = 1u << (int)BuiltInMethods.get_array_elem_len_mult(objtype);
            Array data = (Array)storage;

            System.Diagnostics.Debug.Assert(data.Length == arraysize / elementsize);
            System.Diagnostics.Debug.Assert(AccCIL.AccCIL.GetObjType(t.GetElementType()) == objtype);

            byte[] localcopy = new byte[arraysize + ((16 - arraysize % 16) % 16)];
            Array.Copy(conv.Data, offset, localcopy, 0, localcopy.Length);
            SPEEmulator.EndianBitConverter c = new SPEEmulator.EndianBitConverter(localcopy);

            for (int i = 0; i < data.Length; i++)
            {
                switch (objtype)
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
                        throw new Exception("Unexpected data type: " + objtype.ToString());
                }
            }
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
            else if (rtype.IsArray)
                return conv.ReadUInt(offset);
            else
                throw new Exception("Return type not supported: " + rtype.FullName);
        }

        private static void WriteValue(Type t, SPEEmulator.EndianBitConverter conv, uint offset, object value, bool serialize)
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
            else if (t.IsArray)
            {
                conv.WriteUInt(offset, (uint)value);
                conv.WriteUInt(offset + 4, (uint)value);
                conv.WriteUInt(offset + 8, (uint)value);
                conv.WriteUInt(offset + 12, (uint)value);
            }
            else
                throw new Exception("Unsupported argument type: " + t.FullName);
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

            Type[] types = m_loadedMethodTypes;
            Dictionary<uint, int> addedObjects = new Dictionary<uint, int>();
            Dictionary<object, uint> objref = new Dictionary<object, uint>();
            m_objectRefs = new Dictionary<uint, object>();

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
                            objindex = objref[args[i]];
                        else
                        {
                            objindex = CreateObjectOnLS(types[i], args[i], m_typeSerializeIn[i], conv);
                            addedObjects.Add(objindex, i);
                            m_objectRefs.Add(objindex, args[i]);
                            objref[args[i]] = objindex;
                        }
                    }

                    WriteValue(typeof(uint), conv, lsoffset, objindex, true);
                }
                else
                {
                    WriteValue(types[i], conv, lsoffset, args[i], m_typeSerializeIn[i]);
                }
            }

            conv.WriteUInt(8, lsoffset);
            conv.WriteUInt(12, 0);

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

            //Now extract data back into the objects that are byref
            foreach (KeyValuePair<uint, int> k in addedObjects)
            {
                if (m_loadedMethodTypes[k.Value].IsArray)
                {
                    if (m_typeSerializeOut[k.Value])
                        TransferObjectFromLS(conv, k.Key, types[k.Value], args[k.Value]);
                }
                else
                    throw new Exception("Unexpected ref object");

                //Now remove the entry from the LS object table
                conv.WriteUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET + (k.Key * 16), 0);
                conv.WriteUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET + (k.Key * 16) + 4, 0);
                conv.WriteUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET + (k.Key * 16) + 8, 0);
                conv.WriteUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET + (k.Key * 16) + 12, 0);

                //Decrement the table counter
                uint objcount = conv.ReadUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET);
                conv.WriteUInt(SPEJITCompiler.OBJECT_TABLE_OFFSET, objcount - 1);
            }

            m_objectRefs = null;

            Type rtype = typeof(T);

            if (rtype == typeof(ReturnTypeVoid))
                return default(T);
            else if (!rtype.IsPrimitive)
            {
                uint objindex = (uint)ReadValue(rtype, conv, 0);
                if (objindex == null)
                    return default(T);

                if (!m_typeSerializeOut[addedObjects[objindex]])
                    throw new InvalidOperationException("Cannot return an object that was marked as [In] only");
                return (T)args[addedObjects[objindex]];
            }
            else
                return (T)ReadValue(rtype, conv, 0);
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

            if (!m.IsStatic)
            {
                Type argtype = m.DeclaringType;
                uint objindex = (uint)ReadValue(argtype, c, sp_offset);
                @this = m_objectRefs[objindex];

                transferred.Add(objindex, @this);
                TransferObjectFromLS(c, objindex, argtype, @this);

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
                    else
                    {
                        arguments[i] = m_objectRefs[objindx];
                        transferred.Add(objindx, arguments[i]);
                        TransferObjectFromLS(c, objindx, argtype, arguments[i]);
                    }
                }

                sp_offset += 16;
            }

            object result = m.Invoke(@this, arguments);
            int resultIndex = result == null ? 0 : -1;

            foreach (KeyValuePair<uint, object> t in transferred)
                if (t.Value != null && !t.Value.GetType().IsPrimitive)
                {
                    uint lsoffset = GetObjectLSAddress(t.Key, c);
                    TransferObjectToLS(t.Value.GetType(), lsoffset, t.Value, c);
                    if (t.Value == result)
                        resultIndex = (int)t.Key;
                }

            if (m.ReturnType != null)
            {
                if (m.ReturnType.IsPrimitive)
                    WriteValue(m.ReturnType, c, arg_base, result, true);
                else
                {
                    //TODO: This object is not cleaned from the LS
                    if (resultIndex < 0)
                        resultIndex = (int)CreateObjectOnLS(m.ReturnType, result, true, c);

                    WriteValue(m.ReturnType, c, arg_base, (uint)resultIndex, true);
                }
            }
            
            return true;
        }
    }
}
