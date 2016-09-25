// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace SilverSim.Scripting.Common
{
    public static class CompilerRegistry
    {
        [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public class RegistryImpl : IScriptCompilerRegistry
        {
            readonly RwLockedDictionary<string, IScriptCompiler> m_ScriptCompilers = new RwLockedDictionary<string, IScriptCompiler>();
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

            public IList<string> Names
            {
                get
                {
                    return new List<string>(m_ScriptCompilers.Keys);
                }
            }

            private IScriptCompiler DetermineShBangs(
                Dictionary<int, string> shbangs)
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
                return compiler;
            }

            private IScriptAssembly Compile(UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1)
            {
                IScriptCompiler compiler = DetermineShBangs(shbangs);

                object[] attrs = compiler.GetType().GetCustomAttributes(typeof(CompilerUsesRunAndCollectModeAttribute), false);
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
                IScriptCompiler compiler = DetermineShBangs(shbangs);

                compiler.SyntaxCheck(user, shbangs, assetID, reader, linenumber);
            }

            private void SyntaxCheckAndDump(Stream s, UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1)
            {
                IScriptCompiler compiler = DetermineShBangs(shbangs);

                compiler.SyntaxCheckAndDump(s, user, shbangs, assetID, reader, linenumber);
            }

            public class StreamReaderAddHead : TextReader
            {
                readonly TextReader m_InnerReader;
                string m_Header;
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
                public override int Peek()
                {
                    return m_Header.Length != 0 ?
                        (int)m_Header[0] :
                        m_InnerReader.Peek();
                }
                public override int Read()
                {
                    if (m_Header.Length != 0)
                    {
                        int c = (int)m_Header[0];
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

                public override string ReadToEnd()
                {
                    return m_Header + m_InnerReader.ReadToEnd();
                }
            }

            public IScriptAssembly Compile(AppDomain appDom, UUI user, UUID assetID, TextReader reader)
            {
                int linenumber = 1;
                Dictionary<int, string> shbangs = new Dictionary<int, string>();
                StringBuilder header = new StringBuilder();
                while (reader.Peek() == '/')
                {
                    string shbang = reader.ReadLine();
                    header.AppendLine(shbang);
                    if (shbang.StartsWith("//#!"))
                    {
                        shbangs.Add(linenumber, shbang);
                    }
                }

                using (StreamReaderAddHead headReader = new StreamReaderAddHead(header.ToString(), reader))
                {
                    return Compile(user, shbangs, assetID, headReader, linenumber);
                }
            }

            public void SyntaxCheck(UUI user, UUID assetID, TextReader reader)
            {
                int linenumber = 1;
                Dictionary<int, string> shbangs = new Dictionary<int, string>();
                StringBuilder header = new StringBuilder();
                while (reader.Peek() == '/')
                {
                    string shbang = reader.ReadLine();
                    header.AppendLine(shbang);
                    if (shbang.StartsWith("//#!"))
                    {
                        shbangs.Add(linenumber, shbang);
                    }
                }
                using (StreamReaderAddHead headReader = new StreamReaderAddHead(header.ToString(), reader))
                {
                    Compile(user, shbangs, assetID, headReader, linenumber);
                }
            }

            public void SyntaxCheckAndDump(Stream s, UUI user, UUID assetID, TextReader reader)
            {
                int linenumber = 1;
                Dictionary<int, string> shbangs = new Dictionary<int, string>();
                StringBuilder header = new StringBuilder();
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
                using (StreamReaderAddHead headReader = new StreamReaderAddHead(header.ToString(), reader))
                {
                    SyntaxCheckAndDump(s, user, shbangs, assetID, headReader, linenumber);
                }
            }

            public void CompileToDisk(string filename, UUI user, UUID assetID, TextReader reader)
            {
                int linenumber = 1;
                Dictionary<int, string> shbangs = new Dictionary<int, string>();
                StringBuilder header = new StringBuilder();
                while (reader.Peek() == '/')
                {
                    string shbang = reader.ReadLine();
                    header.AppendLine(shbang);
                    if (shbang.StartsWith("//#!"))
                    {
                        shbangs.Add(linenumber, shbang);
                    }
                }

                IScriptCompiler compiler = DetermineShBangs(shbangs);

                using (StreamReaderAddHead headReader = new StreamReaderAddHead(header.ToString(), reader))
                {
                    compiler.CompileToDisk(filename, AppDomain.CurrentDomain, user, shbangs, assetID, headReader, linenumber);
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Concurrency", "NonConstantStaticFieldsShouldNotBeVisibleRule")]
        public static readonly RegistryImpl ScriptCompilers = new RegistryImpl();
    }
}
