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

using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        public override Quaternion CameraRotation
        {
            get { return Quaternion.Axes2Rot(CameraAtAxis, CameraLeftAxis, CameraUpAxis); }

            set
            {
                CameraAtAxis = value.FwdAxis;
                CameraLeftAxis = value.LeftAxis;
                CameraUpAxis = value.UpAxis;
            }
        }

        private Vector3 m_CameraAtAxis;
        private Vector3 m_CameraLeftAxis;
        private Vector3 m_CameraUpAxis;
        private Vector3 m_CameraPosition;

        public override Vector3 CameraPosition
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_CameraPosition;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_CameraPosition = value;
                }
            }
        }

        public Vector3 CameraLookAt
        {
            get
            {
                Vector3 atAxis = CameraAtAxis;
                if(atAxis == Vector3.Zero)
                {
                    return atAxis;
                }
                return new Vector3(atAxis.X, atAxis.Y, 0).Normalize();
            }
        }

        public override Vector3 CameraAtAxis
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_CameraAtAxis;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_CameraAtAxis = value;
                }
            }
        }

        public override Vector3 CameraLeftAxis
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_CameraLeftAxis;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_CameraLeftAxis = value;
                }
            }
        }

        public override Vector3 CameraUpAxis
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_CameraUpAxis;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_CameraUpAxis = value;
                }
            }
        }

        public bool TryGetCameraConstraints(Vector3 wantedCamPosition, out Vector4 collisionplane)
        {
            AgentCircuit circuit;
            SceneInterface scene;
            collisionplane = new Vector4(0.9f, 0.0f, 0.361f, -10000f);
            if(!Circuits.TryGetValue(m_CurrentSceneID, out circuit))
            {
                return false;
            }
            scene = circuit.Scene;
            if(scene == null)
            {
                return false;
            }
            IPhysicsScene physicsScene = scene.PhysicsScene;
            if(physicsScene == null)
            {
                return false;
            }

            RayResult[] results = physicsScene.ClosestRayTest(GlobalPosition, wantedCamPosition);
            if(results.Length == 0)
            {
                return false;
            }

            Vector3 normal = results[0].HitNormalWorld;

            collisionplane.X = normal.X;
            collisionplane.Y = normal.Y;
            collisionplane.Z = normal.Z;
            collisionplane.W = results[0].HitPointWorld.Dot(normal);
            return true;
        }
    }
}
