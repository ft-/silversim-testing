// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Physics.Common
{
    public abstract class CommonPhysicsController
    {
        [SuppressMessage("Gendarme.Rules.Concurrency", "NonConstantStaticFieldsShouldNotBeVisibleRule")]
        public static double GravityAccelerationConstant { get; set; }

        static CommonPhysicsController()
        {
        }

        protected CommonPhysicsController()
        {

        }

        protected struct PositionalForce
        {
            public Vector3 Force;
            public Vector3 LocalPosition;

            public PositionalForce(Vector3 force, Vector3 localpos)
            {
                Force = force;
                LocalPosition = localpos;
            }
        }

        #region Gravity and Buoyancy
        protected Vector3 GravityMotor(IObject obj)
        {
            return new Vector3(0, 0, obj.PhysicsActor.Mass * GravityAccelerationConstant);
        }

        double m_Buoyancy;

        public double Buoyancy 
        {
            get
            {
                return m_Buoyancy;
            }

            set
            {
                m_Buoyancy = (value < 0) ? 0 : value;
            }
        }


        protected Vector3 BuoyancyMotor(IObject obj)
        {
            return new Vector3(0, 0, -(m_Buoyancy - 1) * obj.PhysicsActor.Mass * GravityAccelerationConstant);
        }
        #endregion

        #region Hover Motor
        double m_HoverHeight;
        bool m_HoverEnabled;
        protected Vector3 HoverMotor(IObject obj)
        {
            if (m_HoverEnabled)
            {
                Vector3 v = new Vector3(0, 0, -obj.PhysicsActor.Mass * GravityAccelerationConstant + m_Buoyancy * obj.PhysicsActor.Mass * GravityAccelerationConstant);
                v.Z += (m_HoverHeight - obj.Position.Z);
                return v;
            }
            else
            {
                return Vector3.Zero;
            }
        }
        #endregion

        #region Target Velocity Motor
        protected Vector3 TargetVelocityMotor(IObject obj, Vector3 targetvel, double factor)
        {
            return (targetvel - obj.Velocity) * factor;
        }
        #endregion

        #region Target Rotation Motor
        protected Vector3 TargetRotationMotor(IObject obj, Quaternion targetrot, double factor)
        {
            return (targetrot / obj.Rotation).AsVector3 * factor;
        }
        #endregion
    }
}
