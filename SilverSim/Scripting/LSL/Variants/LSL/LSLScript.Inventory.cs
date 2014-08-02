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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using System;

namespace SilverSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public void llGiveInventory(UUID destination, string inventory)
        {
        }

        public void llGiveInventoryList(UUID target, string folder, AnArray inventory)
        {

        }

        public void llRemoveInventory(string item)
        {
            ObjectPartInventoryItem resitem;
            if (Part.Inventory.TryGetValue(item, out resitem))
            {
                IScriptInstance si = resitem.ScriptInstance;

                if (si != null)
                {
                    si.Remove();
                }
                Part.Inventory.Remove(resitem.ID);
            }
        }

        public UUID llGetInventoryCreator(string item)
        {
            try
            {
                return Part.Inventory[item].Creator.ID;
            }
            catch
            {
                return UUID.Zero;
            }
        }

        public UUID llGetInventoryKey(string item)
        {
            try
            {
                return Part.Inventory[item].ID;
            }
            catch
            {
                return UUID.Zero;
            }
        }

        public string llGetInventoryName(int type, int number)
        {
            return string.Empty;
        }

        public int llGetInventoryNumber(int type)
        {
            if (type == -1)
            {
                return Part.Inventory.Count;
            }
            return Part.Inventory.CountType((Types.Inventory.InventoryType)type);
        }

        public int llGetInventoryPermMask(string item, int category)
        {
            return 0;
        }

        public int llGetInventoryType(string name)
        {
            try
            {
                return (int)Part.Inventory[name.ToString()].InventoryType;
            }
            catch
            {
                return -1;
            }
        }
    }
}
