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

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using System;

namespace SilverSim.Scripting.LSL.API.Parcel
{
    [ScriptApiName("Parcel")]
    public partial class Parcel_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        ObjectPart Part;
        ObjectPartInventoryItem ScriptItem;
        ScriptInstance Instance;

        public void Initialize(ScriptInstance instance, ObjectPart part, ObjectPartInventoryItem scriptItem)
        {
            Part = part;
            ScriptItem = scriptItem;
            Instance = instance;
        }

        public void Startup(ConfigurationLoader loader)
        {

        }

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
    }
}
