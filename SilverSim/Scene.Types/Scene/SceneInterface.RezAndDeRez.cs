// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
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
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        internal void HandleDeRezObject(Message m)
        {
            SilverSim.Viewer.Messages.Object.DeRezAck ackres;
            SilverSim.Viewer.Messages.Object.DeRezObject req = (SilverSim.Viewer.Messages.Object.DeRezObject)m;
            if(req.AgentID != m.CircuitAgentID ||
                req.SessionID != m.CircuitSessionID)
            {
                return;
            }

            IAgent agent;
            List<ObjectGroup> objectgroups = new List<ObjectGroup>();
            if(!Agents.TryGetValue(req.AgentID, out agent))
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

            bool isActiveGod = agent.IsActiveGod;

            switch (req.Destination)
            {
                case SilverSim.Viewer.Messages.Object.DeRezObject.DeRezAction.GodTakeCopy:
                    if(!isActiveGod || !agent.IsInScene(this))
                    {
                        return;
                    }
                    break;

                case SilverSim.Viewer.Messages.Object.DeRezObject.DeRezAction.Delete:
                    foreach(ObjectGroup grp in objectgroups)
                    {
                        if (!isActiveGod || !agent.IsInScene(this))
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

                case SilverSim.Viewer.Messages.Object.DeRezObject.DeRezAction.Return:
                    foreach(ObjectGroup grp in objectgroups)
                    {
                        if (!isActiveGod || !agent.IsInScene(this))
                        {
                            return;
                        }
                        else if(CanReturn(agent, grp, grp.Position))
                        {

                        }
                        else
                        {
                            agent.SendAlertMessage(string.Format("No permission to return object '{0}' to owners", grp.Name), ID);
                            ackres = new SilverSim.Viewer.Messages.Object.DeRezAck();
                            ackres.TransactionID = req.TransactionID;
                            ackres.Success = false;
                            agent.SendMessageAlways(ackres, ID);
                            return;
                        }
                    }
                    break;

                case SilverSim.Viewer.Messages.Object.DeRezObject.DeRezAction.SaveToExistingUserInventoryItem:
                    ackres = new SilverSim.Viewer.Messages.Object.DeRezAck();
                    ackres.TransactionID = req.TransactionID;
                    ackres.Success = false;
                    agent.SendMessageAlways(ackres, ID);
                    return;

                case SilverSim.Viewer.Messages.Object.DeRezObject.DeRezAction.Take:
                    foreach(ObjectGroup grp in objectgroups)
                    {
                        if (!isActiveGod || !agent.IsInScene(this))
                        {
                            return;
                        }
                        else if(CanTake(agent, grp, grp.Position))
                        {

                        }
                        else
                        {
                            agent.SendAlertMessage(string.Format("No permission to take object '{0}'", grp.Name), ID);
                            ackres = new SilverSim.Viewer.Messages.Object.DeRezAck();
                            ackres.TransactionID = req.TransactionID;
                            ackres.Success = false;
                            agent.SendMessageAlways(ackres, ID);
                            return;
                        }
                    }
                    break;

                case SilverSim.Viewer.Messages.Object.DeRezObject.DeRezAction.TakeCopy:
                    foreach(ObjectGroup grp in objectgroups)
                    {
                        if(CanTakeCopy(agent, grp, grp.Position))
                        {

                        }
                        else
                        {
                            agent.SendAlertMessage(string.Format("No permission to copy object '{0}'", grp.Name), ID);
                            ackres = new SilverSim.Viewer.Messages.Object.DeRezAck();
                            ackres.TransactionID = req.TransactionID;
                            ackres.Success = false;
                            agent.SendMessageAlways(ackres, ID);
                            return;
                        }
                    }
                    break;

                default:
                    agent.SendAlertMessage("Invalid derez request by viewer", ID);
                    ackres = new SilverSim.Viewer.Messages.Object.DeRezAck();
                    ackres.TransactionID = req.TransactionID;
                    ackres.Success = false;
                    agent.SendMessageAlways(ackres, ID);
                    return;
            }

            foreach(ObjectGroup grp in objectgroups)
            {
                grp.Scene.Remove(grp);
            }

            ackres = new SilverSim.Viewer.Messages.Object.DeRezAck();
            ackres.TransactionID = req.TransactionID;
            ackres.Success = true;
            agent.SendMessageAlways(ackres, ID);
        }
    }
}
