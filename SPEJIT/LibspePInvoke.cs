using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace SPEJIT
{
    internal class LibspePInvoke
    {
        public delegate int CallbackHandlerDelegate(IntPtr ls, uint sp);

        public enum CreateFlags : uint
        {
            SPE_CFG_SIGNOTIFY1_OR = 0x00000010,
            SPE_CFG_SIGNOTIFY2_OR = 0x00000020,
            SPE_MAP_PS = 0x00000040,
            SPE_ISOLATED = 0x00000080,
            SPE_ISOLATED_EMULATE = 0x00000100,
            SPE_EVENTS_ENABLE = 0x00001000,
            SPE_AFFINITY_MEMORY = 0x00002000
        }

        public enum CallbackMode : uint
        {
            SPE_CALLBACK_NEW = 1,
            SPE_CALLBACK_UPDATE = 2
        }

        public enum Runflags : uint
        {
            /// <summary>
            /// Specifies that the SPE setup registers r3, r4, and r5 are initialized with the 48 bytes pointed to by argp
            /// </summary>
            SPE_RUN_USER_REGS = 1,
            /// <summary>
            /// Specifies that registered SPE library calls ("callbacks" from this library's view) should not run automatically if a callback is encountered. 
            /// This also disables callbacks that are predefined in the library implementation. See PPE-assisted library calls for details. 
            /// spe_context_run returns as if the SPU would have issued a regular stop and signal instruction. The signal code is returned as part of stopinfo.
            /// </summary>
            SPE_NO_CALLBACKS = 2
        }

        public enum StopReason : uint
        {
            SPE_EXIT = 1,
            SPE_STOP_AND_SIGNAL = 2,
            SPE_RUNTIME_ERROR = 3,
            SPE_RUNTIME_EXCEPTION = 4,
            SPE_RUNTIME_FATAL = 5,
            SPE_CALLBACK_ERROR = 6,
            SPE_ISOLATION_ERROR = 7
        }

        public struct spe_gang_context_t
        {
            public IntPtr data;
        }

        public struct spe_context_t 
        {
            public IntPtr data;
        }

        public struct spe_stop_info_t 
        {
            public StopReason stop_reason;
            public uint high_extra;
            public uint low_extra;
            public int spu_status;
        }

        public struct spe_program_handle_t
        {
            public IntPtr data;
        }

        //TODO: The constant should be UINT_MAX, but the argument must be type int, -1 is the same value for 32 bit,
        // but this does not work if it is sign extended to 64 bit before performing the add
        public static readonly IntPtr SPE_DEFAULT_ENTRY = IntPtr.Add(IntPtr.Zero, -1);

        [DllImport("libspe", SetLastError = true)]
        public static extern spe_context_t spe_context_create(CreateFlags flags, spe_gang_context_t gang_context);
        [DllImport("libspe", SetLastError = true)]
        public static extern spe_context_t spe_context_create(CreateFlags flags, IntPtr gang_context);
        //spe_context_ptr_t spe_context_create(unsigned int flags, spe_gang_context_ptr_t gang)

        [DllImport("libspe", SetLastError = true)]
        public static extern int spe_context_destroy(spe_context_t context);
        //int spe_context_destroy(spe_context_ptr_t spe);

        [DllImport("libspe", SetLastError = true)]
        public static extern int spe_context_run(spe_context_t spe, IntPtr start_instruction, Runflags runflags, IntPtr argp, IntPtr envp, ref spe_stop_info_t stopinfo);
        //int spe_context_run(spe_context_ptr_t spe, unsigned int *entry, unsigned int runflags, void *argp, void *envp, spe_stop_info_t *stopinfo);

        [DllImport("libspe", SetLastError = true)]
        public static extern spe_program_handle_t spe_image_open(string filename);
        //spe_program_handle_t * spe_image_open (const char *filename);

        [DllImport("libspe", SetLastError = true)]
        public static extern int spe_image_close(spe_program_handle_t handle);
        //int spe_image_close (spe_program_handle_t *program);

        [DllImport("libspe", SetLastError = true)]
        public static extern int spe_program_load(spe_context_t spe, spe_program_handle_t program);
        //int spe_program_load(spe_context_ptr_t spe, spe_program_handle_t* program);

        [DllImport("libspe", SetLastError = true)]
        public static extern IntPtr spe_ls_area_get(spe_context_t spe);
        //void * spe_ls_area_get (spe_context_ptr_t spe);

        [DllImport("libspe", SetLastError = true)]
        public static extern int spe_ls_size_get(spe_context_t spe);
        //int spe_ls_size_get(spe_context_ptr_t spe);

        [DllImport("libspe", SetLastError = true)]
        public static extern int spe_callback_handler_register(CallbackHandlerDelegate handler, uint callnum, CallbackMode mode);
        //int spe_callback_handler_register (void *handler, unsigned int callnum, unsigned int mode);

        [DllImport("libspe", SetLastError = true)]
        public static extern int spe_callback_handler_deregister(uint callnum);
        //int spe_callback_handler_deregister (unsigned int callnum);
    }
}
