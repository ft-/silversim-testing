// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Script;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types.Script;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        [PacketHandler(MessageType.ScriptAnswerYes)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        internal void HandleScriptAnswerYes(Message m)
        {
            ScriptAnswerYes req = (ScriptAnswerYes)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            Script.ScriptInstance instance;
            try
            {
                ObjectPart p = Primitives[req.TaskID];
                ObjectPartInventoryItem item = p.Inventory[req.ItemID];
                instance = item.ScriptInstance;
            }
            catch
            {
                return;
            }
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
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        internal void HandleRevokePermissions(Message m)
        {
            RevokePermissions req = (RevokePermissions)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            try
            {
                ObjectGroup o = Objects[req.ObjectID] as ObjectGroup;
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
            catch
            {
                return;
            }
        }
    }
}
