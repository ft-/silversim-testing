// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Script;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types.Script;
using System.Diagnostics.CodeAnalysis;
using SilverSim.Scene.Types.Agent;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        [PacketHandler(MessageType.ScriptReset)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void HandleScriptReset(Message m)
        {
            ScriptReset req = (ScriptReset)m;
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
                !Agents.TryGetValue(req.AgentID, out agent) ||
                !part.CheckPermissions(agent.Owner, agent.Group, SilverSim.Types.Inventory.InventoryPermissionsMask.Modify) ||
                !item.CheckPermissions(agent.Owner, agent.Group, SilverSim.Types.Inventory.InventoryPermissionsMask.Modify))
            {
                return;
            }
            instance = item.ScriptInstance;
            if (instance == null)
            {
                return;
            }

            instance.PostEvent(new ResetScriptEvent());
        }

        [PacketHandler(MessageType.ScriptAnswerYes)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void HandleScriptAnswerYes(Message m)
        {
            ScriptAnswerYes req = (ScriptAnswerYes)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            Script.ScriptInstance instance;
            ObjectPart part;
            ObjectPartInventoryItem item;
            if (!Primitives.TryGetValue(req.TaskID, out part) ||
                !part.Inventory.TryGetValue(req.ItemID, out item))
            {
                return;
            }
            instance = item.ScriptInstance;
            if(instance == null)
            {
                return;
            }

            RuntimePermissionsEvent e = new RuntimePermissionsEvent();
            e.PermissionsKey = req.CircuitAgentOwner;
            e.Permissions = (ScriptPermissions)req.Questions;

            instance.PostEvent(e);
        }

        [PacketHandler(MessageType.RevokePermissions)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void HandleRevokePermissions(Message m)
        {
            RevokePermissions req = (RevokePermissions)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            IObject iobj;
            if(Objects.TryGetValue(req.ObjectID, out iobj))
            {
                ObjectGroup o = iobj as ObjectGroup;
                if (o != null)
                {
                    o.ForEach(delegate(ObjectPart p)
                    {
                        p.Inventory.ForEach(delegate(ObjectPartInventoryItem i)
                        {
                            Script.ScriptInstance instance = i.ScriptInstance;
                            if(instance != null)
                            {
                                instance.RevokePermissions(req.AgentID, (ScriptPermissions)req.ObjectPermissions);
                            }
                        });
                    });
                }
            }
        }
    }
}
