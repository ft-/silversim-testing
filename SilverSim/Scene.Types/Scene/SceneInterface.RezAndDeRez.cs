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

using SilverSim.LL.Messages;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        public struct RezObjectParams
        {
            public Vector3 RayStart;
            public Vector3 RayEnd;
            public UUID RayTargetID;
            public bool RayEndIsIntersection;
            public bool RezSelected;
            public bool RemoveItem;
            public Vector3 Scale;
            public Quaternion Rotation;
            public UInt32 ItemFlags;
            public InventoryPermissionsMask GroupMask;
            public InventoryPermissionsMask EveryoneMask;
            public InventoryPermissionsMask NextOwnerMask;
        }

        public List<UInt32> RezObjects(List<ObjectGroup> groups, RezObjectParams rezparams)
        {
            throw new NotImplementedException();
        }

        public UInt32 RezObject(ObjectGroup group, RezObjectParams rezparams)
        {
            throw new NotImplementedException();
        }

        [PacketHandler(MessageType.DeRezObject)]
        void HandleDeRezObject(Message m)
        {
            SilverSim.LL.Messages.Object.DeRezAck ackres;
            SilverSim.LL.Messages.Object.DeRezObject req = (SilverSim.LL.Messages.Object.DeRezObject)m;
            if(req.AgentID != m.CircuitAgentID ||
                req.SessionID != m.CircuitSessionID)
            {
                return;
            }

            IAgent agent;
            List<ObjectGroup> objectgroups = new List<ObjectGroup>();
            try
            {
                agent = Agents[req.AgentID];
            }
            catch
            {
                return;
            }

            try
            {
                foreach (UInt32 localid in req.ObjectLocalIDs)
                {
                    try
                    {
                        ObjectGroup grp = Primitives[localid].ObjectGroup;
                        objectgroups.Add(grp);
                    }
                    catch
                    {
                        agent.SendAlertMessage("ALERT: DeleteFailObjNotFound", ID);
                    }
                }
            }
            catch
            {
                
            }

            switch(req.Destination)
            {
                case SilverSim.LL.Messages.Object.DeRezObject.DeRezAction.GodTakeCopy:
                    if(!agent.IsActiveGod || !agent.IsInScene(this))
                    {
                        return;
                    }
                    break;

                case SilverSim.LL.Messages.Object.DeRezObject.DeRezAction.Delete:
                    foreach(ObjectGroup grp in objectgroups)
                    {
                        if (!agent.IsActiveGod || !agent.IsInScene(this))
                        {
                            return;
                        }
                        else if(CanDelete(agent, grp, grp.Position))
                        {

                        }
                        else
                        {
                            agent.SendAlertMessage("ALERT: ", ID);
                            return;
                        }
                    }
                    break;

                case SilverSim.LL.Messages.Object.DeRezObject.DeRezAction.Return:
                    foreach(ObjectGroup grp in objectgroups)
                    {
                        if (!agent.IsActiveGod || !agent.IsInScene(this))
                        {
                            return;
                        }
                        else if(CanReturn(agent, grp, grp.Position))
                        {

                        }
                        else
                        {
                            agent.SendAlertMessage("No permission to return object to owners", ID);
                            ackres = new SilverSim.LL.Messages.Object.DeRezAck();
                            ackres.TransactionID = req.TransactionID;
                            ackres.Success = false;
                            agent.SendMessageAlways(ackres, ID);
                            return;
                        }
                    }
                    break;

                case SilverSim.LL.Messages.Object.DeRezObject.DeRezAction.SaveToExistingUserInventoryItem:
                    ackres = new SilverSim.LL.Messages.Object.DeRezAck();
                    ackres.TransactionID = req.TransactionID;
                    ackres.Success = false;
                    agent.SendMessageAlways(ackres, ID);
                    return;

                case SilverSim.LL.Messages.Object.DeRezObject.DeRezAction.Take:
                    foreach(ObjectGroup grp in objectgroups)
                    {
                        if (!agent.IsActiveGod || !agent.IsInScene(this))
                        {
                            return;
                        }
                        else if(CanTake(agent, grp, grp.Position))
                        {

                        }
                        else
                        {
                            agent.SendAlertMessage("No permission to take object", ID);
                            ackres = new SilverSim.LL.Messages.Object.DeRezAck();
                            ackres.TransactionID = req.TransactionID;
                            ackres.Success = false;
                            agent.SendMessageAlways(ackres, ID);
                            return;
                        }
                    }
                    break;

                case SilverSim.LL.Messages.Object.DeRezObject.DeRezAction.TakeCopy:
                    foreach(ObjectGroup grp in objectgroups)
                    {
                        if(CanTakeCopy(agent, grp, grp.Position))
                        {

                        }
                        else
                        {
                            agent.SendAlertMessage("No permission to copy object", ID);
                            ackres = new SilverSim.LL.Messages.Object.DeRezAck();
                            ackres.TransactionID = req.TransactionID;
                            ackres.Success = false;
                            agent.SendMessageAlways(ackres, ID);
                            return;
                        }
                    }
                    break;

                default:
                    agent.SendAlertMessage("Invalid derez request by viewer", ID);
                    ackres = new SilverSim.LL.Messages.Object.DeRezAck();
                    ackres.TransactionID = req.TransactionID;
                    ackres.Success = false;
                    agent.SendMessageAlways(ackres, ID);
                    return;
            }

            foreach(ObjectGroup grp in objectgroups)
            {
                grp.Scene.Remove(grp);
            }

            ackres = new SilverSim.LL.Messages.Object.DeRezAck();
            ackres.TransactionID = req.TransactionID;
            ackres.Success = true;
            agent.SendMessageAlways(ackres, ID);
        }
    }
}
