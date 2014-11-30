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

namespace SilverSim.Scripting.LSL.API.Region
{
    [ScriptApiName("Region")]
    public class Region_API_Factory : ScriptApiFactory
    {
        public Region_API_Factory()
            : base(typeof(Region_API))
        {

        }
    }

    [ScriptApiName("Region")]
    public partial class Region_API : MarshalByRefObject, IScriptApi
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
    }
}
