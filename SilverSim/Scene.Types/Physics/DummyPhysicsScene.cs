// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using System.Linq;

namespace SilverSim.Scene.Types.Physics
{
    public class DummyPhysicsScene : IPhysicsScene
    {
        public DummyPhysicsScene()
        {
        }

        public void Add(IObject obj)
        {
            if(obj.GetType().GetInterfaces().Contains(typeof(IAgent)))
            {
                ((IAgent)obj).PhysicsActor = new AgentUfoPhysics((IAgent)obj);
            }
        }

        public void Remove(IObject obj)
        {
            if (obj.GetType().GetInterfaces().Contains(typeof(IAgent)))
            {
                IPhysicsObject physobj = ((IAgent)obj).PhysicsActor;
                if(physobj is AgentUfoPhysics)
                {
                    ((IAgent)obj).PhysicsActor = new DummyPhysicsObject();
                    ((AgentUfoPhysics)physobj).Dispose();
                }
            }
        }

        public void Shutdown()
        {

        }

        public void RemoveAll()
        {
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

        public static readonly DummyPhysicsScene SharedInstance = new DummyPhysicsScene();
    }
}
