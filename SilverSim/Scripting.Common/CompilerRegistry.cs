/*

SilverSim is distributed under the terms of the
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

using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using ThreadedClasses;

namespace SilverSim.Scripting.Common
{
    public static class CompilerRegistry
    {
        public class RegistryImpl : IScriptCompilerRegistry
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

            private IScriptAssembly Compile(UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1)
            {
                string language = DefaultCompilerName;
                bool useDefault = true;
                int lineno = 0;
                foreach (KeyValuePair<int, string> shbang in shbangs)
                {
                    if (shbang.Value.StartsWith("//#!Engine:"))
                    {
                        /* we got a sh-bang here, it is a lot safer than what OpenSimulator uses */
                        language = shbang.Value.Substring(11).Trim().ToUpper();
                        useDefault = false;
                        lineno = shbang.Key;
                    }
                }

                if(useDefault)
                {
                    shbangs.Add(-1, string.Format("//#!Engine:{0}", language));
                }

                IScriptCompiler compiler;
                try
                {
                    compiler = this[language];
                }
                catch
                {
                    throw new CompilerException(lineno, "Unknown engine specified");
                }

                object[] attrs = compiler.GetType().GetCustomAttributes(typeof(CompilerUsesRunAndCollectMode), false);
                if(attrs.Length != 0)
                {
                    return compiler.Compile(AppDomain.CurrentDomain, user, shbangs, assetID, reader, linenumber);
                }
                else
                {
                    AppDomain appDom = AppDomain.CreateDomain(
                        "Script Domain " + assetID, 
                        AppDomain.CurrentDomain.Evidence);
                    try
                    {
                        IScriptAssembly assembly = compiler.Compile(appDom, user, shbangs, assetID, reader, linenumber);
                        ScriptLoader.RegisterAppDomain(assetID, appDom);
                        return assembly;
                    }
                    catch
                    {
                        AppDomain.Unload(appDom);
                        throw;
                    }
                }
            }

            private void SyntaxCheck(UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1)
            {
                string language = DefaultCompilerName;
                bool useDefault = true;
                int lineno = 0;
                foreach (KeyValuePair<int, string> shbang in shbangs)
                {
                    if (shbang.Value.StartsWith("//#!Engine:"))
                    {
                        /* we got a sh-bang here, it is a lot safer than what OpenSimulator uses */
                        language = shbang.Value.Substring(11).Trim().ToUpper();
                        useDefault = false;
                        lineno = shbang.Key;
                    }
                }

                if (useDefault)
                {
                    shbangs.Add(-1, string.Format("//#!Engine:{0}", language));
                }

                IScriptCompiler compiler;
                try
                {
                    compiler = this[language];
                }
                catch
                {
                    throw new CompilerException(lineno, "Unknown engine specified");
                }
                compiler.SyntaxCheck(user, shbangs, assetID, reader, linenumber);
            }

            public IScriptAssembly Compile(AppDomain appDom, UUI user, UUID assetID, TextReader reader)
            {
                int linenumber = 1;
                Dictionary<int, string> shbangs = new Dictionary<int, string>();
                while (reader.Peek() == '/')
                {
                    string shbang = reader.ReadLine();
                    if (shbang.StartsWith("//#!"))
                    {
                        shbangs.Add(linenumber, shbang);
                    }
                    ++linenumber;
                }

                return Compile(user, shbangs, assetID, reader, linenumber);
            }

            public void SyntaxCheck(UUI user, UUID assetID, TextReader reader)
            {
                int linenumber = 1;
                Dictionary<int, string> shbangs = new Dictionary<int, string>();
                while(reader.Peek() == '/')
                {
                    string shbang = reader.ReadLine();
                    if(shbang.StartsWith("//#!"))
                    {
                        shbangs.Add(linenumber, shbang);
                    }
                    ++linenumber;
                }
                SyntaxCheck(user, shbangs, assetID, reader, linenumber);
            }
        }

        public static RegistryImpl ScriptCompilers = new RegistryImpl();
    }
}
