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
    public class CellSPEEmulatorAccelerator : SPEAcceleratorBase
    {
        private System.Threading.ManualResetEvent m_workingEvent;
        private uint m_exitCode;

        public bool ShowGUI { get; set; }

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
            SPEObjectManager manager = new SPEObjectManager(conv);
            Dictionary<uint, int> transferedObjects = base.LoadInitialArguments(conv, manager, args);

            spe.RegisterCallbackHandler(SPEJITCompiler.STOP_METHOD_CALL & 0xff, spe_callback);

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

            return base.FinalizeAndReturn<T>(conv, manager, transferedObjects, args);
        }


        void spe_Exit(SPEEmulator.SPEProcessor sender, uint exitcode)
        {
            m_exitCode = exitcode;
        }

        void spe_SPEStopped(SPEEmulator.SPEProcessor sender)
        {
            m_workingEvent.Set();
        }

        private bool spe_callback(byte[] ls, uint offset)
        {
            return base.MethodCallback(new SPEEmulator.EndianBitConverter(ls), offset);
        }
    }
}
