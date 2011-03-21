using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Mono.Cecil;

namespace CUDAJIT
{
    public class CUDAManager : AccCIL.AccelleratorBase
    {
        private CUDAJITCompiler m_compiler = new CUDAJITCompiler();
        private string m_ptx = null;
        private string m_entryMethod;

        public override void LoadProgram(IEnumerable<AccCIL.ICompiledMethod> methods)
        {
            string startPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string program = System.IO.Path.Combine(startPath, methods.First().Method.Method.DeclaringType.Module.Assembly.Name.Name);

            m_entryMethod = methods.FirstOrDefault().Method.Method.Name;
#if DEBUG
            // Create ELF
            string ptxfile = System.IO.Path.Combine(startPath, program + ".ptx");
            using (System.IO.FileStream outfile = new System.IO.FileStream(ptxfile, System.IO.FileMode.Create))
            using (System.IO.TextWriter sw = new System.IO.StreamWriter(System.IO.Path.Combine(startPath, program + ".asm")))
            {
                //m_compiler.EmitELFStream(outfile, sw, methods);
                Console.WriteLine("Converted output size in bytes: " + outfile.Length);
            }
#else
            using (System.IO.FileStream outfile = new System.IO.FileStream(elffile, System.IO.FileMode.Create))
                m_compiler.EmitELFStream(outfile, null, methods); 
#endif
            m_ptx = ptxfile;
        }

        // Number of entries in vector
        static int N = 256;
        // Number of threads per block
        static int threadsPerBlock = 256;

        public override T Execute<T>(params object[] args)
        {
            if (string.IsNullOrEmpty(m_entryMethod))
                throw new Exception("No method is loaded");

            // Create cuda context
            GASS.CUDA.CUDA cuda = new GASS.CUDA.CUDA(true);

            // Load module (PTX) and get function
            GASS.CUDA.Types.CUmodule module = cuda.LoadModule(m_ptx);
            GASS.CUDA.Types.CUfunction func = cuda.GetModuleFunction(module, m_entryMethod);

            // Create data
            float[] A = new float[N];
            float[] B = new float[N];
            float[] C = new float[N];

            Random rand = new Random();

            for (int i = 0; i < N; i++)
            {
                A[i] = rand.Next(1000);
                B[i] = rand.Next(1000);
                C[i] = -1;
            }

            // Allocate data on device
            GASS.CUDA.Types.CUdeviceptr dA = cuda.Allocate<float>(A);
            GASS.CUDA.Types.CUdeviceptr dB = cuda.Allocate<float>(B);
            GASS.CUDA.Types.CUdeviceptr dC = cuda.Allocate<float>(C);

            // Copy data to device
            cuda.CopyHostToDevice<float>(dA, A);
            cuda.CopyHostToDevice<float>(dB, B);
            cuda.CopyHostToDevice<float>(dC, C);

            // Load parameters
            int offset = 0;

            cuda.SetParameter(func, offset, dA);
            offset += Marshal.SizeOf(typeof(IntPtr));

            cuda.SetParameter(func, offset, dB);
            offset += Marshal.SizeOf(typeof(IntPtr));

            cuda.SetParameter(func, offset, dC);
            offset += Marshal.SizeOf(typeof(IntPtr));

            cuda.SetParameterSize(func, (uint)offset);

            // Setup execution
            int blocksPerGrid = (N + threadsPerBlock - 1) / threadsPerBlock;
            cuda.SetFunctionBlockShape(func, threadsPerBlock, 1, 1);

            // Launch execution
            cuda.Launch(func, blocksPerGrid, 1);

            // Wait for and retrive result
            cuda.SynchronizeContext();
            cuda.CopyDeviceToHost<float>(dC, C);

            // Clean device memory
            cuda.Free(dA);
            cuda.Free(dB);
            cuda.Free(dC);

            return default(T);
        }

        protected override AccCIL.IJITCompiler Compiler
        {
            get { return m_compiler; }
        }

        /// <summary>
        /// Print GPU for primary card
        /// </summary>
        static void GPUstats()
        {
            GASS.CUDA.CUDA cuda = new GASS.CUDA.CUDA(0, true);

            Console.WriteLine("Device \"{0}\"", cuda.CurrentDevice.Name);
            Console.WriteLine("\tCUDA Capability Major revision number:\t\t{0}", cuda.CurrentDevice.ComputeCapability.Major);
            Console.WriteLine("\tCUDA Capability Minor revision number:\t\t{0}", cuda.CurrentDevice.ComputeCapability.Minor);
            Console.WriteLine("\tTotal amount of global memory:\t\t\t{0} bytes", cuda.CurrentDevice.TotalMemory);

            Console.WriteLine("\tNumber of multiprocessors:\t\t\t{0}", cuda.GetDeviceAttribute(GASS.CUDA.CUDeviceAttribute.MultiProcessorCount));
            Console.WriteLine("\tNumber of cores:\t\t\t\t{0}\n", 8 * cuda.GetDeviceAttribute(GASS.CUDA.CUDeviceAttribute.MultiProcessorCount));

            Console.WriteLine("\tTotal amount of constant memory:\t\t{0}", cuda.CurrentDevice.Properties.TotalConstantMemory);
            Console.WriteLine("\tTotal amount of shared memory per block:\t{0}", cuda.CurrentDevice.Properties.SharedMemoryPerBlock);
            Console.WriteLine("\tTotal number of registers available per block:\t{0}", cuda.CurrentDevice.Properties.RegistersPerBlock);
            Console.WriteLine("\tWarp size:\t\t\t\t\t{0}", cuda.GetDeviceAttribute(GASS.CUDA.CUDeviceAttribute.WarpSize));
            Console.WriteLine("\tMaximum number of threads per block:\t\t{0}", cuda.GetDeviceAttribute(GASS.CUDA.CUDeviceAttribute.MaxThreadsPerBlock));
            Console.WriteLine("\tMaximum sizes of each dimension of a block:\t{0} x {1} x {2}", cuda.GetDeviceAttribute(GASS.CUDA.CUDeviceAttribute.MaxBlockDimX), cuda.GetDeviceAttribute(GASS.CUDA.CUDeviceAttribute.MaxBlockDimY), cuda.GetDeviceAttribute(GASS.CUDA.CUDeviceAttribute.MaxBlockDimZ));
            Console.WriteLine("\tMaximum sizes of each dimension of a grid:\t{0} x {1} x {2}", cuda.GetDeviceAttribute(GASS.CUDA.CUDeviceAttribute.MaxGridDimX), cuda.GetDeviceAttribute(GASS.CUDA.CUDeviceAttribute.MaxGridDimY), cuda.GetDeviceAttribute(GASS.CUDA.CUDeviceAttribute.MaxGridDimZ));
            Console.WriteLine("\tMaximum memory pitch:\t\t\t\t{0}", cuda.CurrentDevice.Properties.MemoryPitch);
            Console.WriteLine("\tTexture alignment:\t\t\t\t{0}", cuda.GetDeviceAttribute(GASS.CUDA.CUDeviceAttribute.TextureAlignment));
            Console.WriteLine("\tClock rate:\t\t\t\t\t{0} GHz\n", cuda.GetDeviceAttribute(GASS.CUDA.CUDeviceAttribute.ClockRate) * 1e-6f);
            Console.WriteLine("\tConcurrent copy and execution:\t\t\t{0}", cuda.GetDeviceAttribute(GASS.CUDA.CUDeviceAttribute.ConcurrentKernels) == 1 ? "Yes" : "No");
            Console.WriteLine("\tRun time limit on kernels:\t\t\t{0}", cuda.GetDeviceAttribute(GASS.CUDA.CUDeviceAttribute.KernelExecTimeout) == 1 ? "Yes" : "No");
            Console.WriteLine("\tIntegrated:\t\t\t\t\t{0}", cuda.GetDeviceAttribute(GASS.CUDA.CUDeviceAttribute.Integrated) == 1 ? "Yes" : "No");
            Console.WriteLine("\tSupport host page-locked memory mapping:\t{0}", cuda.GetDeviceAttribute(GASS.CUDA.CUDeviceAttribute.CanMapHostMemory) == 1 ? "Yes" : "No");
        }

        public override void Dispose()
        {
        }
    }
}
