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
        private class SPEWrapper : IDisposable
        {
            LibspePInvoke.spe_context_t m_context;
            LibspePInvoke.spe_program_handle_t m_program;

            public SPEWrapper()
            {
                m_context = LibspePInvoke.spe_context_create(LibspePInvoke.CreateFlags.SPE_MAP_PS, IntPtr.Zero);
            }

            public void LoadELF(string filename)
            {
                if (m_program.data != IntPtr.Zero)
                    LibspePInvoke.spe_image_close(m_program);

                m_program = LibspePInvoke.spe_image_open(filename);
            }

            public int Run(LibspePInvoke.CallbackHandlerDelegate callbackhandler)
            {
                int result = 0;

                LibspePInvoke.spe_stop_info_t stop = new LibspePInvoke.spe_stop_info_t();
                
                do
                {
                    result = LibspePInvoke.spe_context_run(m_context, LibspePInvoke.SPE_DEFAULT_ENTRY, LibspePInvoke.Runflags.SPE_NO_CALLBACKS, IntPtr.Zero, IntPtr.Zero, ref stop);
                    if (result >= 0)
                    {
                        if (stop.stop_reason == LibspePInvoke.StopReason.SPE_EXIT)
                            return (int)(stop.high_extra & 0x3fff);
                        if (stop.stop_reason == LibspePInvoke.StopReason.SPE_STOP_AND_SIGNAL)
                        {
                            if (stop.high_extra != SPEJITCompiler.STOP_METHOD_CALL)
                                throw new Exception(string.Format("Bad stop and signal type: 0x{0:x4}", stop.high_extra));

                            callbackhandler(this.LS, (uint)stop.spu_status);
                        }
                        else
                            throw new Exception("Bad stuff happened: " + stop.stop_reason.ToString());
                    }

                } while(result > 0);

                throw new Exception(string.Format("Error while running spe, resultcode {0}, stopreason {1}", result, stop.stop_reason.ToString()));
            }

            public IntPtr LS { get { return LibspePInvoke.spe_ls_area_get(m_context); } }
            public int Size { get { return LibspePInvoke.spe_ls_size_get(m_context); } }

            #region IDisposable Members

            public void Dispose()
            {

                if (m_program.data != IntPtr.Zero)
                {
                    LibspePInvoke.spe_image_close(m_program);
                    m_program.data = IntPtr.Zero;
                }

                if (m_context.data != IntPtr.Zero)
                {
                    LibspePInvoke.spe_context_destroy(m_context);
                    m_context.data = IntPtr.Zero;
                }
            }

            #endregion
        }

        private SPEWrapper m_spe = null;

        public override T Execute<T>(params object[] arguments)
        {
            //TODO: Should be cached?
            m_spe.LoadELF(m_elf);

            LSBitConverter conv = new LSBitConverter(m_spe.LS);
            SPEObjectManager manager = new SPEObjectManager(conv);
            if (manager.ObjectTable.Memsize != m_spe.Size)
                throw new Exception(string.Format("Unexpected size in ELF {0} should be {1}", manager.ObjectTable.Memsize, m_spe.Size));

            Dictionary<uint, int> transferedObjects = base.LoadInitialArguments(conv, manager, arguments);

            int r = m_spe.Run(new LibspePInvoke.CallbackHandlerDelegate(spe_callback));
            if (r != SPEJITCompiler.STOP_SUCCESSFULL)
                throw new Exception("Unexpected return code: " + r);

            return base.FinalizeAndReturn<T>(conv, manager, transferedObjects, arguments);
        }

        private int spe_callback(IntPtr ls, uint offset)
        {
            return base.MethodCallback(new LSBitConverter(ls), offset) ? 1 : 0;
        }


        public override void Dispose() 
        {
            base.Dispose();

            if (m_spe != null)
            {
                m_spe.Dispose();
                m_spe = null;
            }
        }

    }
}
