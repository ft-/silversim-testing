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
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.LSL.API.Sound
{
    [ScriptApiName("Sound")]
    public class Sound_API_Factory : ScriptApiFactory
    {
        public Sound_API_Factory()
            : base(typeof(Sound_API))
        {

        }
    }

    [ScriptApiName("Sound")]
    public partial class Sound_API : MarshalByRefObject, IScriptApi
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

        public UUID getSoundAssetID(string item)
        {
            UUID assetID;
            if (!UUID.TryParse(item, out assetID))
            {
                /* must be an inventory item */
                lock (this)
                {
                    ObjectPartInventoryItem i = Part.Inventory[item];
                    if (i.InventoryType != Types.Inventory.InventoryType.Sound)
                    {
                        throw new InvalidOperationException(string.Format("Inventory item {0} is not a sound", item));
                    }
                    assetID = i.AssetID;
                }
            }
            return assetID;
        }

    }
}
