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
using SilverSim.Types;
using SilverSim.Types.Primitive;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Avatar;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        public void ScheduleUpdate(ObjectUpdateInfo objinfo, bool isPhysicsOrigin = false)
        {
            foreach (IAgent a in Agents)
            {
                a.ScheduleUpdate(objinfo, ID);
            }
            foreach(ISceneListener l in SceneListeners)
            {
                if(isPhysicsOrigin && l.IgnorePhysicsLocationUpdates)
                {
                    continue;
                }
                l.ScheduleUpdate(objinfo, ID);
            }
        }

        public void ScheduleUpdate(AgentUpdateInfo agentinfo)
        {
            foreach(IAgent a in Agents)
            {
                a.ScheduleUpdate(agentinfo, ID);
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

        public void SendAgentObjectToAgent(IAgent agent, IAgent targetAgent)
        {
            AgentUpdateInfo aui = agent.GetUpdateInfo(ID);
            if (aui != null)
            {
                targetAgent.ScheduleUpdate(aui, ID);
                targetAgent.SendMessageAlways(agent.GetAvatarAppearanceMsg(), ID);
            }
        }

        public void SendAgentObjectToAllAgents(IAgent agent)
        {
            AgentUpdateInfo aui = agent.GetUpdateInfo(ID);
            if (aui != null)
            {
                foreach (IAgent a in Agents)
                {
                    a.ScheduleUpdate(aui, ID);
                }
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

        /*
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
        */
    }
}
