// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Script;
using SilverSim.Types.Inventory;
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
        public void llGiveInventory(ScriptInstance Instance, LSLKey destination, string inventory)
        {
#warning Implement llGiveInventory(UUID, string)
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(3)]
        public void llGiveInventoryList(ScriptInstance Instance, LSLKey target, string folder, AnArray inventory)
        {
#warning Implement llGiveInventory(UUID, string, AnArray)
        }

        [APILevel(APIFlags.LSL)]
        public void llRemoveInventory(ScriptInstance Instance, string item)
        {
            ObjectPartInventoryItem resitem;
            lock (Instance)
            {
                if (Instance.Part.Inventory.TryGetValue(item, out resitem))
                {
                    ScriptInstance si = resitem.ScriptInstance;

                    Instance.Part.Inventory.Remove(resitem.ID);
                    if (si == Instance)
                    {
                        throw new ScriptAbortException();
                    }
                    else if (si != null)
                    {
                        si = resitem.RemoveScriptInstance;
                        if (si != null)
                        {
                            si.Abort();
                            si.Remove();
                        }
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public LSLKey llGetInventoryCreator(ScriptInstance Instance, string item)
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
        public LSLKey llGetInventoryKey(ScriptInstance Instance, string item)
        {
            lock (Instance)
            {
                try
                {
                    return Instance.Part.Inventory[item].AssetID;
                }
                catch
                {
                    return UUID.Zero;
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public string llGetInventoryName(ScriptInstance Instance, int type, int number)
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
        public int llGetInventoryNumber(ScriptInstance Instance, int type)
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
        public void llSetInventoryPermMask(ScriptInstance Instance, string name, int category, int mask)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public int llGetInventoryPermMask(ScriptInstance Instance, string name, int category)
        {
            lock(Instance)
            {
                try
                {
                    ObjectPartInventoryItem item = Instance.Part.Inventory[name];
                    InventoryPermissionsMask mask;
                    switch(category)
                    {
                        case MASK_BASE:
                            mask = item.Permissions.Base;
                            break;

                        case MASK_EVERYONE:
                            mask = item.Permissions.EveryOne;
                            break;

                        case MASK_GROUP:
                            mask = item.Permissions.Group;
                            break;

                        case MASK_NEXT:
                            mask = item.Permissions.NextOwner;
                            break;

                        case MASK_OWNER:
                            mask = item.Permissions.Current;
                            break;

                        default:
                            mask = InventoryPermissionsMask.None;
                            break;
                    }
                    return (int)(UInt32)mask;
                }
                catch
                {
                    throw new Exception(string.Format("Inventory item {0} does not exist", name));
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public int llGetInventoryType(ScriptInstance Instance, string name)
        {
            lock (Instance)
            {
                try
                {
                    return (int)Instance.Part.Inventory[name].InventoryType;
                }
                catch
                {
                    return -1;
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(1.0)]
        public LSLKey llRequestInventoryData(ScriptInstance Instance, string name)
        {
#warning Implement llRequestInventoryData
            throw new NotImplementedException();
        }

        #region osGetInventoryDesc
        [APILevel(APIFlags.OSSL)]
        public string osGetInventoryDesc(ScriptInstance Instance, string item)
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

        #region Rez Inventory
        [APILevel(APIFlags.LSL)]
        public void llRezObject(ScriptInstance Instance, string inventory, Vector3 pos, Vector3 vel, Quaternion rot, int param)
        {
#warning Implement llRezObject
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public void llRezAtRoot(ScriptInstance Instance, string inventory, Vector3 pos, Vector3 vel, Quaternion rot, int param)
        {
#warning Implement llRezAtRoot
            throw new NotImplementedException();
        }
        #endregion
    }
}
