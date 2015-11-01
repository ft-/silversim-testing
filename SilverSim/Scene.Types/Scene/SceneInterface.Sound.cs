// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Sound;
using SilverSim.Scene.Types.Agent;
using SilverSim.Types;
using SilverSim.Scene.Types.Object;
using System.Diagnostics.CodeAnalysis;
using SilverSim.Types.Primitive;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        public void SendPreloadSound(ObjectPart objpart, UUID sound)
        {
            PreloadSound req = new PreloadSound();
            req.OwnerID = objpart.ObjectGroup.Owner.ID;
            req.SoundID = sound;
            req.ObjectID = objpart.ID;

            foreach (IAgent agent in Agents)
            {
                agent.SendMessageAlways(req, ID);
            }
        }

        public void SendAttachedSound(ObjectPart objpart, UUID sound, double gain, double soundradius, PrimitiveSoundFlags flags)
        {
            AttachedSound req = new AttachedSound();
            req.OwnerID = objpart.ObjectGroup.Owner.ID;
            req.SoundID = sound;
            req.ObjectID = objpart.ID;
            req.Flags = flags;

            if (gain < 0)
            {
                gain = 0;
            }
            else if (gain > 1)
            {
                gain = 1;
            }

            req.Gain = gain;

            if (objpart.ObjectGroup.IsAttachedToPrivate)
            {
                IAgent agent;
                if(Agents.TryGetValue(objpart.ObjectGroup.Owner.ID, out agent))
                {
                    if ((agent.GlobalPosition - objpart.GlobalPosition).Length <= soundradius)
                    {
                        agent.SendMessageAlways(req, ID);
                    }
                }
            }
            else
            {
                foreach (IAgent agent in Agents)
                {
                    if ((agent.GlobalPosition - objpart.GlobalPosition).Length <= soundradius)
                    {
                        agent.SendMessageAlways(req, ID);
                    }
                }
            }
        }

        public void SendAttachedSoundGainChange(ObjectPart objpart, double gain, double soundradius)
        {
            AttachedSoundGainChange req = new AttachedSoundGainChange();
            req.ObjectID = objpart.ID;
            req.Gain = gain.Clamp(0, 1);

            if (objpart.ObjectGroup.IsAttachedToPrivate)
            {
                IAgent agent;
                if (Agents.TryGetValue(objpart.ObjectGroup.Owner.ID, out agent))
                {
                    if ((agent.GlobalPosition - objpart.GlobalPosition).Length <= soundradius)
                    {
                        agent.SendMessageAlways(req, ID);
                    }
                }
            }
            else
            {
                foreach (IAgent agent in Agents)
                {
                    if ((agent.GlobalPosition - objpart.GlobalPosition).Length <= soundradius)
                    {
                        agent.SendMessageAlways(req, ID);
                    }
                }
            }
        }

        public void SendTriggerSound(ObjectPart objpart, UUID sound, double gain, double soundradius)
        {
            SoundTrigger req = new SoundTrigger();
            req.OwnerID = objpart.ObjectGroup.Owner.ID;
            req.SoundID = sound;
            req.ObjectID = objpart.ID;
            if (objpart.LinkNumber != 1)
            {
                req.ParentID = objpart.ObjectGroup.ID;
            }
            else
            {
                req.ParentID = UUID.Zero;
            }

            if (gain < 0)
            {
                gain = 0;
            }
            else if (gain > 1)
            {
                gain = 1;
            }

            req.Position = objpart.GlobalPosition;
            req.Gain = gain;
            req.GridPosition = RegionData.Location;

            if (objpart.ObjectGroup.IsAttachedToPrivate)
            {
                IAgent agent;
                if (Agents.TryGetValue(objpart.ObjectGroup.Owner.ID, out agent))
                {
                    if ((agent.GlobalPosition - req.Position).Length <= soundradius)
                    {
                        agent.SendMessageAlways(req, ID);
                    }
                }
            }
            else
            {
                foreach (IAgent agent in Agents)
                {
                    if ((agent.GlobalPosition - req.Position).Length <= soundradius)
                    {
                        agent.SendMessageAlways(req, ID);
                    }
                }
            }
        }

        public void SendTriggerSound(ObjectPart objpart, UUID sound, double gain, double soundradius, Vector3 top_north_east, Vector3 bottom_south_west)
        {
            if(gain < 0)
            {
                gain = 0;
            }
            else if(gain > 1)
            {
                gain = 1;
            }

            SoundTrigger req = new SoundTrigger();
            req.OwnerID = objpart.ObjectGroup.Owner.ID;
            req.SoundID = sound;
            req.ObjectID = objpart.ID;
            req.GridPosition = RegionData.Location;

            if (objpart.LinkNumber != 1)
            {
                req.ParentID = objpart.ObjectGroup.ID;
            }
            else
            {
                req.ParentID = UUID.Zero;
            }
            req.Position = objpart.GlobalPosition;
            req.Gain = gain;

            if (objpart.ObjectGroup.IsAttachedToPrivate)
            {
                IAgent agent;
                if (Agents.TryGetValue(objpart.ObjectGroup.Owner.ID, out agent))
                {
                    if ((agent.GlobalPosition - req.Position).Length <= soundradius)
                    {
                        agent.SendMessageAlways(req, ID);
                    }
                }
            }
            else
            {
                foreach (IAgent agent in Agents)
                {
                    if ((agent.GlobalPosition - req.Position).Length <= soundradius)
                    {
                        agent.SendMessageAlways(req, ID);
                    }
                }
            }
        }

        [PacketHandler(MessageType.SoundTrigger)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        internal void HandleSoundTrigger(Message m)
        {
            SoundTrigger req = (SoundTrigger)m;
            if(req.OwnerID != UUID.Zero && req.ObjectID != req.CircuitAgentID)
            {
                m_Log.DebugFormat("Ignoring Sound Trigger {0} {1}", req.ObjectID, req.SoundID);
                /* ignore Sound requests for other agents */
                return;
            }
            req.OwnerID = req.CircuitAgentID;

            IAgent ownAgent;
            try
            {
                ownAgent = Agents[req.CircuitAgentID];
            }
            catch
            {
                return;
            }

            if(!ownAgent.IsInScene(this))
            {
                return;
            }

            Vector3 pos = ownAgent.Position;
            req.Position = pos;
            req.GridPosition = RegionData.Location;

            if(req.Gain < 0)
            {
                req.Gain = 0;
            }
            else if(req.Gain > 1)
            {
                req.Gain = 1;
            }

            foreach(IAgent agent in Agents)
            {
                agent.SendMessageAlways(req, ID);
            }
        }
    }
}
