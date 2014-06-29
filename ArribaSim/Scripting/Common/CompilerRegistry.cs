/*

ArribaSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using ArribaSim.Scene.Types.Script;
using System;
using ThreadedClasses;

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
