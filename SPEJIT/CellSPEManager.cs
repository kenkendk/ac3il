using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPEJIT
{
    /// <summary>
    /// Implementation of an SPE controller for a physical SPE device
    /// </summary>
    class CellSPEManager : AccCIL.IAccellerator
    {
        #region ISPEManager Members

        public void LoadProgram(IEnumerable<AccCIL.ICompiledMethod> methods)
        {
            throw new NotImplementedException();
        }

        public object Execute(params object[] arguments)
        {
            throw new NotImplementedException();
        }

        public OutType Accelerate<OutType>(Func<OutType> func)
        {
            throw new NotImplementedException();
        }

        public OutType Accelerate<InType, OutType>(Func<InType, OutType> func, InType arg)
        {
            throw new NotImplementedException();
        }

        public OutType Accelerate<InType1, InType2, OutType>(Func<InType1, InType2, OutType> func, InType1 arg1, InType2 arg2)
        {
            throw new NotImplementedException();
        }

        public OutType Accelerate<InType1, InType2, InType3, OutType>(Func<InType1, InType2, InType3, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3)
        {
            throw new NotImplementedException();
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, OutType>(Func<InType1, InType2, InType3, InType4, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4)
        {
            throw new NotImplementedException();
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, OutType>(Func<InType1, InType2, InType3, InType4, InType5, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5)
        {
            throw new NotImplementedException();
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6)
        {
            throw new NotImplementedException();
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7)
        {
            throw new NotImplementedException();
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8)
        {
            throw new NotImplementedException();
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9)
        {
            throw new NotImplementedException();
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10)
        {
            throw new NotImplementedException();
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11)
        {
            throw new NotImplementedException();
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12)
        {
            throw new NotImplementedException();
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13)
        {
            throw new NotImplementedException();
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14)
        {
            throw new NotImplementedException();
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14, InType15 arg15)
        {
            throw new NotImplementedException();
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, InType16, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, InType16, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14, InType15 arg15, InType16 arg16)
        {
            throw new NotImplementedException();
        }

        public void Accelerate(Action action)
        {
            throw new NotImplementedException();
        }

        public void Accelerate<InType1>(Action<InType1> action, InType1 arg1)
        {
            throw new NotImplementedException();
        }

        public void Accelerate<InType1, InType2>(Action<InType1, InType2> action, InType1 arg1, InType2 arg2)
        {
            throw new NotImplementedException();
        }

        public void Accelerate<InType1, InType2, InType3>(Action<InType1, InType2, InType3> action, InType1 arg1, InType2 arg2, InType3 arg3)
        {
            throw new NotImplementedException();
        }

        public void Accelerate<InType1, InType2, InType3, InType4>(Action<InType1, InType2, InType3, InType4> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4)
        {
            throw new NotImplementedException();
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5>(Action<InType1, InType2, InType3, InType4, InType5> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5)
        {
            throw new NotImplementedException();
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6>(Action<InType1, InType2, InType3, InType4, InType5, InType6> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6)
        {
            throw new NotImplementedException();
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7)
        {
            throw new NotImplementedException();
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8)
        {
            throw new NotImplementedException();
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9)
        {
            throw new NotImplementedException();
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10)
        {
            throw new NotImplementedException();
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11)
        {
            throw new NotImplementedException();
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12)
        {
            throw new NotImplementedException();
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13)
        {
            throw new NotImplementedException();
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14)
        {
            throw new NotImplementedException();
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14, InType15 arg15)
        {
            throw new NotImplementedException();
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, InType16>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, InType16> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14, InType15 arg15, InType16 arg16)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
