/*

ArribaSim is distributed under the terms of the
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

using ArribaSim.Scene.Types.Object;
using ArribaSim.Types;
using ArribaSim.Types.Inventory;

namespace ArribaSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public void llGiveInventory(UUID destination, AString inventory)
        {
        }

        public void llGiveInventoryList(UUID target, AString folder, AnArray inventory)
        {

        }

        public void llRemoveInventory(AString item)
        {
            ObjectPartInventoryItem resitem;
            if (Part.Inventory.TryGetValue(item.ToString(), out resitem))
            {
                if (resitem.ScriptInstance != null)
                {
                    resitem.ScriptInstance.Remove();
                }
                Part.Inventory.Remove(resitem.ID);
            }
        }

        public UUID llGetInventoryCreator(AString item)
        {
            try
            {
                return Part.Inventory[item.ToString()].Creator.ID;
            }
            catch
            {
                return UUID.Zero;
            }
        }

        public UUID llGetInventoryKey(AString item)
        {
            try
            {
                return Part.Inventory[item.ToString()].ID;
            }
            catch
            {
                return UUID.Zero;
            }
        }

        public AString llGetInventoryName(Integer type, Integer number)
        {
            return new AString();
        }

        public Integer llGetInventoryNumber(Integer type)
        {
            if (type.AsInt == -1)
            {
                return new Integer(Part.Inventory.Count);
            }
            return new Integer(Part.Inventory.CountType((Types.Inventory.InventoryType)type.AsInt));
        }

        public Integer llGetInventoryPermMask(AString item, Integer category)
        {
            return new Integer(0);
        }

        public Integer llGetInventoryType(AString name)
        {
            try
            {
                return new Integer((int)Part.Inventory[name.ToString()].InventoryType);
            }
            catch
            {
                return new Integer(-1);
            }
        }

        public Integer llGetScriptState(AString script)
        {
            ObjectPartInventoryItem item;
            if(Part.Inventory.TryGetValue(script.ToString(), out item))
            {
                if(item.InventoryType != InventoryType.LSLText && item.InventoryType != InventoryType.LSLBytecode)
                {
                    llShout(DEBUG_CHANNEL, AString.Format("Inventory item {0} is not a script", script.ToString()));
                }
                else if(null == item.ScriptInstance)
                {
                    llShout(DEBUG_CHANNEL, AString.Format("Inventory item {0} is not a compiled script", script.ToString()));
                }
                else
                {
                    return new ABoolean(item.ScriptInstance.IsRunning).AsInteger;
                }
            }
            else
            {
                llShout(DEBUG_CHANNEL, AString.Format("Inventory item {0} does not exist", script.ToString()));
            }
            return new Integer(0);
        }

        public void llSetScriptState(AString script, Integer running)
        {
            ObjectPartInventoryItem item;
            if (Part.Inventory.TryGetValue(script.ToString(), out item))
            {
                if (item.InventoryType != InventoryType.LSLText && item.InventoryType != InventoryType.LSLBytecode)
                {
                    llShout(DEBUG_CHANNEL, AString.Format("Inventory item {0} is not a script", script.ToString()));
                }
                else if (null == item.ScriptInstance)
                {
                    llShout(DEBUG_CHANNEL, AString.Format("Inventory item {0} is not a compiled script", script.ToString()));
                }
                else
                {
                    item.ScriptInstance.IsRunning = running;
                }
            }
            else
            {
                llShout(DEBUG_CHANNEL, AString.Format("Inventory item {0} does not exist", script.ToString()));
            }
        }
    }
}
