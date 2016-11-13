// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Types;

namespace SilverSim.Scene.Physics.Common
{
    public abstract class CommonPhysicsController
    {
        public double CombinedGravityAccelerationConstant
        {
            get
            {
                return BaseGravityAccelerationConstant + AdditionalGravityAccelerationConstant;
            }
        }
        public double AdditionalGravityAccelerationConstant { get; set; }
        protected abstract double BaseGravityAccelerationConstant { get; }

        static CommonPhysicsController()
        {
        }

        protected CommonPhysicsController()
        {

        }

        public struct PositionalForce
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
        protected double GravityConstant(IPhysicalObject obj)
        {
            return obj.PhysicsActor.Mass * CombinedGravityAccelerationConstant * obj.PhysicsGravityMultiplier;
        }

        protected Vector3 GravityMotor(IPhysicalObject obj)
        {
            return new Vector3(0, 0, -GravityConstant(obj));
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


        protected Vector3 BuoyancyMotor(IPhysicalObject obj)
        {
            return new Vector3(0, 0, (m_Buoyancy - 1) * GravityConstant(obj));
        }
        #endregion

        #region Hover Motor
        double m_HoverHeight;
        bool m_HoverEnabled;
        protected Vector3 HoverMotor(IPhysicalObject obj)
        {
            if (m_HoverEnabled)
            {
                Vector3 v = new Vector3(0, 0, (m_Buoyancy - 1) * GravityConstant(obj));
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
        protected Vector3 TargetVelocityMotor(IPhysicalObject obj, Vector3 targetvel, double factor)
        {
            return (targetvel - obj.Velocity) * factor;
        }
        #endregion

        #region Target Rotation Motor
        protected Vector3 TargetRotationMotor(IPhysicalObject obj, Quaternion targetrot, double factor)
        {
            return (targetrot / obj.Rotation).AsVector3 * factor;
        }
        #endregion
    }
}
