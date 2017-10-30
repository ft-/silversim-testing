// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Scene.Types.Script;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Updater;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace SilverSim.Scripting.Common
{
    public static class CompilerRegistry
    {
        public sealed class RegistryImpl : IScriptCompilerRegistry
        {
            private readonly RwLockedDictionary<string, IScriptCompiler> m_ScriptCompilers = new RwLockedDictionary<string, IScriptCompiler>();
            public string DefaultCompilerName { get; set; }

            public RegistryImpl()
            {
                DefaultCompilerName = "lsl";
            }

            public IScriptCompiler this[string name]
            {
                get
                {
                    return string.IsNullOrEmpty(name) ?
                        m_ScriptCompilers[DefaultCompilerName] :
                        m_ScriptCompilers[name];
                }
                set
                {
                    if (string.IsNullOrEmpty(name))
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

            public IList<string> Names => new List<string>(m_ScriptCompilers.Keys);

            private IScriptCompiler DetermineShBangs(
                Dictionary<int, string> shbangs,
                CultureInfo currentCulture)
            {
                var language = DefaultCompilerName;
                var useDefault = true;
                int lineno = 0;
                foreach (var shbang in shbangs)
                {
                    if (shbang.Value.StartsWith("//#!Engine:"))
                    {
                        /* we got a sh-bang here, it is a lot safer than what OpenSimulator uses */
                        language = shbang.Value.Substring(11).Trim().ToLower();
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
                    throw new CompilerException(lineno, string.Format(this.GetLanguageString(currentCulture, "UnknownEngine0Specified", "Unknown engine '{0}' specified"), language));
                }
                return compiler;
            }

            private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
            {
                var aName = new AssemblyName(args.Name);

                foreach (var s in new string[] { CoreUpdater.Instance.BinariesPath, CoreUpdater.Instance.PluginsPath })
                {
                    var assemblyName = Path.Combine(s, aName.Name + ".dll");
                    if (File.Exists(assemblyName))
                    {
                        return Assembly.LoadFile(assemblyName);
                    }
                }
                return null;
            }

            private IScriptAssembly Compile(UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1, CultureInfo cultureInfo = null)
            {
                var compiler = DetermineShBangs(shbangs, cultureInfo);

                var attrs = compiler.GetType().GetCustomAttributes(typeof(CompilerUsesRunAndCollectModeAttribute), false);
                var attrs2 = compiler.GetType().GetCustomAttributes(typeof(CompilerUsesInMemoryCompilationAttribute), false);
                if(attrs.Length != 0 || attrs2.Length != 0)
                {
                    return compiler.Compile(AppDomain.CurrentDomain, user, shbangs, assetID, reader, linenumber, cultureInfo);
                }
                else
                {
                    var appDom = AppDomain.CreateDomain(
                        "Script Domain " + assetID.ToString(),
                        AppDomain.CurrentDomain.Evidence);
                    appDom.AssemblyResolve += ResolveAssembly;
                    appDom.Load("SilverSim.Types");
                    appDom.Load("SilverSim.ServiceInterfaces");
                    appDom.Load("SilverSim.Scene.Types");
                    appDom.Load("SilverSim.Scripting.Common");
                    try
                    {
                        var assembly = compiler.Compile(appDom, user, shbangs, assetID, reader, linenumber, cultureInfo);
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

            private void SyntaxCheck(UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1, CultureInfo cultureInfo = null)
            {
                var compiler = DetermineShBangs(shbangs, cultureInfo);

                compiler.SyntaxCheck(user, shbangs, assetID, reader, linenumber, cultureInfo);
            }

            private void SyntaxCheckAndDump(Stream s, UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1, CultureInfo cultureInfo = null)
            {
                var compiler = DetermineShBangs(shbangs, cultureInfo);

                compiler.SyntaxCheckAndDump(s, user, shbangs, assetID, reader, linenumber, cultureInfo);
            }

            public class StreamReaderAddHead : TextReader
            {
                private readonly TextReader m_InnerReader;
                private string m_Header;
                public StreamReaderAddHead(string header, TextReader reader)
                {
                    m_Header = header;
                    m_InnerReader = reader;
                }

                public override void Close()
                {
                    m_InnerReader.Close();
                }

                protected override void Dispose(bool disposing)
                {
                    if(disposing)
                    {
                        m_InnerReader.Dispose();
                    }
                }

                public override int Peek() => m_Header.Length != 0 ?
                        m_Header[0] :
                        m_InnerReader.Peek();

                public override int Read()
                {
                    if (m_Header.Length != 0)
                    {
                        int c = m_Header[0];
                        m_Header = m_Header.Substring(1);
                        return c;
                    }
                    else
                    {
                        return m_InnerReader.Read();
                    }
                }

                public override int Read(char[] buffer, int index, int count)
                {
                    int n = 0;
                    while(count-- > 0)
                    {
                        int c = Read();
                        if(c < 0)
                        {
                            break;
                        }
                        buffer[index++] = (char)c;
                        ++n;
                    }
                    return n;
                }

                public override int ReadBlock(char[] buffer, int index, int count)
                {
                    int n = 0;
                    while (count-- > 0)
                    {
                        int c = Read();
                        if (c < 0)
                        {
                            break;
                        }
                        buffer[index++] = (char)c;
                        ++n;
                    }
                    return n;
                }

                public override string ReadLine()
                {
                    if(m_Header.Length > 0)
                    {
                        string res = string.Empty;
                        int pos = m_Header.IndexOf('\n');
                        if(pos >= 0)
                        {
                            res = m_Header.Substring(0, pos + 1);
                            m_Header = m_Header.Substring(pos + 1);
                            return res;
                        }
                        else
                        {
                            res = m_Header;
                            m_Header = string.Empty;
                        }
                        return res + m_InnerReader.ReadLine();
                    }
                    else
                    {
                        return m_InnerReader.ReadLine();
                    }
                }

                public override string ReadToEnd() => m_Header + m_InnerReader.ReadToEnd();
            }

            public IScriptAssembly Compile(AppDomain appDom, UUI user, UUID assetID, TextReader reader, CultureInfo cultureInfo = null)
            {
                int linenumber = 1;
                var shbangs = new Dictionary<int, string>();
                var header = new StringBuilder();
                while (reader.Peek() == '/')
                {
                    var shbang = reader.ReadLine();
                    header.AppendLine(shbang);
                    if (shbang.StartsWith("//#!"))
                    {
                        shbangs.Add(linenumber, shbang);
                    }
                    ++linenumber;
                }

                using (var headReader = new StreamReaderAddHead(header.ToString(), reader))
                {
                    return Compile(user, shbangs, assetID, headReader, 1, cultureInfo);
                }
            }

            public void SyntaxCheck(UUI user, UUID assetID, TextReader reader, CultureInfo cultureInfo = null)
            {
                int linenumber = 1;
                var shbangs = new Dictionary<int, string>();
                var header = new StringBuilder();
                while (reader.Peek() == '/')
                {
                    var shbang = reader.ReadLine();
                    header.AppendLine(shbang);
                    if (shbang.StartsWith("//#!"))
                    {
                        shbangs.Add(linenumber, shbang);
                    }
                    ++linenumber;
                }

                using (var headReader = new StreamReaderAddHead(header.ToString(), reader))
                {
                    Compile(user, shbangs, assetID, headReader, 1, cultureInfo);
                }
            }

            public void SyntaxCheckAndDump(Stream s, UUI user, UUID assetID, TextReader reader, CultureInfo cultureInfo = null)
            {
                int linenumber = 1;
                var shbangs = new Dictionary<int, string>();
                var header = new StringBuilder();
                while (reader.Peek() == '/')
                {
                    string shbang = reader.ReadLine();
                    header.AppendLine(shbang);
                    if (shbang.StartsWith("//#!"))
                    {
                        shbangs.Add(linenumber, shbang);
                    }
                    ++linenumber;
                }
                using (var headReader = new StreamReaderAddHead(header.ToString(), reader))
                {
                    SyntaxCheckAndDump(s, user, shbangs, assetID, headReader, 1, cultureInfo);
                }
            }

            public void CompileToDisk(string filename, UUI user, UUID assetID, TextReader reader, CultureInfo cultureInfo = null)
            {
                int linenumber = 1;
                var shbangs = new Dictionary<int, string>();
                var header = new StringBuilder();
                while (reader.Peek() == '/')
                {
                    var shbang = reader.ReadLine();
                    header.AppendLine(shbang);
                    if (shbang.StartsWith("//#!"))
                    {
                        shbangs.Add(linenumber, shbang);
                    }
                    ++linenumber;
                }

                var compiler = DetermineShBangs(shbangs, cultureInfo);

                using (var headReader = new StreamReaderAddHead(header.ToString(), reader))
                {
                    compiler.CompileToDisk(filename, AppDomain.CurrentDomain, user, shbangs, assetID, headReader, 1, cultureInfo);
                }
            }
        }

        public static readonly RegistryImpl ScriptCompilers = new RegistryImpl();
    }
}
