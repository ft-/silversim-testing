// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;

namespace SilverSim.Scene.Physics.Common.Vehicle
{
    public class VehicleMotor
    {
        readonly VehicleParams m_Params;

        internal VehicleMotor(VehicleParams param)
        {
            m_Params = param;
        }

        public void Process(double dt, PhysicsStateData currentState, SceneInterface scene)
        {
            if(m_Params.VehicleType == VehicleType.None)
            {
                /* disable vehicle */
                LinearForce = Vector3.Zero;
                AngularTorque = Vector3.Zero;
                return;
            }

            VehicleFlags flags = m_Params.Flags;
            Vector3 linearForce = Vector3.Zero;
            Vector3 angularTorque = Vector3.Zero;

            #region Transform Reference Frame
            Quaternion referenceFrame = m_Params[VehicleRotationParamId.ReferenceFrame];
            Vector3 velocity = currentState.Velocity / referenceFrame;
            Vector3 angularVelocity = (Quaternion.CreateFromEulers(currentState.AngularVelocity) / referenceFrame).GetEulerAngles();
            Quaternion angularOrientaton = currentState.Rotation / referenceFrame;
            #endregion

            #region Motor Inputs
            linearForce += (m_Params[VehicleVectorParamId.LinearMotorDirection] - velocity).ElementMultiply(m_Params.OneByLinearMotorTimescale * dt);
            angularTorque += (m_Params[VehicleVectorParamId.AngularMotorDirection] - angularVelocity).ElementMultiply(m_Params.OneByAngularMotorTimescale * dt);
            #endregion

            #region Motor Limiting
            if ((m_Params.Flags & VehicleFlags.LimitMotorDown) != 0 && linearForce.Z < 0)
            {
                linearForce.Z = 0;
            }
            if ((m_Params.Flags & VehicleFlags.LimitMotorUp) != 0 && linearForce.Z > 0)
            {
                linearForce.Z = 0;
            }
            #endregion

            #region Friction
            linearForce -= (currentState.Velocity).ElementMultiply(m_Params.OneByLinearFrictionTimescale * dt);
            angularTorque -= (currentState.AngularVelocity).ElementMultiply(m_Params.OneByAngularFrictionTimescale * dt);
            #endregion

            #region Hover Height
            double hoverForce = 0;
            Vector3 pos = currentState.Position;
            double hoverHeight = scene.Terrain[pos];
            double waterHeight = scene.RegionSettings.WaterHeight;

            if((flags & VehicleFlags.HoverGlobalHeight) != 0)
            {
                hoverHeight = m_Params[VehicleFloatParamId.HoverHeight];
            }
            else if((flags & VehicleFlags.HoverWaterOnly) != 0 ||
                (flags & VehicleFlags.HoverTerrainOnly) == 0 &&
                hoverHeight < waterHeight)
            {
                hoverHeight = waterHeight;
            }

            hoverForce = (hoverHeight - pos.Z) * m_Params[VehicleFloatParamId.HoverEfficiency] * m_Params.OneByHoverTimescale * dt;
            if((m_Params.Flags & VehicleFlags.HoverUpOnly) != 0 && hoverForce < 0)
            {
                hoverForce = 0;
            }
            linearForce.Z += hoverForce;
            #endregion

            #region Vertical Attractor
            /* vertical attractor is a angular motor 
            VEHICLE_FLAG_LIMIT_ROLL_ONLY affects this one to be only affected on roll axis
            */
            Vector3 angularPos = angularOrientaton.GetEulerAngles();
            Vector3 vertAttractorTorque;
            vertAttractorTorque = angularPos * m_Params[VehicleFloatParamId.VerticalAttractionEfficiency] * m_Params[VehicleFloatParamId.VerticalAttractionTimescale] * dt;
            if((flags & VehicleFlags.LimitRollOnly) !=0)
            {
                vertAttractorTorque.Y = 0;
                vertAttractorTorque.Z = 0;
            }
            angularTorque += vertAttractorTorque;
            #endregion

            #region Linear Wind Affector
            Vector3 windvelocity = scene.Environment.Wind[pos];
            linearForce += (windvelocity - velocity).ElementMultiply(m_Params[VehicleVectorParamId.LinearWindEfficiency]) * dt;
            #endregion

            #region Angular Wind Affector
            windvelocity = new Vector3(-windvelocity.Y, windvelocity.X, 0);

            if (angularVelocity.X * windvelocity.X >= 0 &&
                angularVelocity.X.PosIfNotNeg() * (angularVelocity.X - windvelocity.X) > 0)
            {
                windvelocity.X = 0;
            }

            if (angularVelocity.Y * windvelocity.Y >= 0 &&
                angularVelocity.Y.PosIfNotNeg() * (angularVelocity.Y - windvelocity.Y) > 0)
            {
                windvelocity.Y = 0;
            }

            AngularTorque += scene.Environment.Wind[pos].ElementMultiply(m_Params[VehicleVectorParamId.AngularWindEfficiency]) * dt;
            #endregion

            #region Banking Motor
            angularTorque.Z -= (AngularTorque.X * ((double)1).Lerp(velocity.X, m_Params[VehicleFloatParamId.BankingMix])) * m_Params[VehicleFloatParamId.BankingEfficiency] * m_Params.OneByBankingTimescale * dt;
            #endregion

            #region Buoyancy
            /* we simply act against the physics effect of the BuoyancyMotor */
            linearForce.Z -= m_Params[VehicleFloatParamId.Buoyancy] * currentState.Mass * CommonPhysicsController.GravityAccelerationConstant;
            #endregion

            #region Angular Deflection
            /* Angular deflection reorients the vehicle to the velocity vector */
            Vector3 deflect = Quaternion.RotBetween(Vector3.UnitX, velocity).AsVector3;
            angularTorque -= (deflect * m_Params[VehicleFloatParamId.AngularDeflectionEfficiency] * m_Params.OneByAngularDeflectionTimescale * dt);
            #endregion

            #region Linear Deflection
            /* Linear deflection deflects the affecting force along the reference x-axis */
            Vector3 naturalVelocity = velocity;
            naturalVelocity = (naturalVelocity.X < 0 ? -Vector3.UnitX : Vector3.UnitX) * naturalVelocity.Length;
            linearForce += (naturalVelocity - velocity) * m_Params[VehicleFloatParamId.LinearDeflectionEfficiency] * m_Params.OneByLinearDeflectionTimescale * dt;
            #endregion  

            #region Motor Decay
            m_Params.DecayDirections(dt);
            #endregion

            LinearForce = linearForce;
            AngularTorque = angularTorque * referenceFrame;
        }

        public Vector3 LinearForce { get; private set; }
        public Vector3 AngularTorque { get; private set; }
    }

    public static class VehicleMotorExtension
    {
        public static VehicleMotor GetMotor(this VehicleParams param)
        {
            return new VehicleMotor(param);
        }
    }
}
