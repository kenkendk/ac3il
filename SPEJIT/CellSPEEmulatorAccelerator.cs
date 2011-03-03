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
                m_compiler.EmitELFStream(outfile, sw, methods);
                Console.WriteLine("Converted output size in bytes: " + outfile.Length);
            }
#else
            using (System.IO.FileStream outfile = new System.IO.FileStream(elffile, System.IO.FileMode.Create))
                m_compiler.EmitELFStream(outfile, null, methods); 
#endif
            m_elf = elffile;
        }

        protected override AccCIL.IJITCompiler Compiler
        {
            get { return m_compiler; }
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


            spe.WriteLSWord(0, 0);
            spe.WriteLSWord(4, (uint)args.Length);

            uint lsoffset = (uint)spe.LS.Length - (16 * 8);

            Type[] types = m_loadedMethodTypes;
            Dictionary<uint, int> addedObjects = new Dictionary<uint, int>();

            for (int i = args.Length - 1; i >= 0; i--)
            {
                lsoffset -= 16;

                if (types[i] == typeof(int) || types[i] == typeof(uint) || types[i] == typeof(short) || types[i] == typeof(ushort) || types[i] == typeof(byte) || types[i] == typeof(sbyte))
                {
                    spe.WriteLSWord(lsoffset, (uint)Convert.ToInt32(args[i]));
                    spe.WriteLSWord(lsoffset + 4, (uint)Convert.ToInt32(args[i]));
                    spe.WriteLSWord(lsoffset + 8, (uint)Convert.ToInt32(args[i]));
                    spe.WriteLSWord(lsoffset + 12, (uint)Convert.ToInt32(args[i]));
                }
                else if (types[i] == typeof(long) || types[i] == typeof(ulong))
                {
                    spe.WriteLSDWord(lsoffset, (ulong)Convert.ToInt64(args[i]));
                    spe.WriteLSDWord(lsoffset + 8, (ulong)Convert.ToInt64(args[i]));
                }
                else if (types[i] == typeof(float))
                {
                    spe.WriteLSFloat(lsoffset, (float)args[i]);
                    spe.WriteLSFloat(lsoffset + 4, (float)args[i]);
                    spe.WriteLSFloat(lsoffset + 8, (float)args[i]);
                    spe.WriteLSFloat(lsoffset + 12, (float)args[i]);
                }
                else if (types[i] == typeof(double))
                {
                    spe.WriteLSDouble(lsoffset, (double)args[i]);
                    spe.WriteLSDouble(lsoffset + 8, (double)args[i]);
                }
                else if (types[i] == typeof(bool))
                {
                    spe.WriteLSWord(lsoffset, (uint)(((bool)args[i]) ? 1 : 0));
                    spe.WriteLSWord(lsoffset + 4, (uint)(((bool)args[i]) ? 1 : 0));
                    spe.WriteLSWord(lsoffset + 8, (uint)(((bool)args[i]) ? 1 : 0));
                    spe.WriteLSWord(lsoffset + 12, (uint)(((bool)args[i]) ? 1 : 0));
                }
                else if (types[i].IsArray)
                {
                    if (args[i] == null)
                    {
                        spe.WriteLSWord(lsoffset, 0);
                        spe.WriteLSWord(lsoffset + 4, 0);
                        spe.WriteLSWord(lsoffset + 8, 0);
                        spe.WriteLSWord(lsoffset + 12, 0);
                    }
                    else
                    {

                        //Register a new object in the object table
                        uint tablecount = spe.ReadLSWord(SPEJITCompiler.OBJECT_TABLE_OFFSET - 16);
                        uint tablesize = spe.ReadLSWord(SPEJITCompiler.OBJECT_TABLE_OFFSET - 16 + 4);

                        if (tablecount == tablesize)
                            throw new Exception("Not enough space in SPE object table");

                        uint nextoffset = 0;
                        for (uint j = 0; j < tablecount; j++)
                        {
                            uint objsize = spe.ReadLSWord(SPEJITCompiler.OBJECT_TABLE_OFFSET + (j * 16) + 4);
                            uint objoffset = spe.ReadLSWord(SPEJITCompiler.OBJECT_TABLE_OFFSET + (j * 16) + 8);
                            nextoffset = Math.Max(nextoffset, objoffset + objsize + ((16 - objsize % 16) % 16));
                        }

                        Type elType = types[i].GetElementType();
                        AccCIL.KnownObjectTypes objtype = AccCIL.AccCIL.GetObjType(elType);
                        uint elementsize = 1u << (int)BuiltInMethods.get_array_elem_len_mult(objtype);

                        Array arr = (Array)args[i];
                        uint arraysize = (uint)((arr).Length * elementsize);
                        uint requiredSpace = arraysize + ((16 - arraysize % 16) % 16);

                        if (nextoffset + requiredSpace > spe.LSLR)
                            throw new Exception(string.Format("Unable to fit array of size {0} onto spe at offset {1}", requiredSpace, nextoffset));

                        //Write the object table description
                        spe.WriteLSWord(SPEJITCompiler.OBJECT_TABLE_OFFSET + (tablecount * 16), (uint)objtype);
                        spe.WriteLSWord(SPEJITCompiler.OBJECT_TABLE_OFFSET + (tablecount * 16) + 4, arraysize);
                        spe.WriteLSWord(SPEJITCompiler.OBJECT_TABLE_OFFSET + (tablecount * 16) + 8, nextoffset);
                        spe.WriteLSWord(SPEJITCompiler.OBJECT_TABLE_OFFSET + (tablecount * 16) + 12, 0);

                        //Update the table counter
                        spe.WriteLSWord(SPEJITCompiler.OBJECT_TABLE_OFFSET - 16, tablecount + 1);


                        byte[] localbuffer = new byte[requiredSpace];

                        if (m_typeSerializeIn[i])
                        {
                            //Build contents in local memory
                            EndianBitConverter ebc = new EndianBitConverter(localbuffer);

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

                        }

                        //Copy data over
                        Array.Copy(localbuffer, 0, spe.LS, nextoffset, localbuffer.Length);

                        //Set the register to the object table index
                        spe.WriteLSWord(lsoffset, tablecount);
                        spe.WriteLSWord(lsoffset + 4, tablecount);
                        spe.WriteLSWord(lsoffset + 8, tablecount);
                        spe.WriteLSWord(lsoffset + 12, tablecount);
                        addedObjects.Add(tablecount, i);

                    }
                }
                else
                    throw new Exception("Unsupported argument type: " + args[i].GetType());
            }

            spe.WriteLSWord(8, lsoffset);
            spe.WriteLSWord(12, 0);

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
                if (m_exitCode != (SPEJITCompiler.STOP_SUCCESSFULL & 0xff))
                    throw new Exception("Invalid exitcode: " + m_exitCode);

            }

            //Now extract data back into the objects that are byref
            foreach (KeyValuePair<uint, int> k in addedObjects)
            {
                if (m_loadedMethodTypes[k.Value].IsArray)
                {
                    if (m_typeSerializeOut[k.Value])
                    {
                        AccCIL.KnownObjectTypes objtype = (AccCIL.KnownObjectTypes)spe.ReadLSWord(SPEJITCompiler.OBJECT_TABLE_OFFSET + (k.Key * 16));
                        uint arraysize = spe.ReadLSWord(SPEJITCompiler.OBJECT_TABLE_OFFSET + (k.Key * 16) + 4);
                        uint offset = spe.ReadLSWord(SPEJITCompiler.OBJECT_TABLE_OFFSET + (k.Key * 16) + 8);

                        uint elementsize = 1u << (int)BuiltInMethods.get_array_elem_len_mult(objtype);
                        Array data = (Array)args[k.Value];

                        System.Diagnostics.Debug.Assert(data.Length == arraysize / elementsize);
                        System.Diagnostics.Debug.Assert(AccCIL.AccCIL.GetObjType(m_loadedMethodTypes[k.Value].GetElementType()) == objtype);

                        byte[] localcopy = new byte[arraysize + ((16 - arraysize % 16) % 16)];
                        Array.Copy(spe.LS, offset, localcopy, 0, localcopy.Length);
                        EndianBitConverter c = new EndianBitConverter(localcopy);

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
                }
                else
                    throw new Exception("Unexpected ref object");

                //Now remove the entry from the LS object table
                spe.WriteLSWord(SPEJITCompiler.OBJECT_TABLE_OFFSET + (k.Key * 16), 0);
                spe.WriteLSWord(SPEJITCompiler.OBJECT_TABLE_OFFSET + (k.Key * 16) + 4, 0);
                spe.WriteLSWord(SPEJITCompiler.OBJECT_TABLE_OFFSET + (k.Key * 16) + 8, 0);
                spe.WriteLSWord(SPEJITCompiler.OBJECT_TABLE_OFFSET + (k.Key * 16) + 12, 0);

                //Decrement the table counter
                uint objcount = spe.ReadLSWord(SPEJITCompiler.OBJECT_TABLE_OFFSET);
                spe.WriteLSWord(SPEJITCompiler.OBJECT_TABLE_OFFSET, objcount - 1);
            }

            
            Type rtype = typeof(T);

            if (rtype == typeof(uint))
                return (T)Convert.ChangeType(spe.ReadLSWord(0), typeof(T));
            else if (rtype == typeof(int))
                return (T)Convert.ChangeType((int)spe.ReadLSWord(0), typeof(T));
            else if (rtype == typeof(short))
                return (T)Convert.ChangeType((short)(spe.ReadLSWord(0) & 0xffff), typeof(T));
            else if (rtype == typeof(ushort))
                return (T)Convert.ChangeType((ushort)(spe.ReadLSWord(0) & 0xffff), typeof(T));
            else if (rtype == typeof(byte))
                return (T)Convert.ChangeType((byte)(spe.ReadLSWord(0) & 0xff), typeof(T));
            else if (rtype == typeof(sbyte))
                return (T)Convert.ChangeType((sbyte)(spe.ReadLSWord(0) & 0xff), typeof(T));
            else if (rtype == typeof(ulong))
                return (T)Convert.ChangeType(spe.ReadLSDWord(0), typeof(T));
            else if (rtype == typeof(long))
                return (T)Convert.ChangeType((long)spe.ReadLSDWord(0), typeof(T));
            else if (rtype == typeof(float))
                return (T)Convert.ChangeType(spe.ReadLSFloat(0), typeof(T));
            else if (rtype == typeof(double))
                return (T)Convert.ChangeType(spe.ReadLSDouble(0), typeof(T));
            else if (rtype == typeof(ReturnTypeVoid))
                return default(T);
            else if (rtype.IsArray)
            {
                uint objindex = spe.ReadLSWord(0);
                if (objindex == 0)
                    return default(T);
                else
                {
                    if (!m_typeSerializeOut[addedObjects[objindex]])
                        throw new InvalidOperationException("Cannot return an object that was marked as [In] only");
                    return (T)args[addedObjects[objindex]];
                }
            }
            else
                throw new Exception("Return type not supported: " + rtype.FullName);
        }

        void spe_Exit(SPEEmulator.SPEProcessor sender, uint exitcode)
        {
            m_exitCode = exitcode;
        }

        void spe_SPEStopped(SPEEmulator.SPEProcessor sender)
        {
            m_workingEvent.Set();
        }

    }
}
