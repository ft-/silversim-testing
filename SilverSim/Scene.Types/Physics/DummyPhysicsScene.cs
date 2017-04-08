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
using SilverSim.Threading;
using SilverSim.Types;
using System.Linq;

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
                IAgent agent = (IAgent)obj;
                IPhysicsObject physobj;
                m_Agents.Remove(agent);
                agent.PhysicsActors.Remove(m_SceneID, out physobj);
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

        public string PhysicsEngineName
        {
            get
            {
                return "Dummy";
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

        public RayResult[] ClosestRayTest(Vector3 rayFromWorld, Vector3 rayToWorld)
        {
            return new RayResult[0];
        }

        public RayResult[] AllHitsRayTest(Vector3 rayFromWorld, Vector3 rayToWorld)
        {
            return new RayResult[0];
        }

        public RayResult[] ClosestRayTest(Vector3 rayFromWorld, Vector3 rayToWorld, RayTestHitFlags flags)
        {
            return new RayResult[0];
        }

        public RayResult[] AllHitsRayTest(Vector3 rayFromWorld, Vector3 rayToWorld, RayTestHitFlags flags)
        {
            return new RayResult[0];
        }
    }
}
