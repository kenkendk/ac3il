using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPEJIT
{
    /// <summary>
    /// Implementation of an SPE controller for a physical SPE device
    /// </summary>
    public class CellSPEAccelerator : SPEAcceleratorBase
    {
        /// <summary>
        /// Returns a value indicating if the machine has a physcial SPE
        /// </summary>
        public static bool HasHardwareSPE
        {
            get
            {
                try
                {
                    using (new SPEWrapper()) { }
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private delegate bool CallbackHandlerDelegate(IntPtr ls, uint sp);

        /// <summary>
        /// This class wraps all the details related to using pointer arithmetics,
        /// for communicating with libspe2.so
        /// </summary>
        private class SPEWrapper : IDisposable
        {
            IntPtr m_context;
            IntPtr m_program;

            public SPEWrapper()
            {
                m_context = LibspePInvoke.spe_context_create(LibspePInvoke.CreateFlags.SPE_MAP_PS, IntPtr.Zero);

                if (m_context == IntPtr.Zero)
                    throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
            }

            public void LoadELF(string filename)
            {
                if (!System.IO.File.Exists(filename))
                    throw new System.IO.FileNotFoundException(new System.IO.FileNotFoundException().Message, filename);

                //The spe elf must be executable otherwise libspe will not load it
                if (LibspePInvoke.chmod(filename, 511 /*777 octal*/) != 0)
                    throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());

                if (m_program != IntPtr.Zero)
                    LibspePInvoke.spe_image_close(m_program);

                m_program = LibspePInvoke.spe_image_open(filename);

                if (m_program == IntPtr.Zero)
                    throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());

                int res = LibspePInvoke.spe_program_load(m_context, m_program);
                
                if (res != 0)
                    throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
            }

            public int Run(CallbackHandlerDelegate callbackhandler)
            {
                int result = 0;

                LibspePInvoke.spe_stop_info_t stop = new LibspePInvoke.spe_stop_info_t();
                stop.stop_reason = LibspePInvoke.StopReason.NONE;
                LibspePInvoke.spe_start_t start = LibspePInvoke.SPE_DEFAULT_ENTRY;


                do
                {
                    //Console.WriteLine("Running context: {0}, errno: {1}", m_context.ToInt64(), System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                    result = LibspePInvoke.spe_context_run(m_context, ref start, LibspePInvoke.Runflags.SPE_NO_CALLBACKS, IntPtr.Zero, IntPtr.Zero, ref stop);
                    //Console.WriteLine("Ran context: {0}, errno: {1}, result: {2}, stop_reason: {3}, extra_low: {4}, extra_high: {5}, spu_status: {6}, pc: {7} (0x{7:x4})", m_context.ToInt64(), System.Runtime.InteropServices.Marshal.GetLastWin32Error(), result, stop.stop_reason, stop.low_extra, stop.high_extra, stop.spu_status, start.entrypoint);
                    if (result >= 0)
                    {
                        if (stop.stop_reason == LibspePInvoke.StopReason.SPE_EXIT)
                            return (int)(stop.low_extra & 0x00ff);
                        if (stop.stop_reason == LibspePInvoke.StopReason.SPE_STOP_AND_SIGNAL)
                        {
                            if (stop.low_extra != SPEJITCompiler.STOP_METHOD_CALL)
                                throw new Exception(string.Format("Bad stop and signal type: 0x{0:x4}", stop.low_extra));

                            if (!callbackhandler(this.LS, (uint)start.entrypoint))
                                throw new Exception("Error in callback handler");

                            //Skip over the data for the callback
                            start.entrypoint += 4;
                        }
                        else
                            throw new Exception("Bad stuff happened: " + stop.stop_reason.ToString());
                    }

                } while(result > 0);

                throw new Exception(string.Format("Error while running spe, resultcode {0}, stopreason {1}", result, stop.stop_reason.ToString()));
            }

            public IntPtr LS
            {
                get
                {
                    IntPtr res = LibspePInvoke.spe_ls_area_get(m_context);
                    if (res == IntPtr.Zero)
                        throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());

                    return res;
                }
            }
            public int Size
            {
                get
                {
                    int res = LibspePInvoke.spe_ls_size_get(m_context);
                    if (res <= 0)
                        throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                    return res;
                }
            }

            #region IDisposable Members

            public void Dispose()
            {

                if (m_program != IntPtr.Zero)
                {
                    LibspePInvoke.spe_image_close(m_program);
                    m_program = IntPtr.Zero;
                }

                if (m_context != IntPtr.Zero)
                {
                    LibspePInvoke.spe_context_destroy(m_context);
                    m_context = IntPtr.Zero;

                }
            }

            #endregion
        }

        public override T Execute<T>(params object[] arguments)
        {
            using (SPEWrapper spe = new SPEWrapper())
            {
                //TODO: Should be cached?
                spe.LoadELF(m_elf);

                LSBitConverter conv = new LSBitConverter(spe.LS);
                SPEObjectManager manager = new SPEObjectManager(conv);
                if (manager.ObjectTable.Memsize != spe.Size)
                    throw new Exception(string.Format("Unexpected size in loaded ELF {0} should be {1}", manager.ObjectTable.Memsize, spe.Size));

                Dictionary<uint, int> transferedObjects = base.LoadInitialArguments(conv, manager, arguments);

                int r = spe.Run(new CallbackHandlerDelegate(spe_callback));
                if (r != (SPEJITCompiler.STOP_SUCCESSFULL & 0xff))
                    throw new Exception("Unexpected return code: " + r);

                return base.FinalizeAndReturn<T>(conv, manager, transferedObjects, arguments);
            }
        }

        private bool spe_callback(IntPtr ls, uint offset)
        {
            //Console.WriteLine("Callback with ls: {0}, offset: {1}", ls.ToInt64(), offset);
            SPEEmulator.IEndianBitConverter conv = new LSBitConverter(ls);
            return base.MethodCallback(conv, offset);
        }
    }
}
