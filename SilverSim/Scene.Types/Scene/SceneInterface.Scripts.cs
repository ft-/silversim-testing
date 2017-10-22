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

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Experience;
using SilverSim.Types;
using SilverSim.Types.Script;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Land;
using SilverSim.Viewer.Messages.Script;
using System;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        [PacketHandler(MessageType.ScriptReset)]
        internal void HandleScriptReset(Message m)
        {
            var req = (ScriptReset)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            IAgent agent = null;
            Script.ScriptInstance instance;
            ObjectPart part;
            ObjectPartInventoryItem item;
            if (!Primitives.TryGetValue(req.ObjectID, out part) ||
                !part.Inventory.TryGetValue(req.ItemID, out item) ||
                !Agents.TryGetValue(req.AgentID, out agent) ||
                !part.CheckPermissions(agent.Owner, agent.Group, SilverSim.Types.Inventory.InventoryPermissionsMask.Modify) ||
                !item.CheckPermissions(agent.Owner, agent.Group, SilverSim.Types.Inventory.InventoryPermissionsMask.Modify))
            {
                agent?.SendAlertMessage("NOTIFY: CannotResetSelectObjectsNoPermission", ID);
                return;
            }
            instance = item.ScriptInstance;
            if (instance == null)
            {
                return;
            }

            instance.PostEvent(new ResetScriptEvent());
        }

        [PacketHandler(MessageType.GetScriptRunning)]
        internal void HandleGetScriptRunning(Message m)
        {
            var req = (GetScriptRunning)m;

            IAgent agent;
            Script.ScriptInstance instance;
            ObjectPart part;
            ObjectPartInventoryItem item;
            if (!Primitives.TryGetValue(req.ObjectID, out part) ||
                !part.Inventory.TryGetValue(req.ItemID, out item) ||
                !Agents.TryGetValue(req.CircuitAgentID, out agent) ||
                !part.CheckPermissions(agent.Owner, agent.Group, SilverSim.Types.Inventory.InventoryPermissionsMask.Modify) ||
                !item.CheckPermissions(agent.Owner, agent.Group, SilverSim.Types.Inventory.InventoryPermissionsMask.Modify))
            {
                return;
            }
            instance = item.ScriptInstance;

            var reply = new ScriptRunningReply()
            {
                ItemID = req.ItemID,
                ObjectID = req.ObjectID,
                IsRunning = instance?.IsRunning == true
            };
            agent.SendMessageAlways(reply, ID);
        }

        [PacketHandler(MessageType.SetScriptRunning)]
        internal void HandleSetScriptRunning(Message m)
        {
            var req = (SetScriptRunning)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            IAgent agent;
            Script.ScriptInstance instance;
            ObjectPart part;
            ObjectPartInventoryItem item;
            if (!Primitives.TryGetValue(req.ObjectID, out part) ||
                !part.Inventory.TryGetValue(req.ItemID, out item) ||
                !Agents.TryGetValue(req.CircuitAgentID, out agent) ||
                !part.CheckPermissions(agent.Owner, agent.Group, SilverSim.Types.Inventory.InventoryPermissionsMask.Modify) ||
                !item.CheckPermissions(agent.Owner, agent.Group, SilverSim.Types.Inventory.InventoryPermissionsMask.Modify))
            {
                return;
            }
            instance = item.ScriptInstance;
            if(instance != null)
            {
                instance.IsRunning = req.IsRunning;
            }

            var reply = new ScriptRunningReply()
            {
                ItemID = req.ItemID,
                ObjectID = req.ObjectID,
                IsRunning = instance?.IsRunning == true
            };
            agent.SendMessageAlways(reply, ID);
        }

        [PacketHandler(MessageType.ScriptAnswerYes)]
        internal void HandleScriptAnswerYes(Message m)
        {
            var req = (ScriptAnswerYes)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            Script.ScriptInstance instance;
            ObjectPart part;
            ObjectPartInventoryItem item;
            IAgent agent;
            if (!Primitives.TryGetValue(req.TaskID, out part) ||
                !part.Inventory.TryGetValue(req.ItemID, out item) ||
                !Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }
            instance = item.ScriptInstance;
            if(instance == null)
            {
                return;
            }

            if(item.ExperienceID != UUID.Zero)
            {
                ExperienceServiceInterface experienceService = ExperienceService;
                if(req.Questions == ScriptPermissions.ExperienceGrantedPermissions)
                {
                    /* allow response */
                    if (agent.WaitsForExperienceResponse(part, item.ID))
                    {
                        try
                        {
                            experienceService.Permissions[item.ExperienceID, agent.Owner] = true;
                        }
                        catch (Exception ex)
                        {
                            m_Log.WarnFormat("Could not store experience accept: {0}", ex.Message);
                        }
                        instance.PostEvent(new ExperiencePermissionsEvent
                        {
                            PermissionsKey = agent.Owner
                        });
                    }
                }
                else if(req.Questions == ScriptPermissions.None)
                {
                    /* deny response */
                    if (agent.WaitsForExperienceResponse(part, item.ID))
                    {
                        try
                        {
                            experienceService.Permissions[item.ExperienceID, agent.Owner] = false;
                        }
                        catch (Exception ex)
                        {
                            m_Log.WarnFormat("Could not store experience denial: {0}", ex.Message);
                        }
                        instance.PostEvent(new ExperiencePermissionsDeniedEvent
                        {
                            AgentId = agent.Owner,
                            Reason = 4
                        });
                    }
                }
            }

            var e = new RuntimePermissionsEvent()
            {
                PermissionsKey = req.CircuitAgentOwner,
                Permissions = req.Questions
            };
            instance.PostEvent(e);
        }

        [PacketHandler(MessageType.RevokePermissions)]
        internal void HandleRevokePermissions(Message m)
        {
            var req = (RevokePermissions)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            IObject iobj;
            if(Objects.TryGetValue(req.ObjectID, out iobj))
            {
                var o = iobj as ObjectGroup;
                if (o != null)
                {
                    foreach(ObjectPart p in o.Values)
                    {
                        foreach(ObjectPartInventoryItem i in p.Inventory.Values)
                        {
                            Script.ScriptInstance instance = i.ScriptInstance;
                            instance?.RevokePermissions(req.AgentID, req.ObjectPermissions);
                        }
                    }
                }
            }
        }
    }
}
