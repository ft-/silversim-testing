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

namespace SilverSim.Scripting.LSL.API.Inventory
{
    [ScriptApiName("Inventory")]
    [LSLImplementation]
    public class Inventory_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        public Inventory_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_ALL = -1;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_NONE = -1;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_TEXTURE = 0;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_SOUND = 1;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_LANDMARK = 3;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_CLOTHING = 5;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_OBJECT = 6;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_NOTECARD = 7;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_SCRIPT = 10;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_BODYPART = 13;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_ANIMATION = 20;
        [APILevel(APIFlags.LSL)]
        public const int INVENTORY_GESTURE = 21;


        [APILevel(APIFlags.LSL)]
        public const int MASK_BASE = 0;
        [APILevel(APIFlags.LSL)]
        public const int MASK_OWNER = 1;
        [APILevel(APIFlags.LSL)]
        public const int MASK_GROUP = 2;
        [APILevel(APIFlags.LSL)]
        public const int MASK_EVERYONE = 3;
        [APILevel(APIFlags.LSL)]
        public const int MASK_NEXT = 4;

        [APILevel(APIFlags.LSL)]
        public const int PERM_TRANSFER = 8192;
        [APILevel(APIFlags.LSL)]
        public const int PERM_MODIFY = 16384;
        [APILevel(APIFlags.LSL)]
        public const int PERM_COPY = 32768;
        [APILevel(APIFlags.LSL)]
        public const int PERM_MOVE = 524288;
        [APILevel(APIFlags.LSL)]
        public const int PERM_ALL = 2147483647;

        [APILevel(APIFlags.LSL)]
        public const string EOF = "\n\n\n";

        [APILevel(APIFlags.LSL)]
        public static void llGiveInventory(ScriptInstance Instance, UUID destination, string inventory)
        {
#warning Implement llGiveInventory(UUID, string)
        }

        [APILevel(APIFlags.LSL)]
        public static void llGiveInventoryList(ScriptInstance Instance, UUID target, string folder, AnArray inventory)
        {
#warning Implement llGiveInventory(UUID, string, AnArray)
        }

        [APILevel(APIFlags.LSL)]
        public static void llRemoveInventory(ScriptInstance Instance, string item)
        {
            ObjectPartInventoryItem resitem;
            lock (Instance)
            {
                if (Instance.Part.Inventory.TryGetValue(item, out resitem))
                {
                    ScriptInstance si = resitem.ScriptInstance;

                    if (si != null)
                    {
                        si.Abort();
                        si.Remove();
                    }
                    Instance.Part.Inventory.Remove(resitem.ID);
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public static UUID llGetInventoryCreator(ScriptInstance Instance, string item)
        {
            lock (Instance)
            {
                try
                {
                    return Instance.Part.Inventory[item].Creator.ID;
                }
                catch
                {
                    return UUID.Zero;
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public static UUID llGetInventoryKey(ScriptInstance Instance, string item)
        {
            lock (Instance)
            {
                try
                {
                    return Instance.Part.Inventory[item].ID;
                }
                catch
                {
                    return UUID.Zero;
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public static string llGetInventoryName(ScriptInstance Instance, int type, int number)
        {
            lock(Instance)
            {
                try
                {
                    if (type == INVENTORY_ALL)
                    {
                        return Instance.Part.Inventory[(uint)number].Name;
                    }
                    else if (type >= 0)
                    {
                        return Instance.Part.Inventory[(Types.Inventory.InventoryType)type, (uint)number].Name;
                    }
                }
                catch
                {

                }
            }
            return string.Empty;
        }

        [APILevel(APIFlags.LSL)]
        public static int llGetInventoryNumber(ScriptInstance Instance, int type)
        {
            lock (Instance)
            {
                if (type == INVENTORY_ALL)
                {
                    return Instance.Part.Inventory.Count;
                }
                return Instance.Part.Inventory.CountType((Types.Inventory.InventoryType)type);
            }
        }

        [APILevel(APIFlags.LSL)]
        public static int llGetInventoryPermMask(ScriptInstance Instance, string item, int category)
        {
            return 0;
        }

        [APILevel(APIFlags.LSL)]
        public static int llGetInventoryType(ScriptInstance Instance, string name)
        {
            lock (Instance)
            {
                try
                {
                    return (int)Instance.Part.Inventory[name.ToString()].InventoryType;
                }
                catch
                {
                    return -1;
                }
            }
        }

        #region osGetInventoryDesc
        [APILevel(APIFlags.OSSL)]
        public static string osGetInventoryDesc(ScriptInstance Instance, string item)
        {
            lock (Instance)
            {
                try
                {
                    return Instance.Part.Inventory[item].Description;
                }
                catch
                {
                    return string.Empty;
                }
            }
        }
        #endregion
    }
}
