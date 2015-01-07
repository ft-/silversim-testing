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

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.LSL.API.Vehicles
{
    [ScriptApiName("Vehicle")]
    [LSLImplementation]
    public class Vehicle_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        public Vehicle_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL)]
        public void llSetVehicleFlags(ScriptInstance Instance, int flags)
        {
#warning Implement llSetVehicleFlags(int)
        }

        [APILevel(APIFlags.LSL)]
        public void llRemoveVehicleFlags(ScriptInstance Instance, int flags)
        {
#warning Implement llRemoveVehicleFlags(int)
        }

        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_ANGULAR_DEFLECTION_EFFICIENCY = 32;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_ANGULAR_DEFLECTION_TIMESCALE = 33;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_ANGULAR_MOTOR_DECAY_TIMESCALE = 35;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_ANGULAR_MOTOR_TIMESCALE = 34;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_BANKING_EFFICIENCY = 38;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_BANKING_MIX = 39;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_BANKING_TIMESCALE = 40;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_BUOYANCY = 27;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_HOVER_HEIGHT = 24;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_HOVER_EFFICIENCY = 25;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_HOVER_TIMESCALE = 26;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_LINEAR_DEFLECTION_EFFICIENCY = 28;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_LINEAR_DEFLECTION_TIMESCALE = 29;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_LINEAR_MOTOR_DECAY_TIMESCALE = 31;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_LINEAR_MOTOR_TIMESCALE = 30;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_VERTICAL_ATTRACTION_EFFICIENCY = 36;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_VERTICAL_ATTRACTION_TIMESCALE = 37;

        [APILevel(APIFlags.LSL)]
        public void llSetVehicleFloatParam(ScriptInstance Instance, int param, double value)
        {
#warning Implement llSetVehicleFloatParam(int, double)
        }

        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_REFERENCE_FRAME = 44;
        [APILevel(APIFlags.LSL)]
        public void llSetVehicleRotationParam(ScriptInstance Instance, int param, Quaternion rot)
        {
#warning Implement llSetVehicleRotationParam(int, Quaternion)
        }

        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_TYPE_NONE = 0;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_TYPE_SLED = 1;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_TYPE_CAR = 2;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_TYPE_BOAT = 3;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_TYPE_AIRPLANE = 4;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_TYPE_BALLOON = 5;
        [APILevel(APIFlags.LSL)]
        public void llSetVehicleType(ScriptInstance Instance, int type)
        {
#warning Implement llSetVehicleType(int)
        }

        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_ANGULAR_FRICTION_TIMESCALE = 17;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_ANGULAR_MOTOR_DIRECTION = 19;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_LINEAR_FRICTION_TIMESCALE = 16;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_LINEAR_MOTOR_DIRECTION = 18;
        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_LINEAR_MOTOR_OFFSET = 20;
        [APILevel(APIFlags.LSL)]
        public void llSetVehicleVectorParam(ScriptInstance Instance, int param, Vector3 vec)
        {
#warning Implement llSetVehicleVectorParam(int, Vector3)
        }
    }
}
