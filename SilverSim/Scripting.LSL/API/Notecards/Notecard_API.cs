// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using System;

namespace SilverSim.Scripting.LSL.API.Notecards
{
    [ScriptApiName("Notecard")]
    [LSLImplementation]
    public partial class Notecard_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        public Notecard_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }
    }
}
