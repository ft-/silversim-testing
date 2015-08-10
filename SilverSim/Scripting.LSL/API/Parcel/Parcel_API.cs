// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using System;

namespace SilverSim.Scripting.LSL.API.Parcel
{
    [ScriptApiName("Parcel")]
    [LSLImplementation]
    public partial class Parcel_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_FLAG_ALLOW_FLY = 0x1;                           
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_FLAG_ALLOW_SCRIPTS = 0x2;                       
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_FLAG_ALLOW_LANDMARK = 0x8;                      
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_FLAG_ALLOW_TERRAFORM = 0x10;                    
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_FLAG_ALLOW_DAMAGE = 0x20;                       
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_FLAG_ALLOW_CREATE_OBJECTS = 0x40;               
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_FLAG_USE_ACCESS_GROUP = 0x100;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_FLAG_USE_ACCESS_LIST = 0x200;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_FLAG_USE_BAN_LIST = 0x400;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_FLAG_USE_LAND_PASS_LIST = 0x800;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_FLAG_LOCAL_SOUND_ONLY = 0x8000;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_FLAG_RESTRICT_PUSHOBJECT = 0x200000;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_FLAG_ALLOW_GROUP_SCRIPTS = 0x2000000;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_FLAG_ALLOW_CREATE_GROUP_OBJECTS = 0x4000000;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_FLAG_ALLOW_ALL_OBJECT_ENTRY = 0x8000000;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_FLAG_ALLOW_GROUP_OBJECT_ENTRY = 0x10000000;

        public Parcel_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }
    }
}
