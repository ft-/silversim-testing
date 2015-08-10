// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.Tests.Extensions;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace SilverSim.Tests.Scripting
{
    public class SyntaxCheck : ITest
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        Dictionary<UUID, string> Files = new Dictionary<UUID, string>();
        bool WriteParserResult = false;

        public void Startup(ConfigurationLoader loader)
        {
            IConfig config = loader.Config.Configs[GetType().FullName];
            foreach(string key in config.GetKeys())
            {
                UUID uuid;
                if (UUID.TryParse(key, out uuid))
                {
                    Files[uuid] = config.GetString(key);
                }
            }
            CompilerRegistry.ScriptCompilers.DefaultCompilerName = config.GetString("DefaultCompiler");
            WriteParserResult = config.GetBoolean("WriteParserResult", false);
        }

        public bool Run()
        {
            bool success = true;
            int count = 0;
            int successcnt = 0;
            foreach (KeyValuePair<UUID, string> file in Files)
            {
                ++count;
                m_Log.InfoFormat("Testing syntax of {1} ({0})", file.Key, file.Value);
                try
                {
                    using(TextReader reader = new StreamReader(file.Value))
                    {
                        if (WriteParserResult)
                        {
                            using (FileStream st = new FileStream(file.Value + ".dump.txt", FileMode.Create))
                            {
                                CompilerRegistry.ScriptCompilers.SyntaxCheckAndDump(st, UUI.Unknown, file.Key, reader);
                            }
                        }
                        else
                        {
                            CompilerRegistry.ScriptCompilers.SyntaxCheck(UUI.Unknown, file.Key, reader);
                        }
                    }
                    m_Log.InfoFormat("Syntax of {1} ({0}) parsed successfully", file.Key, file.Value);
                    ++successcnt;
                }
                catch (CompilerException e)
                {
                    m_Log.ErrorFormat("Syntax of {1} ({0}) failed to parse: {2}", file.Key, file.Value, e.Message);
                    m_Log.WarnFormat("Stack Trace:\n{0}", e.StackTrace.ToString());
                    success = false;
                }
                catch (Exception e)
                {
                    m_Log.ErrorFormat("Syntax of {1} ({0}) failed to parse: {2}", file.Key, file.Value, e.Message);
                    m_Log.WarnFormat("Stack Trace:\n{0}", e.StackTrace.ToString());
                    success = false;
                }
            }

            m_Log.InfoFormat("{0} of {1} compilations successful", successcnt, count);
            return success;
        }
    }
}
