// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.Asset.Format.Mesh;
using System.Threading;

namespace SilverSim.Scene.Physics.ShapeManager
{
    public class PhysicsShapeReference
    {
        protected PhysicsShapeManager m_PhysicsManager;
        protected PhysicsConvexShape m_ConvexShape;

        protected PhysicsShapeReference(PhysicsShapeManager manager, PhysicsConvexShape shape)
        {
            m_PhysicsManager = manager;
            m_ConvexShape = shape;
            Interlocked.Increment(ref m_ConvexShape.UseCount);
        }

        public static implicit operator PhysicsConvexShape (PhysicsShapeReference s)
        {
            return s.m_ConvexShape;
        }
    }
}
