// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using System;

namespace SilverSim.Scripting.LSL.API.Region
{
    [ScriptApiName("Region")]
    [LSLImplementation]
    public partial class Region_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        [APILevel(APIFlags.LSL)]
        public const int REGION_FLAG_ALLOW_DAMAGE = 0x1;
        [APILevel(APIFlags.LSL)]
        public const int REGION_FLAG_FIXED_SUN = 0x10;
        [APILevel(APIFlags.LSL)]
        public const int REGION_FLAG_BLOCK_TERRAFORM = 0x40;
        [APILevel(APIFlags.LSL)]
        public const int REGION_FLAG_SANDBOX = 0x100;
        [APILevel(APIFlags.LSL)]
        public const int REGION_FLAG_DISABLE_COLLISIONS = 0x1000;
        [APILevel(APIFlags.LSL)]
        public const int REGION_FLAG_DISABLE_PHYSICS = 0x4000;
        [APILevel(APIFlags.LSL)]
        public const int REGION_FLAG_BLOCK_FLY = 0x80000;
        [APILevel(APIFlags.LSL)]
        public const int REGION_FLAG_ALLOW_DIRECT_TELEPORT = 0x100000;
        [APILevel(APIFlags.LSL)]
        public const int REGION_FLAG_RESTRICT_PUSHOBJECT = 0x400000;   
      
        public Region_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }
    }
}
