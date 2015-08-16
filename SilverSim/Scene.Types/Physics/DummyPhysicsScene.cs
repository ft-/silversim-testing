// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;

namespace SilverSim.Scene.Types.Physics
{
    public class DummyPhysicsScene : IPhysicsScene
    {
        public DummyPhysicsScene()
        {
        }

        public void Add(IObject obj)
        {

        }

        public void Remove(IObject obj)
        {

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
