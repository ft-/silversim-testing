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
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scripting.Common;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;

namespace SilverSim.Scripting.LSL.API.Base
{
    public partial class Base_API
    {
        [APILevel(APIFlags.LSL)]
        public string llGetScriptName(ScriptInstance Instance)
        {
            lock (Instance)
            {
                try
                {
                    return Instance.Item.Name;
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llResetScript(ScriptInstance Instance)
        {
            throw new ResetScriptException();
        }

        [APILevel(APIFlags.LSL)]
        public void llResetOtherScript(ScriptInstance Instance, string name)
        {
            lock (Instance)
            {
                ObjectPartInventoryItem item;
                ScriptInstance si;
                if (Instance.Part.Inventory.TryGetValue(name, out item))
                {
                    si = item.ScriptInstance;
                    if (item.InventoryType != InventoryType.LSLText && item.InventoryType != InventoryType.LSLBytecode)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a script", name));
                    }
                    else if (null == si)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a compiled script", name));
                    }
                    else
                    {
                        si.PostEvent(new ResetScriptEvent());
                    }
                }
                else
                {
                    throw new Exception(string.Format("Inventory item {0} does not exist", name));
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public int llGetScriptState(ScriptInstance Instance, string script)
        {
            ObjectPartInventoryItem item;
            ScriptInstance si;
            lock (Instance)
            {
                if (Instance.Part.Inventory.TryGetValue(script, out item))
                {
                    si = item.ScriptInstance;
                    if (item.InventoryType != InventoryType.LSLText && item.InventoryType != InventoryType.LSLBytecode)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a script", script));
                    }
                    else if (null == si)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a compiled script", script));
                    }
                    else
                    {
                        return si.IsRunning ? TRUE : FALSE;
                    }
                }
                else
                {
                    throw new Exception(string.Format("Inventory item {0} does not exist", script));
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetScriptState(ScriptInstance Instance, string script, int running)
        {
            ObjectPartInventoryItem item;
            ScriptInstance si;
            lock (Instance)
            {
                if (Instance.Part.Inventory.TryGetValue(script, out item))
                {
                    si = item.ScriptInstance;
                    if (item.InventoryType != InventoryType.LSLText && item.InventoryType != InventoryType.LSLBytecode)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a script", script));
                    }
                    else if (null == si)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a compiled script", script));
                    }
                    else
                    {
                        si.IsRunning = running != 0;
                    }
                }
                else
                {
                    throw new Exception(string.Format("Inventory item {0} does not exist", script));
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llRemoteLoadScript(ScriptInstance Instance, LSLKey target, string name, int running, int start_param)
        {
            lock (Instance)
            {
                Instance.ShoutError("This function has been deprecated. Please use llRemoteLoadscriptPin instead");
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llRemoteLoadScriptPin(ScriptInstance Instance, LSLKey target, string name, int pin, int running, int start_param)
        {
            lock(Instance)
            {
                ObjectPartInventoryItem scriptitem;
                ObjectPart destpart;
                AssetData asset;
                try
                {
                    destpart = Instance.Part.ObjectGroup.Scene.Primitives[target];
                }
                catch
                {
                    Instance.ShoutError("llRemoteLoadScriptPin: destination prim does not exist");
                    return;
                }

                try
                {
                    scriptitem = Instance.Part.Inventory[name];
                }
                catch
                {
                    Instance.ShoutError(string.Format("llRemoteLoadScriptPin: Script '{0}' does not exist", name));
                    return;
                }

                try
                {
                    asset = Instance.Part.ObjectGroup.Scene.AssetService[scriptitem.AssetID];
                }
                catch
                {
                    Instance.ShoutError(string.Format("llRemoteLoadScriptPin: Failed to find asset for script '{0}'", name));
                    return;
                }

                if(destpart.ID == Instance.Part.ID)
                {
                    Instance.ShoutError("llRemoteLoadScriptPin: Unable to add item");
                    return;
                }

                if(scriptitem.InventoryType != InventoryType.LSLText)
                {
                    Instance.ShoutError(string.Format("llRemoteLoadScriptPin: Inventory item '{0}' is not a script", name));
                    return;
                }

                if (destpart.Owner != Instance.Part.Owner)
                {
                    if ((scriptitem.Permissions.Current & InventoryPermissionsMask.Transfer) == 0)
                    {
                        Instance.ShoutError(string.Format("llRemoteLoadScriptPin: Item {0} does not have transfer permission", scriptitem.Name));
                        return;
                    }
                    else if(destpart.CheckPermissions(Instance.Part.Owner, Instance.Part.ObjectGroup.Group, InventoryPermissionsMask.Modify))
                    {
                        Instance.ShoutError(string.Format("llRemoteLoadScriptPin: Dest Part {0} does not have modify permission", destpart.Name));
                        return;
                    }
                }
                if ((scriptitem.Permissions.Current & InventoryPermissionsMask.Copy) == 0)
                {
                    Instance.ShoutError(string.Format("llRemoteLoadScriptPin: Item {0} does not have copy permission", scriptitem.Name));
                    return;
                }

                if(destpart.ObjectGroup.AttachPoint != AttachmentPoint.NotAttached)
                {
                    return;
                }

                if(destpart.ScriptAccessPin != pin)
                {
                    Instance.ShoutError(string.Format("llRemoteLoadScriptPin: Item {0} trying to load script onto prim {1} without correct access pin", Instance.Part.Name, destpart.Name));
                    return;
                }

                ObjectPartInventoryItem newitem = new ObjectPartInventoryItem(scriptitem);
                destpart.Inventory.Replace(name, newitem);
                ScriptInstance instance = ScriptLoader.Load(destpart, newitem, newitem.Owner, asset);
                instance.IsRunning = running != 0;
                instance.PostEvent(new OnRezEvent(start_param));
            }
        }
    }
}
