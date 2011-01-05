using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccCIL
{
    public abstract class AccelleratorBase : IAccellerator
    {
        protected abstract T DoAccelerate<T>(string assembly, string methodName, params object[] args);

        /// <summary>
        /// Internal marker class for returntype &quot;void&quot;
        /// </summary>
        protected class ReturnTypeVoid { }

        #region ISPEManager Members

        public abstract void LoadProgram(IEnumerable<ICompiledMethod> methods);

        public abstract object Execute(params object[] arguments);

        public OutType Accelerate<OutType>(Func<OutType> func)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, typeof(OutType));
        }

        public OutType Accelerate<InType, OutType>(Func<InType, OutType> func, InType arg)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, arg);
        }

        public OutType Accelerate<InType1, InType2, OutType>(Func<InType1, InType2, OutType> func, InType1 arg1, InType2 arg2)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, arg1, arg2);
        }

        public OutType Accelerate<InType1, InType2, InType3, OutType>(Func<InType1, InType2, InType3, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, arg1, arg2, arg3);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, OutType>(Func<InType1, InType2, InType3, InType4, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, arg1, arg2, arg3, arg4);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, OutType>(Func<InType1, InType2, InType3, InType4, InType5, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, arg1, arg2, arg3, arg4, arg5);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14, InType15 arg15)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
        }

        public OutType Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, InType16, OutType>(Func<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, InType16, OutType> func, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14, InType15 arg15, InType16 arg16)
        {
            return DoAccelerate<OutType>(func.Method.Module.Assembly.ManifestModule.Name, func.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
        }

        public void Accelerate(Action action)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name);
        }
        public void Accelerate<InType1>(Action<InType1> action, InType1 arg1)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, arg1);
        }

        public void Accelerate<InType1, InType2>(Action<InType1, InType2> action, InType1 arg1, InType2 arg2)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, arg1, arg2);
        }

        public void Accelerate<InType1, InType2, InType3>(Action<InType1, InType2, InType3> action, InType1 arg1, InType2 arg2, InType3 arg3)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, arg1, arg2, arg3);
        }

        public void Accelerate<InType1, InType2, InType3, InType4>(Action<InType1, InType2, InType3, InType4> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, arg1, arg2, arg3, arg4);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5>(Action<InType1, InType2, InType3, InType4, InType5> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, arg1, arg2, arg3, arg4, arg5);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6>(Action<InType1, InType2, InType3, InType4, InType5, InType6> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14, InType15 arg15)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
        }

        public void Accelerate<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, InType16>(Action<InType1, InType2, InType3, InType4, InType5, InType6, InType7, InType8, InType9, InType10, InType11, InType12, InType13, InType14, InType15, InType16> action, InType1 arg1, InType2 arg2, InType3 arg3, InType4 arg4, InType5 arg5, InType6 arg6, InType7 arg7, InType8 arg8, InType9 arg9, InType10 arg10, InType11 arg11, InType12 arg12, InType13 arg13, InType14 arg14, InType15 arg15, InType16 arg16)
        {
            DoAccelerate<ReturnTypeVoid>(action.Method.Module.Assembly.ManifestModule.Name, action.Method.Name, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
        }
        #endregion
    }
}
