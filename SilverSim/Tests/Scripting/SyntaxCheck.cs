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

namespace SilverSim.Tests.Scripting
{
    public class SyntaxCheck : ITest
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        Dictionary<UUID, string> Files = new Dictionary<UUID, string>();

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
        }

        public bool Run()
        {
            bool success = true;
            foreach (KeyValuePair<UUID, string> file in Files)
            {
                m_Log.InfoFormat("Testing syntax of {1} ({0})", file.Key, file.Value);
                try
                {
                    using(TextReader reader = new StreamReader(file.Value))
                    {
                        CompilerRegistry.ScriptCompilers.SyntaxCheck(UUI.Unknown, file.Key, reader);
                    }
                    m_Log.InfoFormat("Syntax of {1} ({0}) parsed successfully", file.Key, file.Value);
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

            return success;
        }
    }
}
