// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Viewer.Messages;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Agent;
using SilverSim.Types.Grid;
using SilverSim.Types.IM;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using ThreadedClasses;
using System.Threading;
using SilverSim.Viewer.Messages.Script;
using System.IO;
using SilverSim.Scripting.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Types.Asset;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        [PacketHandler(MessageType.RezScript)]
        internal void HandleRezScript(Message m)
        {
            RezScript req = (RezScript)m;
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
                circuit = Circuits[req.InventoryBlock.FolderID];
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

        void RezScriptFromAgentInventory(AgentCircuit circuit, ObjectPart part, RezScript req)
        {
            UUID itemID = req.InventoryBlock.ItemID;
            InventoryItem item;
            try
            {
                item = InventoryService.Item[Owner.ID, itemID];
            }
            catch
            {
                Messages.Alert.AlertMessage res = new Messages.Alert.AlertMessage();
                res.Message = "ALERT: ScriptMissing";
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
                    Messages.Alert.AlertMessage res = new Messages.Alert.AlertMessage();
                    res.Message = "ALERT: ScriptMissing";
                    circuit.SendMessage(res);
                    return;
                }
            }
            
            if(item.AssetType != AssetType.LSLText)
            {
                Messages.Alert.AlertMessage res = new Messages.Alert.AlertMessage();
                res.Message = "Unable to rez a non-script asset as script";
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
                    Messages.Alert.AlertMessage res = new Messages.Alert.AlertMessage();
                    res.Message = "ALERT: ScriptMissing";
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
                    Messages.Alert.AlertMessage res = new Messages.Alert.AlertMessage();
                    res.Message = "ALERT: UnableToLoadScript";
                    circuit.SendMessage(res);
                    return;
                }
            }

            if (data.Type != AssetType.LSLText)
            {
                Messages.Alert.AlertMessage res = new Messages.Alert.AlertMessage();
                res.Message = "Unable to rez a non-script asset as script";
                circuit.SendMessage(res);
                return;
            }

            RezActualScript(circuit, part, req, data);

            if(0 == (item.Permissions.Current & InventoryPermissionsMask.Copy))
            {
                InventoryService.Item.Delete(Owner.ID, itemID);
            }
        }

        void RezNewScript(AgentCircuit circuit, ObjectPart part, RezScript req)
        {
            AssetData data;
            try
            {
                /* this is the KAN-Ed llSay script */
                data = circuit.Scene.AssetService["366ac8e9-b391-11dc-8314-0800200c9a66"];
            }
            catch
            {
                Messages.Alert.AlertMessage res = new Messages.Alert.AlertMessage();
                res.Message = "ALERT: ScriptMissing";
                circuit.SendMessage(res);
                return;
            }

            RezActualScript(circuit, part, req, data);
        }

        void RezActualScript(AgentCircuit circuit, ObjectPart part, RezScript req, AssetData data)
        {
            if(!part.CheckPermissions(Owner, Group, InventoryPermissionsMask.Modify))
            {
                Messages.Alert.AlertMessage res = new Messages.Alert.AlertMessage();
                res.Message = "ALERT: NoPermModifyObject";
                circuit.SendMessage(res);
                return;
            }
            ObjectPartInventoryItem item = new ObjectPartInventoryItem();
            item.Name = req.InventoryBlock.Name;
            item.AssetID = data.ID;
            item.Description = req.InventoryBlock.Description;
            item.AssetType = Types.Asset.AssetType.LSLText;
            item.Creator = Owner;
            item.Flags = 0;
            item.Group = Group;
            item.IsGroupOwned = false;
            item.ID = UUID.Random;
            item.InventoryType = InventoryType.LSLText;
            item.LastOwner = Owner;
            item.ParentFolderID = part.ID;
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
                using (TextReader reader = new StreamReader(data.InputStream))
                {
                    instance = ScriptLoader.Load(part, item, item.Owner, data);
                }
                item.ScriptInstance = instance;
                item.ScriptInstance.IsRunning = true;
            }
            catch
            {
                Messages.Alert.AlertMessage res = new Messages.Alert.AlertMessage();
                res.Message = "Could not compile script";
                circuit.SendMessage(res);
                return;
            }
        }
    }
}
