#ifndef _MATRIXMUL_KERNEL_H_
#define _MATRIXMUL_KERNEL_H_

#include <stdio.h>
#include <cuPrintf.cu>

//The macro CUPRINTF is defined for architectures
//with different compute capabilities.
#if __CUDA_ARCH__ < 200 	//Compute capability 1.x architectures
#define CUPRINTF cuPrintf
#else						//Compute capability 2.x architectures
#define CUPRINTF(fmt, ...) printf("[%d, %d]:\t" fmt, \
								blockIdx.y*gridDim.x+blockIdx.x,\
								threadIdx.z*blockDim.x*blockDim.y+threadIdx.y*blockDim.x+threadIdx.x,\
								__VA_ARGS__)
#endif

extern "C" __global__ void VecAdd(float* A, float* B, float* C){
	int i = threadIdx.x;
	C[i] = A[i] + B[i];

	CUPRINTF("Computed value is:%d\n", C[0]);
}

#endif // #ifndef _MATRIXMUL_KERNEL_H_