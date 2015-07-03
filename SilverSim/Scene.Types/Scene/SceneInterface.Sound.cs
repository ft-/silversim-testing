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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.LL.Messages;
using SilverSim.LL.Messages.Sound;
using SilverSim.Scene.Types.Agent;
using SilverSim.Types;
using SilverSim.Scene.Types.Object;

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

            foreach (IAgent agent in Agents)
            {
                if ((agent.GlobalPosition - req.Position).Length <= soundradius)
                {
                    agent.SendMessageAlways(req, ID);
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

            foreach (IAgent agent in Agents)
            {
                if ((agent.GlobalPosition - req.Position).Length <= soundradius)
                {
                    agent.SendMessageAlways(req, ID);
                }
            }
        }

        [PacketHandler(MessageType.SoundTrigger)]
        void HandleSoundTrigger(Message m)
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
