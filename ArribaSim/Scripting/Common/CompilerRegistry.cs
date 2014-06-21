using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreadedClasses;
using ArribaSim.Scene.Types.Script;

namespace ArribaSim.Scripting.Common
{
    public static class CompilerRegistry
    {
        public class RegistryImpl
        {
            private RwLockedDictionary<string, IScriptCompiler> m_ScriptCompilers = new RwLockedDictionary<string, IScriptCompiler>();
            public string DefaultCompilerName { get; set; }
            public RegistryImpl()
            {

            }

            public IScriptCompiler this[string name]
            {
                get
                {
                    if (String.IsNullOrEmpty(name))
                    {
                        return m_ScriptCompilers[DefaultCompilerName];
                    }
                    else
                    {
                        return m_ScriptCompilers[name];
                    }
                }
                set
                {
                    if (String.IsNullOrEmpty(name))
                    {
                        throw new ArgumentException();
                    }
                    if (value == null)
                    {
                        m_ScriptCompilers.Remove(name);
                    }
                    else
                    {
                        m_ScriptCompilers.Add(name, value);
                    }
                }
            }
        }

        public static RegistryImpl ScriptCompilers = new RegistryImpl();
    }
}
