// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using ThreadedClasses;

namespace SilverSim.Scripting.Common
{
    public static class CompilerRegistry
    {
        [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
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
                        throw new ArgumentException("value");
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
                        "Script Domain " + assetID.ToString(), 
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

            private void SyntaxCheckAndDump(Stream s, UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1)
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
                compiler.SyntaxCheckAndDump(s, user, shbangs, assetID, reader, linenumber);
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

            public void SyntaxCheckAndDump(Stream s, UUI user, UUID assetID, TextReader reader)
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
                SyntaxCheckAndDump(s, user, shbangs, assetID, reader, linenumber);
            }

        }

        [SuppressMessage("Gendarme.Rules.Concurrency", "NonConstantStaticFieldsShouldNotBeVisibleRule")]
        public static RegistryImpl ScriptCompilers = new RegistryImpl();
    }
}
