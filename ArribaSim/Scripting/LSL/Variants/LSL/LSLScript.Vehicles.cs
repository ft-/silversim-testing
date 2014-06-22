/*

ArribaSim is distributed under the terms of the
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
using ArribaSim.Types;

namespace ArribaSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public void llSetVehicleFlags(Integer flags)
        {

        }

        public void llRemoveVehicleFlags(Integer flags)
        {

        }

        public readonly Integer VEHICLE_ANGULAR_DEFLECTION_EFFICIENCY = new Integer(32);
        public readonly Integer VEHICLE_ANGULAR_DEFLECTION_TIMESCALE = new Integer(33);
        public readonly Integer VEHICLE_ANGULAR_MOTOR_DECAY_TIMESCALE = new Integer(35);
        public readonly Integer VEHICLE_ANGULAR_MOTOR_TIMESCALE = new Integer(34);
        public readonly Integer VEHICLE_BANKING_EFFICIENCY = new Integer(38);
        public readonly Integer VEHICLE_BANKING_MIX = new Integer(39);
        public readonly Integer VEHICLE_BANKING_TIMESCALE = new Integer(40);
        public readonly Integer VEHICLE_BUOYANCY = new Integer(27);
        public readonly Integer VEHICLE_HOVER_HEIGHT = new Integer(24);
        public readonly Integer VEHICLE_HOVER_EFFICIENCY = new Integer(25);
        public readonly Integer VEHICLE_HOVER_TIMESCALE = new Integer(26);
        public readonly Integer VEHICLE_LINEAR_DEFLECTION_EFFICIENCY = new Integer(28);
        public readonly Integer VEHICLE_LINEAR_DEFLECTION_TIMESCALE = new Integer(29);
        public readonly Integer VEHICLE_LINEAR_MOTOR_DECAY_TIMESCALE = new Integer(31);
        public readonly Integer VEHICLE_LINEAR_MOTOR_TIMESCALE = new Integer(30);
        public readonly Integer VEHICLE_VERTICAL_ATTRACTION_EFFICIENCY = new Integer(36);
        public readonly Integer VEHICLE_VERTICAL_ATTRACTION_TIMESCALE = new Integer(37);

        public void llSetVehicleFloatParam(Integer param, Real value)
        {

        }

        public readonly Integer VEHICLE_REFERENCE_FRAME = new Integer(44);
        public void llSetVehicleRotationParam(Integer param, Quaternion rot)
        {

        }

        public readonly Integer VEHICLE_TYPE_NONE = new Integer(0);
        public readonly Integer VEHICLE_TYPE_SLED = new Integer(1);
        public readonly Integer VEHICLE_TYPE_CAR = new Integer(2);
        public readonly Integer VEHICLE_TYPE_BOAT = new Integer(3);
        public readonly Integer VEHICLE_TYPE_AIRPLANE = new Integer(4);
        public readonly Integer VEHICLE_TYPE_BALLOON = new Integer(5);
        public void llSetVehicleType(Integer type)
        {

        }

        public readonly Integer VEHICLE_ANGULAR_FRICTION_TIMESCALE = new Integer(17);
        public readonly Integer VEHICLE_ANGULAR_MOTOR_DIRECTION = new Integer(19);
        public readonly Integer VEHICLE_LINEAR_FRICTION_TIMESCALE = new Integer(16);
        public readonly Integer VEHICLE_LINEAR_MOTOR_DIRECTION = new Integer(18);
        public readonly Integer VEHICLE_LINEAR_MOTOR_OFFSET = new Integer(20);
        public void llSetVehicleVectorParam(Integer param, Vector3 vec)
        {

        }
    }
}
