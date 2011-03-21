using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPEJIT
{
    public abstract class SPEAcceleratorBase : AccCIL.AccelleratorBase
    {
        protected SPEJIT.SPEJITCompiler m_compiler = new SPEJITCompiler();
        protected string m_elf = null;
        protected Dictionary<int, Mono.Cecil.MethodReference> m_callpoints = null;

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


        protected Dictionary<uint, int> LoadInitialArguments(SPEEmulator.IEndianBitConverter conv, SPEObjectManager manager, object[] args)
        {
            conv.WriteUInt(0, 0);
            conv.WriteUInt(4, (uint)args.Length);

            uint lsoffset = manager.ObjectTable.Memsize - (16 * 8);
            //The -(16 * 8) is used to prevent the bootloader stack setup from overwriting the arguments

            Dictionary<uint, int> transferedObjects = new Dictionary<uint, int>();

            for (int i = args.Length - 1; i >= 0; i--)
            {
                lsoffset -= 16;

                if (m_loadedMethodTypes[i].IsPrimitive)
                    manager.WriteRegisterPrimitiveValue(args[i], lsoffset);
                else
                {
                    uint objindex = manager.CreateObjectOnLS(args[i]);
                    if (objindex != 0 && !transferedObjects.ContainsKey(objindex))
                        transferedObjects[objindex] = i;

                    manager.WriteRegisterPrimitiveValue(objindex, lsoffset);
                }
            }

            conv.WriteUInt(8, lsoffset);
            conv.WriteUInt(12, 0);

            manager.SaveObjectTable();

            return transferedObjects;
        }

        protected T FinalizeAndReturn<T>(SPEEmulator.IEndianBitConverter conv, SPEObjectManager manager, Dictionary<uint, int> transferedObjects, object[] args)
        {
            SPEObjectManager newmanager = new SPEObjectManager(conv);

            //Now extract data back into the objects that are byref
            foreach (KeyValuePair<uint, int> k in transferedObjects)
            {
                if (m_typeSerializeOut[k.Value])
                    newmanager.ReadObjectFromLS(k.Key, args[k.Value]);

                //Remove the entry from the LS object table
                manager.DeleteObject(k.Key);
            }

            //Write back the old, unmodified object table, 
            manager.SaveObjectTable();

            Type rtype = typeof(T);

            if (rtype == typeof(ReturnTypeVoid))
                return default(T);
            else if (rtype.IsPrimitive)
                return (T)newmanager.ReadRegisterPrimitiveValue(rtype, 0);
            else
            {
                uint objindex = (uint)newmanager.ReadRegisterPrimitiveValue(typeof(uint), 0);
                if (objindex == 0)
                    return default(T);

                return (T)newmanager.ReadObjectFromLS(objindex);
            }
        }

        protected bool MethodCallback(SPEEmulator.IEndianBitConverter c, uint offset)
        {
            uint sp = c.ReadUInt(offset);

            //Stack size is 77 elements, and return address is next next instruction
            uint call_address = (c.ReadUInt(sp + (16 * 77)) - 4) / 4;

            Mono.Cecil.MethodReference calledmethod;
            m_callpoints.TryGetValue((int)call_address, out calledmethod);

            if (calledmethod == null)
                throw new Exception("No method call registered at " + call_address);


            //All good, we have a real function, now load all required arguments onto PPE
            object[] arguments = new object[calledmethod.Parameters.Count];

            System.Reflection.MethodInfo m = AccCIL.AccCIL.FindReflectionMethod(calledmethod);
            if (m == null)
                throw new Exception("Unable to find function called: " + calledmethod.DeclaringType.FullName + "::" + calledmethod.Name);

            uint arg_base = sp + 32;
            uint sp_offset = arg_base;
            object @this = null;

            SPEObjectManager manager = new SPEObjectManager(c);

            if (!m.IsStatic)
            {
                Type argtype = m.DeclaringType;
                uint objindex = (uint)manager.ReadRegisterPrimitiveValue(typeof(uint), sp_offset);
                @this = manager.ReadObjectFromLS(objindex);

                sp_offset += 16;
            }

            for (int i = 0; i < arguments.Length; i++)
            {
                Type argtype = Type.GetType(calledmethod.Parameters[i].ParameterType.FullName);
                arguments[i] = manager.ReadRegisterPrimitiveValue(argtype.IsPrimitive ? argtype : typeof(uint), sp_offset);
                if (!argtype.IsPrimitive)
                    arguments[i] = manager.ReadObjectFromLS((uint)arguments[i]);

                sp_offset += 16;
            }

            object result = m.Invoke(@this, arguments);
            int resultIndex = result == null ? 0 : -1;

            foreach (KeyValuePair<uint, object> t in manager.KnownObjectsById)
                if (t.Value != null)
                {
                    //Strings are imutable, so there is no reason to transfer them back
                    if (manager.ObjectTable[t.Key].KnownType == AccCIL.KnownObjectTypes.String)
                        continue;

                    manager.WriteObjectToLS(t.Key, t.Value);

                    if (t.Value == result)
                        resultIndex = (int)t.Key;
                }

            if (m.ReturnType != null)
            {
                if (m.ReturnType.IsPrimitive)
                    manager.WriteRegisterPrimitiveValue(result, arg_base);
                else
                {
                    if (resultIndex < 0)
                    {
                        resultIndex = (int)manager.CreateObjectOnLS(result);
                        manager.ObjectTable[(uint)resultIndex].Refcount = 1;
                        manager.SaveObjectTable();
                    }

                    manager.WriteRegisterPrimitiveValue((uint)resultIndex, arg_base);
                }
            }

            return true;
        }

        public override void Dispose()
        {
#if !DEBUG
            if (m_elf != null)
                try { System.IO.File.Delete(m_elf); }
                catch { }
#endif
        }
    }
}
