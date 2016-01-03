// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System.Linq;
using ThreadedClasses;

namespace SilverSim.Scene.Types.Physics
{
    public class DummyPhysicsScene : IPhysicsScene
    {
        readonly UUID m_SceneID;
        readonly RwLockedList<IObject> m_Agents = new RwLockedList<IObject>();

        public DummyPhysicsScene(UUID sceneID)
        {
            m_SceneID = sceneID;
        }

        public void Add(IObject obj)
        {
            if(obj.GetType().GetInterfaces().Contains(typeof(IAgent)))
            {
                IAgent agent = (IAgent)obj;
                agent.PhysicsActors.Add(m_SceneID, new AgentUfoPhysics(agent, m_SceneID));
                m_Agents.Add(obj);
            }
        }

        public void Remove(IObject obj)
        {
            if (obj.GetType().GetInterfaces().Contains(typeof(IAgent)))
            {
                IPhysicsObject physobj;
                m_Agents.Remove(obj);
                obj.PhysicsActors.Remove(m_SceneID, out physobj);
            }
        }

        public void Shutdown()
        {
            foreach (IObject obj in m_Agents)
            {
                Remove(obj);
            }
        }

        public void RemoveAll()
        {
            foreach(IObject obj in m_Agents)
            {
                Remove(obj);
            }
        }


        public double PhysicsDilationTime
        {
            get
            {
                return 0;
            }
        }

        public double PhysicsExecutionTime
        {
            get
            {
                return 0f;
            }
        }

        public double PhysicsFPS
        {
            get
            {
                return 0;
            }
        }

        public uint PhysicsFrameNumber
        {
            get
            {
                return 0;
            }
        }
    }
}
