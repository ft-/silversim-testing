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
        public static double GravityAccelerationConstant = -9.8f;

        protected CommonPhysicsController()
        {

        }

        #region Gravity and Buoyancy
        protected Vector3 GravityMotor(IObject obj, double dt)
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


        protected Vector3 BuoyancyMotor(IObject obj, double dt)
        {
            return new Vector3(0, 0, -m_Buoyancy * obj.PhysicsActor.Mass * GravityAccelerationConstant);
        }
        #endregion

        #region Hover Motor
        double m_HoverHeight;
        bool m_HoverEnabled;
        protected Vector3 HoverMotor(IObject obj, double dt)
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
        protected Vector3 TargetVelocityMotor(IObject obj, Vector3 targetvel, double factor, double dt)
        {
            return (targetvel - obj.Velocity) * factor;
        }
        #endregion

        #region Target Rotation Motor
        protected Vector3 TargetRotationMotor(IObject obj, Quaternion targetrot, double factor, double dt)
        {
            return (targetrot / obj.Rotation).AsVector3 * factor;
        }
        #endregion
    }
}
