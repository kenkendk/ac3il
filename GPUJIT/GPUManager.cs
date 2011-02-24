using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Mono.Cecil;

namespace PTXJIT
{
    public class GPUManager : AccCIL.AccelleratorBase
    {
        // Number of entries in vector
        static int N = 256;
        // Number of threads per block
        static int threadsPerBlock = 256;
        // Path to module (PTX or CUBIN) to load
        static string moduleName = "D:\\Dokumenter\\Visual Studio 2010\\Projects\\Sandkasse\\Sandkasse\\vector.ptx";
        // Name of function to run
        static string functionName = "VecAdd";

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

        protected override T DoAccelerate<T>(string assembly, string methodName, params object[] args)
        {
            throw new NotImplementedException();
        }

        public override void LoadProgram(IEnumerable<AccCIL.ICompiledMethod> methods)
        {
            throw new NotImplementedException();
        }

        public override object Execute(params object[] arguments)
        {
            // Compile Code

            // Create module (PTX)

            // Create cuda context
            GASS.CUDA.CUDA cuda = new GASS.CUDA.CUDA(true);

            // Load module (PTX) and get function
            GASS.CUDA.Types.CUmodule module = cuda.LoadModule(moduleName);
            GASS.CUDA.Types.CUfunction func = cuda.GetModuleFunction(module, functionName);

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

            // Check result
            for (int i = 0; i < N; i++)
                if (A[i] + B[i] != C[i])
                    throw new Exception("GPU failed to calculate the correct result!");

            return null;
        }
    }
}
