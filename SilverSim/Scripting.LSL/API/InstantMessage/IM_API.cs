// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using System;

namespace SilverSim.Scripting.LSL.APIs.IM
{
    [ScriptApiName("InstantMessage")]
    [LSLImplementation]
    public partial class IM_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        public IM_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }
    }
}
