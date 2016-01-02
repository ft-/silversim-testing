// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages.Avatar;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        public void ScheduleUpdate(ObjectUpdateInfo objinfo)
        {
            foreach (IAgent a in Agents)
            {
                a.ScheduleUpdate(objinfo, ID);
            }
            foreach(ISceneListener l in SceneListeners)
            {
                l.ScheduleUpdate(objinfo, ID);
            }
        }

        private SilverSim.Viewer.Messages.Object.ObjectUpdate AgentToObjectUpdate(IAgent agent)
        {
            SilverSim.Viewer.Messages.Object.ObjectUpdate m = new Viewer.Messages.Object.ObjectUpdate();
            SilverSim.Viewer.Messages.Object.ObjectUpdate.ObjData d = new Viewer.Messages.Object.ObjectUpdate.ObjData();
            d.Data = new byte[0];
            d.ExtraParams = new byte[1];
            d.FullID = agent.ID;
            d.LocalID = agent.LocalID;
            d.Material = PrimitiveMaterial.Flesh;
            d.MediaURL = string.Empty;
            d.NameValue = string.Format("FirstName STRING RW SV {0}\nLastName STRING RW SV {1}\nTitle STRING RW SV {2}", agent.FirstName, agent.LastName, string.Empty);
            d.ObjectData = new byte[76];
            agent.CollisionPlane.ToBytes(d.ObjectData, 0);
            agent.Position.ToBytes(d.ObjectData, 16);
            agent.Velocity.ToBytes(d.ObjectData, 28);
            agent.Acceleration.ToBytes(d.ObjectData, 40);
            Vector3.Zero.ToBytes(d.ObjectData, 64); /* set to zero as per SL ObjectUpdate definition for the 76 byte format */
            Quaternion rot = agent.Rotation;
            IObject sittingobj = agent.SittingOnObject;
            if(sittingobj == null)
            {
                rot.X = 0;
                rot.Y = 0;
                rot.Normalize();
                d.ParentID = 0;
            }
            else
            {
                d.ParentID = sittingobj.LocalID;
            }
            rot.ToBytes(d.ObjectData, 52);
            
            d.PathCurve = 16;
            d.PathScaleX = 100;
            d.PathScaleY = 100;
            d.PCode = PrimitiveCode.Avatar;
            d.ProfileCurve = 1;
            d.Material = PrimitiveMaterial.Flesh;
            d.PSBlock = new byte[0];
            d.Scale = new SilverSim.Types.Vector3(0.45f, 0.6f, 1.9f);
            d.Text = string.Empty;
            d.TextColor = new SilverSim.Types.ColorAlpha(0, 0, 0, 0);
            d.TextureAnim = new byte[0];
            d.TextureEntry = new byte[0];
            d.UpdateFlags = PrimitiveFlags.Physics | PrimitiveFlags.ObjectModify | PrimitiveFlags.ObjectCopy | PrimitiveFlags.ObjectAnyOwner |
                            PrimitiveFlags.ObjectYouOwner | PrimitiveFlags.ObjectMove | PrimitiveFlags.InventoryEmpty | PrimitiveFlags.ObjectTransfer |
                            PrimitiveFlags.ObjectOwnerModify;
            m.ObjectData.Add(d);
            m.GridPosition = GridPosition;
            return m;
        }

        public void SendAgentObjectToAgent(IAgent agent, IAgent targetAgent)
        {
            SilverSim.Viewer.Messages.Object.ObjectUpdate m = AgentToObjectUpdate(agent);
            targetAgent.SendMessageAlways(m, ID);
        }

        public void SendAgentObjectToAllAgents(IAgent agent)
        {
            SilverSim.Viewer.Messages.Object.ObjectUpdate m = AgentToObjectUpdate(agent);
            foreach (IAgent a in Agents)
            {
                a.SendMessageAlways(m, ID);
            }
        }

        public void SendAgentAnimToAllAgents(AvatarAnimation areq)
        {
            foreach (IAgent a in Agents)
            {
                if (a.Owner.ID == areq.Sender)
                {
                    a.SendMessageIfRootAgent(areq, ID);
                }
                else
                {
                    a.SendMessageAlways(areq, ID);
                }
            }
        }

        public void SendKillObjectToAgents(List<UInt32> localids)
        {
            SilverSim.Viewer.Messages.Object.KillObject m = new Viewer.Messages.Object.KillObject();
            m.LocalIDs.AddRange(localids);
            
            foreach(IAgent a in Agents)
            {
                a.SendMessageAlways(m, ID);
            }
        }

        public void SendKillObjectToAgents(UInt32 localid)
        {
            SilverSim.Viewer.Messages.Object.KillObject m = new Viewer.Messages.Object.KillObject();
            m.LocalIDs.Add(localid);

            foreach (IAgent a in Agents)
            {
                a.SendMessageAlways(m, ID);
            }
        }
    }
}
