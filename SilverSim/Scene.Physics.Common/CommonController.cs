/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

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
