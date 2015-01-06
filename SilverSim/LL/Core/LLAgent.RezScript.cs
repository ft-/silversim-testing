﻿/*

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

using log4net;
using SilverSim.LL.Messages;
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
using SilverSim.LL.Messages.Script;
using System.IO;
using SilverSim.Scripting.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Types.Asset;

namespace SilverSim.LL.Core
{
    public partial class LLAgent
    {
        void HandleRezScript(Message m)
        {
            RezScript req = (RezScript)m;
            if(req.CircuitSessionID != req.SessionID ||
                req.AgentID != req.CircuitAgentID)
            {
                return;
            }
            SceneInterface scene;
            ObjectPart part;
            Circuit circuit;
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

        void RezScriptFromAgentInventory(Circuit circuit, ObjectPart part, RezScript req)
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

            RezActualScript(circuit, part, req, data);

            if(0 == (item.Permissions.Current & InventoryPermissionsMask.Copy))
            {
                InventoryService.Item.Delete(Owner.ID, itemID);
            }
        }

        void RezNewScript(Circuit circuit, ObjectPart part, RezScript req)
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

        void RezActualScript(Circuit circuit, ObjectPart part, RezScript req, AssetData data)
        {
            ObjectPartInventoryItem item = new ObjectPartInventoryItem();
            item.Name = req.InventoryBlock.Name;
            item.AssetID = data.ID;
            item.Description = req.InventoryBlock.Description;
            item.AssetType = Types.Asset.AssetType.LSLText;
            item.Creator = Owner;
            item.Flags = 0;
            item.Group = Group;
            item.GroupOwned = false;
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
