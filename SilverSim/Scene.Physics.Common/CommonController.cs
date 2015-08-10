// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Scene.Types.Object;
using SilverSim.Types;

namespace SilverSim.Scene.Physics.Common
{
    public abstract class CommonPhysicsController
    {
        public static double GravityAccelerationConstant = -9.8f;

        public CommonPhysicsController()
        {

        }

        #region Gravity and Buoyancy
        protected Vector3 GravityMotor(IObject obj, double dt)
        {
            return new Vector3(0, 0, obj.PhysicsActor.Mass * GravityAccelerationConstant);
        }

        double m_Buoyancy = 0f;

        public double Buoyancy 
        {
            get
            {
                return m_Buoyancy;
            }

            set
            {
                if(value < 0)
                {
                    m_Buoyancy = 0;
                }
                else
                {
                    m_Buoyancy = value;
                }
            }
        }


        protected Vector3 BuoyancyMotor(IObject obj, double dt)
        {
            return new Vector3(0, 0, -m_Buoyancy * obj.PhysicsActor.Mass * GravityAccelerationConstant);
        }
        #endregion

        #region Hover Motor
        double m_HoverHeight = 0f;
        bool m_HoverEnabled = false;
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
    }
}
