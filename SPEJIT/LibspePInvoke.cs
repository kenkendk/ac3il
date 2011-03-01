using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace SPEJIT
{
    internal class LibspePInvoke
    {
        public struct spe_context_t {

        }

        public struct spe_stop_info_t {

        }

        //int spe_context_run(spe_context_ptr_t spe, unsigned int *entry, unsigned int runflags, void *argp, void *envp, spe_stop_info_t *stopinfo);

        [DllImport("libspe", SetLastError = true)]
        public static extern int spe_context_run(ref spe_context_t spe, ref uint start_instruction, uint runflags, IntPtr argp, IntPtr envp, ref spe_stop_info_t stopinfo);

    }
}
