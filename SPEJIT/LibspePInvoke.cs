using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace SPEJIT
{
    /// <summary>
    /// This class contains all the P/Invoke related code used to call libspe2.so
    /// </summary>
    internal class LibspePInvoke
    {
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
            NONE = 0,
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
            NONE = 0,
            SPE_EXIT = 1,
            SPE_STOP_AND_SIGNAL = 2,
            SPE_RUNTIME_ERROR = 3,
            SPE_RUNTIME_EXCEPTION = 4,
            SPE_RUNTIME_FATAL = 5,
            SPE_CALLBACK_ERROR = 6,
            SPE_ISOLATION_ERROR = 7
        }

        [StructLayout(LayoutKind.Sequential, Pack=1)]
        public struct spe_stop_info_t 
        {
            public StopReason stop_reason;
            public uint high_extra;
            public uint low_extra;
            public int spu_status;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct spe_start_t
        {
            public uint entrypoint;
        }

        //TODO: The constant should be UINT_MAX, but the argument must be type int, -1 is the same value for 32 bit,
        // but this does not work if it is sign extended to 64 bit before performing the add
        public static readonly spe_start_t SPE_DEFAULT_ENTRY = new spe_start_t() { entrypoint = uint.MaxValue };

        [DllImport("libc", SetLastError = true)]
        public static extern int chmod([MarshalAs(UnmanagedType.LPStr)] string filename, uint mode);
        //int chmod(const char *path, mode_t mode);

        [DllImport("libspe2", SetLastError = true)]
        public static extern IntPtr spe_context_create(CreateFlags flags, IntPtr gang_context);
        //spe_context_ptr_t spe_context_create(unsigned int flags, spe_gang_context_ptr_t gang)

        [DllImport("libspe2", SetLastError = true)]
        public static extern int spe_context_destroy(IntPtr context);
        //int spe_context_destroy(spe_context_ptr_t spe);

        [DllImport("libspe2", SetLastError = true)]
        public static extern int spe_context_run(IntPtr spe, ref spe_start_t entry, Runflags runflags, IntPtr argp, IntPtr envp, ref spe_stop_info_t stopinfo);
        //int spe_context_run(spe_context_ptr_t spe, unsigned int *entry, unsigned int runflags, void *argp, void *envp, spe_stop_info_t *stopinfo);

        [DllImport("libspe2", SetLastError = true)]
        public static extern IntPtr spe_image_open([MarshalAs(UnmanagedType.LPStr)] string filename);
        //spe_program_handle_t * spe_image_open (const char *filename);

        [DllImport("libspe2", SetLastError = true)]
        public static extern int spe_image_close(IntPtr handle);
        //int spe_image_close (spe_program_handle_t *program);

        [DllImport("libspe2", SetLastError = true)]
        public static extern int spe_program_load(IntPtr spe, IntPtr program);
        //int spe_program_load(spe_context_ptr_t spe, spe_program_handle_t* program);

        [DllImport("libspe2", SetLastError = true)]
        public static extern IntPtr spe_ls_area_get(IntPtr spe);
        //void * spe_ls_area_get (spe_context_ptr_t spe);

        [DllImport("libspe2", SetLastError = true)]
        public static extern int spe_ls_size_get(IntPtr spe);
        //int spe_ls_size_get(spe_context_ptr_t spe);
    }
}
