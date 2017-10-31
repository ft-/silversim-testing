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

using SilverSim.Viewer.Messages.Avatar;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using SilverSim.Viewer.Messages;

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

        public void ScheduleUpdate(ObjectInventoryUpdateInfo objinfo)
        {
            foreach (IAgent a in Agents)
            {
                a.ScheduleUpdate(objinfo, ID);
            }
            foreach (ISceneListener l in SceneListeners)
            {
                l.ScheduleUpdate(objinfo, ID);
            }
        }

        private Message AgentToObjectUpdate(IAgent agent)
        {
            Viewer.Messages.Object.UnreliableObjectUpdate m;
            var sittingOn = agent.SittingOnObject;
            if (sittingOn != null)
            {
                m = new Viewer.Messages.Object.ObjectUpdate();
            }
            else
            {
                m = new Viewer.Messages.Object.UnreliableObjectUpdate();
            }
            var d = new Viewer.Messages.Object.UnreliableObjectUpdate.ObjData
            {
                Data = new byte[0],
                ExtraParams = new byte[1],
                FullID = agent.ID,
                LocalID = agent.LocalID,
                Material = PrimitiveMaterial.Flesh,
                MediaURL = string.Empty,
                NameValue = string.Format("FirstName STRING RW SV {0}\nLastName STRING RW SV {1}\nTitle STRING RW SV {2}", agent.FirstName, agent.LastName, string.Empty),
                ObjectData = new byte[76],
                PathCurve = 16,
                PathScaleX = 100,
                PathScaleY = 100,
                PCode = PrimitiveCode.Avatar,
                ProfileCurve = 1,
                PSBlock = new byte[0],
                Scale = new Vector3(0.45f, 0.6f, 1.9f),
                Text = string.Empty,
                TextColor = new ColorAlpha(0, 0, 0, 0),
                TextureAnim = new byte[0],
                TextureEntry = new byte[0],
                UpdateFlags = PrimitiveFlags.Physics | PrimitiveFlags.ObjectModify | PrimitiveFlags.ObjectCopy | PrimitiveFlags.ObjectAnyOwner |
                            PrimitiveFlags.ObjectYouOwner | PrimitiveFlags.ObjectMove | PrimitiveFlags.InventoryEmpty | PrimitiveFlags.ObjectTransfer |
                            PrimitiveFlags.ObjectOwnerModify
            };
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
                rot.NormalizeSelf();
                d.ParentID = 0;
            }
            else
            {
                d.ParentID = sittingobj.LocalID;
            }
            rot.ToBytes(d.ObjectData, 52);

            m.ObjectData.Add(d);
            m.GridPosition = GridPosition;
            return m;
        }

        public void SendAgentObjectToAgent(IAgent agent, IAgent targetAgent)
        {
            var m = AgentToObjectUpdate(agent);
            targetAgent.SendMessageAlways(m, ID);
            targetAgent.SendMessageAlways(agent.GetAvatarAppearanceMsg(), ID);
        }

        public void SendAgentObjectToAllAgents(IAgent agent)
        {
            var m = AgentToObjectUpdate(agent);
            foreach (IAgent a in Agents)
            {
                a.SendMessageAlways(m, ID);
            }
        }

        public void SendAgentAppearanceToAllAgents(IAgent agent)
        {
            var am = agent.GetAvatarAppearanceMsg();
            foreach (IAgent a in Agents)
            {
                a.SendMessageAlways(am, ID);
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
            var m = new Viewer.Messages.Object.KillObject();
            m.LocalIDs.AddRange(localids);

            foreach(IAgent a in Agents)
            {
                a.SendMessageAlways(m, ID);
            }
        }

        public void SendKillObjectToAgents(UInt32 localid)
        {
            var m = new Viewer.Messages.Object.KillObject();
            m.LocalIDs.Add(localid);

            foreach (IAgent a in Agents)
            {
                a.SendMessageAlways(m, ID);
            }
        }
    }
}
