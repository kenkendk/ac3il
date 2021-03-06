﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccCIL;

namespace CUDAJIT
{
    class CUDAJITCompiler : IJITCompiler
    {
        /// <summary>
        /// The list of all mappped CIL to SPE translations
        /// </summary>
        private static readonly Dictionary<Mono.Cecil.Cil.Code, System.Reflection.MethodInfo> _opTranslations;

        /// <summary>
        /// The list of active optimizers
        /// </summary>
        private List<IOptimizer> m_optimizers = new List<IOptimizer>();

        /// <summary>
        /// Static initializer for building instruction table based on reflection
        /// </summary>
        static CUDAJITCompiler()
        {
            _opTranslations = BuildTranslationTable();
        }

        /// <summary>
        /// Gets the list of active optimizers
        /// </summary>
        public IList<IOptimizer> Optimizers
        {
            get { return m_optimizers; }
        }

        /// <summary>
        /// Gets or sets the current optimization level
        /// </summary>
        public OptimizationLevel OptimizationLevel { get; set; }

        private static Dictionary<Mono.Cecil.Cil.Code, System.Reflection.MethodInfo> BuildTranslationTable()
        {
            Dictionary<Mono.Cecil.Cil.Code, System.Reflection.MethodInfo> res = new Dictionary<Mono.Cecil.Cil.Code, System.Reflection.MethodInfo>();

            Mono.Cecil.Cil.Code v;
            foreach (System.Reflection.MethodInfo mi in typeof(CUDAOpCodeMapper).GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
                if (Enum.TryParse<Mono.Cecil.Cil.Code>(mi.Name, true, out v))
                    res[v] = mi;

            return res;
        }


        public ICompiledMethod JIT(AccCIL.IR.MethodEntry method)
        {
            throw new NotImplementedException();
        }
    }
}
