// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Alert;
using SilverSim.Viewer.Messages.Script;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        [PacketHandler(MessageType.RezScript)]
        public void HandleRezScript(Message m)
        {
            var req = (RezScript)m;
            if(req.CircuitSessionID != req.SessionID ||
                req.AgentID != req.CircuitAgentID)
            {
                return;
            }
            SceneInterface scene;
            ObjectPart part;
            AgentCircuit circuit;
            try
            {
                circuit = Circuits[req.CircuitSceneID];
                scene = circuit.Scene;
                part = scene.Primitives[req.InventoryBlock.FolderID];
            }
            catch
            {
                return;
            }

            if(req.InventoryBlock.ItemID != UUID.Zero)
            {
                RezScriptFromAgentInventory(circuit, part, req);
            }
            else
            {
                RezNewScript(circuit, part, req);
            }
        }

        private void RezScriptFromAgentInventory(AgentCircuit circuit, ObjectPart part, RezScript req)
        {
            var itemID = req.InventoryBlock.ItemID;
            InventoryItem item;
            try
            {
                item = InventoryService.Item[Owner.ID, itemID];
            }
            catch
            {
                var res = new AlertMessage
                {
                    Message = "ALERT: ScriptMissing"
                };
                circuit.SendMessage(res);
                return;
            }

            if(item.AssetType == AssetType.Link)
            {
                try
                {
                    item = InventoryService.Item[Owner.ID, item.AssetID];
                }
                catch
                {
                    var res = new AlertMessage
                    {
                        Message = "ALERT: ScriptMissing"
                    };
                    circuit.SendMessage(res);
                    return;
                }
            }

            if(item.AssetType != AssetType.LSLText)
            {
                var res = new AlertMessage
                {
                    Message = "Unable to rez a non-script asset as script"
                };
                circuit.SendMessage(res);
                return;
            }

            AssetData data;

            /* Fetch asset locally first. It can prevent a lengthy request on foreign services. */
            try
            {
                data = circuit.Scene.AssetService[item.AssetID];
            }
            catch
            {
                /* Fetch asset from agent */
                try
                {
                    data = AssetService[item.AssetID];
                }
                catch
                {
                    var res = new AlertMessage
                    {
                        Message = "ALERT: ScriptMissing"
                    };
                    circuit.SendMessage(res);
                    return;
                }

                /* Do not trust foreign sources for correct flags */
                data.Temporary = false;
                data.Local = false;

                /* Save asset locally */
                try
                {
                    circuit.Scene.AssetService.Store(data);
                }
                catch
                {
                    var res = new AlertMessage
                    {
                        Message = "ALERT: UnableToLoadScript"
                    };
                    circuit.SendMessage(res);
                    return;
                }
            }

            if (data.Type != AssetType.LSLText)
            {
                var res = new AlertMessage
                {
                    Message = this.GetLanguageString(CurrentCulture, "UnableToRezANonScriptAsScript", "Unable to rez a non-script asset as script")
                };
                circuit.SendMessage(res);
                return;
            }

            RezActualScript(circuit, part, req, data);

            if(0 == (item.Permissions.Current & InventoryPermissionsMask.Copy))
            {
                InventoryService.Item.Delete(Owner.ID, itemID);
            }
        }

        private void RezNewScript(AgentCircuit circuit, ObjectPart part, RezScript req)
        {
            AssetData data;
            try
            {
                /* this is the KAN-Ed llSay script */
                data = circuit.Scene.AssetService["366ac8e9-b391-11dc-8314-0800200c9a66"];
            }
            catch
            {
                var res = new AlertMessage
                {
                    Message = "ALERT: ScriptMissing"
                };
                circuit.SendMessage(res);
                return;
            }

            RezActualScript(circuit, part, req, data);
        }

        private void RezActualScript(AgentCircuit circuit, ObjectPart part, RezScript req, AssetData data)
        {
            if(!part.CheckPermissions(Owner, Group, InventoryPermissionsMask.Modify))
            {
                var res = new AlertMessage
                {
                    Message = "ALERT: NoPermModifyObject"
                };
                circuit.SendMessage(res);
                return;
            }
            var item = new ObjectPartInventoryItem
            {
                Name = req.InventoryBlock.Name,
                AssetID = data.ID,
                Description = req.InventoryBlock.Description,
                AssetType = AssetType.LSLText,
                Creator = Owner,
                Owner = Owner,
                Flags = 0,
                Group = Group,
                IsGroupOwned = false,
                InventoryType = InventoryType.LSL,
                LastOwner = Owner,
                ParentFolderID = part.ID
            };
            item.Permissions.Base = req.InventoryBlock.BaseMask;
            item.Permissions.Current = req.InventoryBlock.OwnerMask;
            item.Permissions.EveryOne = req.InventoryBlock.EveryoneMask;
            item.Permissions.Group = req.InventoryBlock.GroupMask;
            item.Permissions.NextOwner = req.InventoryBlock.NextOwnerMask;
            item.SaleInfo.Price = req.InventoryBlock.SalePrice;
            item.SaleInfo.Type = req.InventoryBlock.SaleType;

            part.Inventory.Add(item);
            part.SendObjectUpdate();

            ScriptInstance instance;
            try
            {
                SceneInterface scene = part.ObjectGroup.Scene;
                instance = ScriptLoader.Load(part, item, item.Owner, data, CurrentCulture, openInclude: part.OpenScriptInclude);
                item.ScriptInstance = instance;
                item.ScriptInstance.IsRunningAllowed = scene.CanRunScript(item.Owner, part.ObjectGroup.GlobalPosition, item.AssetID);
                item.ScriptInstance.IsRunning = true;
                item.ScriptInstance.Reset();
                part.ObjectGroup.Scene.SendObjectPropertiesToAgent(this, part);
            }
            catch
            {
                var res = new AlertMessage
                {
                    Message = this.GetLanguageString(circuit.Agent.CurrentCulture, "CouldNotCompileScript", "Could not compile script")
                };
                circuit.SendMessage(res);
                return;
            }
        }
    }
}
