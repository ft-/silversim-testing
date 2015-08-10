// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Physics.Vehicle;
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
            IPhysicsObject physobj = Instance.Part.ObjectGroup.RootPart.PhysicsActor;
            if (null == physobj)
            {
                Instance.ShoutError("Object has not physical properties");
                return;
            }

            physobj.SetVehicleFlags = (VehicleFlags)flags;
        }

        [APILevel(APIFlags.LSL)]
        public void llRemoveVehicleFlags(ScriptInstance Instance, int flags)
        {
            IPhysicsObject physobj = Instance.Part.ObjectGroup.RootPart.PhysicsActor;
            if (null == physobj)
            {
                Instance.ShoutError("Object has not physical properties");
                return;
            }

            physobj.ClearVehicleFlags = (VehicleFlags)flags;
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
            IPhysicsObject physobj = Instance.Part.ObjectGroup.RootPart.PhysicsActor;
            if(null == physobj)
            {
                Instance.ShoutError("Object has not physical properties");
                return;
            }

            switch(param)
            {
                case VEHICLE_ANGULAR_FRICTION_TIMESCALE:
                    physobj[VehicleVectorParamId.AngularFrictionTimescale] = new Vector3(value);
                    break;

                case VEHICLE_ANGULAR_MOTOR_DIRECTION:
                    physobj[VehicleVectorParamId.AngularMotorDirection] = new Vector3(value);
                    break;

                case VEHICLE_LINEAR_FRICTION_TIMESCALE:
                    physobj[VehicleVectorParamId.LinearFrictionTimescale] = new Vector3(value);
                    break;

                case VEHICLE_LINEAR_MOTOR_DIRECTION:
                    physobj[VehicleVectorParamId.LinearMotorDirection] = new Vector3(value);
                    break;

                case VEHICLE_LINEAR_MOTOR_OFFSET:
                    physobj[VehicleVectorParamId.LinearMotorOffset] = new Vector3(value);
                    break;

                case VEHICLE_ANGULAR_DEFLECTION_EFFICIENCY:
                    physobj[VehicleFloatParamId.AngularDeflectionEfficiency] = value;
                    break;

                case VEHICLE_ANGULAR_DEFLECTION_TIMESCALE:
                    physobj[VehicleFloatParamId.AngularDeflectionTimescale] = value;
                    break;

                case VEHICLE_ANGULAR_MOTOR_DECAY_TIMESCALE:
                    physobj[VehicleFloatParamId.AngularMotorDecayTimescale] = value;
                    break;

                case VEHICLE_ANGULAR_MOTOR_TIMESCALE:
                    physobj[VehicleFloatParamId.AngularMotorTimescale] = value;
                    break;

                case VEHICLE_BANKING_EFFICIENCY:
                    physobj[VehicleFloatParamId.BankingEfficiency] = value;
                    break;

                case VEHICLE_BANKING_MIX:
                    physobj[VehicleFloatParamId.BankingMix] = value;
                    break;

                case VEHICLE_BANKING_TIMESCALE:
                    physobj[VehicleFloatParamId.BankingTimescale] = value;
                    break;

                case VEHICLE_BUOYANCY:
                    physobj[VehicleFloatParamId.Buoyancy] = value;
                    break;

                case VEHICLE_HOVER_HEIGHT:
                    physobj[VehicleFloatParamId.HoverHeight] = value;
                    break;

                case VEHICLE_HOVER_EFFICIENCY:
                    physobj[VehicleFloatParamId.HoverEfficiency] = value;
                    break;

                case VEHICLE_HOVER_TIMESCALE:
                    physobj[VehicleFloatParamId.HoverTimescale] = value;
                    break;

                case VEHICLE_LINEAR_DEFLECTION_EFFICIENCY:
                    physobj[VehicleFloatParamId.LinearDeflectionEfficiency] = value;
                    break;

                case VEHICLE_LINEAR_DEFLECTION_TIMESCALE:
                    physobj[VehicleFloatParamId.LinearDeflectionTimescale] = value;
                    break;

                case VEHICLE_LINEAR_MOTOR_DECAY_TIMESCALE:
                    physobj[VehicleFloatParamId.LinearMotorDecayTimescale] = value;
                    break;

                case VEHICLE_LINEAR_MOTOR_TIMESCALE:
                    physobj[VehicleFloatParamId.LinearMotorTimescale] = value;
                    break;

                case VEHICLE_VERTICAL_ATTRACTION_EFFICIENCY:
                    physobj[VehicleFloatParamId.VerticalAttractionEfficiency] = value;
                    break;

                case VEHICLE_VERTICAL_ATTRACTION_TIMESCALE:
                    physobj[VehicleFloatParamId.VerticalAttractionTimescale] = value;
                    break;

                default:
                    break;
            }
        }

        [APILevel(APIFlags.LSL)]
        public const int VEHICLE_REFERENCE_FRAME = 44;
        [APILevel(APIFlags.LSL)]
        public void llSetVehicleRotationParam(ScriptInstance Instance, int param, Quaternion rot)
        {
            IPhysicsObject physobj = Instance.Part.ObjectGroup.RootPart.PhysicsActor;
            if (null == physobj)
            {
                Instance.ShoutError("Object has not physical properties");
                return;
            }

            switch(param)
            {
                case VEHICLE_REFERENCE_FRAME:
                    physobj[VehicleRotationParamId.ReferenceFrame] = rot;
                    break;

                default:
                    break;
            }
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
            IPhysicsObject physobj = Instance.Part.ObjectGroup.RootPart.PhysicsActor;
            if(null == physobj)
            {
                Instance.ShoutError("Object has not physical properties");
                return;
            }

            switch(type)
            {
                case VEHICLE_TYPE_NONE:
                    physobj.VehicleType = VehicleType.None;
                    break;

                case VEHICLE_TYPE_SLED:
                    physobj.VehicleType = VehicleType.Sled;
                    break;

                case VEHICLE_TYPE_CAR:
                    physobj.VehicleType = VehicleType.Car;
                    break;

                case VEHICLE_TYPE_BOAT:
                    physobj.VehicleType = VehicleType.Boat;
                    break;

                case VEHICLE_TYPE_AIRPLANE:
                    physobj.VehicleType = VehicleType.Airplane;
                    break;

                case VEHICLE_TYPE_BALLOON:
                    physobj.VehicleType = VehicleType.Balloon;
                    break;

                default:
                    Instance.ShoutError("Invalid vehicle type");
                    break;
            }
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
            IPhysicsObject physobj = Instance.Part.ObjectGroup.RootPart.PhysicsActor;
            if (null == physobj)
            {
                Instance.ShoutError("Object has not physical properties");
                return;
            }

            switch(param)
            {
                case VEHICLE_ANGULAR_FRICTION_TIMESCALE:
                    physobj[VehicleVectorParamId.AngularFrictionTimescale] = vec;
                    break;
                
                case VEHICLE_ANGULAR_MOTOR_DIRECTION:
                    physobj[VehicleVectorParamId.AngularMotorDirection] = vec;
                    break;
                
                case VEHICLE_LINEAR_FRICTION_TIMESCALE:
                    physobj[VehicleVectorParamId.LinearFrictionTimescale] = vec;
                    break;
                
                case VEHICLE_LINEAR_MOTOR_DIRECTION:
                    physobj[VehicleVectorParamId.LinearMotorDirection] = vec;
                    break;

                case VEHICLE_LINEAR_MOTOR_OFFSET:
                    physobj[VehicleVectorParamId.LinearMotorOffset] = vec;
                    break;

            }
        }
    }
}
