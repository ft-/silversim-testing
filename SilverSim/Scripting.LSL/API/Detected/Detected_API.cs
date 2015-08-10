// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.LSL.API.Detected
{
    [ScriptApiName("Detected")]
    [LSLImplementation]
    public partial class Detected_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        [APILevel(APIFlags.LSL)]
        public const int TOUCH_INVALID_FACE = -1;
        [APILevel(APIFlags.LSL)]
        public static readonly Vector3 TOUCH_INVALID_TEXCOORD = new Vector3(-1.0, -1.0, 0.0);
        [APILevel(APIFlags.LSL)]
        public static readonly Vector3 TOUCH_INVALID_VECTOR = Vector3.Zero;

        public Detected_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }
    }
}
