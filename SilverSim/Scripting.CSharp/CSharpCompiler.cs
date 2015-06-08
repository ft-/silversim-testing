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

using log4net;
using Microsoft.CSharp;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Types;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ThreadedClasses;

namespace SilverSim.Scripting.CSharp
{
    public class CSharpCompiler : IScriptCompiler, IPlugin
    {
        private static readonly ILog m_Log = LogManager.GetLogger("C# COMPILER");
        string m_CacheDirectory;
        bool m_IncludeDebugInformation;
        RwLockedDictionaryAutoAdd<UUID, object> m_CompileLockTokens = new RwLockedDictionaryAutoAdd<UUID, object>(delegate() { return new object(); });
        ServerParamServiceInterface m_ServerParamStorage;

        public CSharpCompiler(IConfig configSection)
        {
            m_CacheDirectory = configSection.GetString("CacheDirectory", "../data/CompilerCache");
            m_IncludeDebugInformation = configSection.GetBoolean("IncludeDebugInformation", false);
        }

        public void Startup(ConfigurationLoader loader)
        {
            CompilerRegistry.ScriptCompilers["cs"] = this;
            m_ServerParamStorage = loader.GetServerParamStorage();
        }

        public void CheckUser(UUI user)
        {
            if(!user.IsAuthoritative)
            {
                throw new CompilerException(1, "This region does not have authoritative data about your identity.");
            }

            string s = m_ServerParamStorage.GetString(null, "AllowedCSharpUsers", "");
            string[] tokens = s.Split(',');
            foreach(string v in tokens)
            {
                UUID uuid;
                if(UUID.TryParse(v, out uuid))
                {
                    if(user.HomeURI == null && uuid == user.ID)
                    {
                        return;
                    }
                }
                else if(user.HomeURI != null)
                {
                    if(v == user.FullName)
                    {
                        return;
                    }
                }
            }
            throw new CompilerException(1, "This region does not allow using C# with your avatar.");
        }

        public string CompileAssembly(UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber)
        {
            string assemblyFile = Path.Combine(m_CacheDirectory, "csharp_" + assetID + ".dll");
            CompilerParameters parameters = new CompilerParameters();
            parameters.GenerateExecutable = false;
            parameters.OutputAssembly = assemblyFile;
            parameters.IncludeDebugInformation = m_IncludeDebugInformation;
            parameters.TreatWarningsAsErrors = false;

            string rootPath = AppDomain.CurrentDomain.BaseDirectory;
            foreach (AssemblyName an in MethodBase.GetCurrentMethod().DeclaringType.Assembly.GetReferencedAssemblies())
            {
                parameters.ReferencedAssemblies.Add(Path.Combine(rootPath, an.Name + ".dll"));
            }

            if (!File.Exists(assemblyFile))
            {
                /* we do micro loccking here as well , we lock out only the compiler for the same compilation unit */
                lock (m_CompileLockTokens[assetID])
                {
                    CSharpCodeProvider csCodeProvider = new CSharpCodeProvider();

                    CompilerResults results = csCodeProvider.CompileAssemblyFromSource(
                            parameters, reader.ReadToEnd());

                    m_CompileLockTokens.Remove(assetID);

                    if (results.Errors.Count > 0)
                    {
                        Dictionary<int, string> messages = new Dictionary<int, string>();
                        foreach (CompilerError err in results.Errors)
                        {
                            int actLine = err.Line + linenumber - 1;
                            string s;
                            if (err.IsWarning)
                            {

                            }
                            else if (messages.TryGetValue(actLine, out s))
                            {
                                messages[actLine] = s + "\n" + err.ErrorText;
                            }
                            else
                            {
                                messages[actLine] = err.ErrorText;
                            }
                        }
                        if (messages.Count != 0)
                        {
                            throw new CompilerException(messages);
                        }
                    }
                }
            }
            return assemblyFile;
        }

        public IScriptAssembly Compile(AppDomain appDom, UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1)
        {
            Assembly assembly = appDom.Load(CompileAssembly(user, shbangs, assetID, reader, linenumber));
            Type scriptType = null;
            foreach(Type t in assembly.GetTypes())
            {
                if(typeof(ScriptInstance).IsAssignableFrom(t))
                {
                    scriptType = t;
                    break;
                }
            }

            if(scriptType == null)
            {
                throw new CompilerException(1, "C# script does not contain a class derived from ScriptInstance");
            }
            return new CSharpScriptAssembly(assembly, scriptType);
        }

        public void SyntaxCheck(UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1)
        {
            CompileAssembly(user, shbangs, assetID, reader, linenumber);
        }

        public IScriptState StateFromXml(System.Xml.XmlTextReader reader, Dictionary<string, string> attrs, ObjectPartInventoryItem item)
        {
            throw new NotImplementedException();
        }

        /* testing only function for certain compilers */
        public void SyntaxCheckAndDump(Stream s, UUI user, Dictionary<int, string> shbangs, UUID assetID, TextReader reader, int linenumber = 1)
        {
            throw new NotImplementedException();
        }
    }
}
