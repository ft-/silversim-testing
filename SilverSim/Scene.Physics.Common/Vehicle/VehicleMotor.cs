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
        double HeightExceededTime;

        internal VehicleMotor(VehicleParams param)
        {
            m_Params = param;
        }

        bool IsMouselookSteerActive(VehicleFlags flags, PhysicsStateData currentState)
        {
            return ((flags & VehicleFlags.MouselookSteer) != 0 && currentState.IsAgentInMouselook) ||
                (flags & VehicleFlags.MousePointSteer) != 0;
        }

        bool IsMouselookBankActive(VehicleFlags flags, PhysicsStateData currentState)
        {
            return ((flags & VehicleFlags.MouselookBank) != 0 && currentState.IsAgentInMouselook) ||
                (flags & VehicleFlags.MousePointBank) != 0;
        }

        public void Process(double dt, PhysicsStateData currentState, SceneInterface scene)
        {
            if(m_Params.VehicleType == VehicleType.None)
            {
                /* disable vehicle */
                LinearForce = Vector3.Zero;
                AngularTorque = Vector3.Zero;
                HeightExceededTime = 0f;
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

            Vector3 mouselookAngularInput = Vector3.Zero;
            if ((flags & (VehicleFlags.MouselookBank | VehicleFlags.MouselookSteer | VehicleFlags.MousePointBank | VehicleFlags.MousePointSteer)) != 0)
            {
                Quaternion localCam = currentState.CameraRotation / m_Params[VehicleRotationParamId.ReferenceFrame];
                mouselookAngularInput = (localCam / angularOrientaton).GetEulerAngles();
                mouselookAngularInput.Y = 0;
                mouselookAngularInput.X = (IsMouselookBankActive(flags, currentState)) ?
                    mouselookAngularInput.Z * m_Params[VehicleFloatParamId.MouselookAltitude] :
                    0;

                if(IsMouselookSteerActive(flags, currentState))
                {
                    mouselookAngularInput.Z *= m_Params[VehicleFloatParamId.MouselookAzimuth];
                }
                else
                {
                    mouselookAngularInput.Z = 0;
                }
            }

            #region Motor Inputs
            linearForce += (m_Params[VehicleVectorParamId.LinearMotorDirection] - velocity).ElementMultiply(m_Params.OneByLinearMotorTimescale * dt);
            angularTorque += (m_Params[VehicleVectorParamId.AngularMotorDirection] - angularVelocity + mouselookAngularInput).ElementMultiply(m_Params.OneByAngularMotorTimescale * dt);

            if((m_Params.Flags & VehicleFlags.TorqueWorldZ) != 0)
            {
                /* translate Z to world (needed for motorcycles based on halcyon design) */
                double angZ = angularTorque.Z;
                angularTorque.Z = 0;
                Quaternion q = Quaternion.CreateFromEulers(0, 0, angZ);
                angularTorque += (q * angularOrientaton).GetEulerAngles();
            }
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


            #region Hover Height Influence Calculation
            double hoverForce;
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
            #endregion

            #region Disable Motor Logic (neat idea based on halcyon simulator)
            double disableMotorsAfter = m_Params[VehicleFloatParamId.DisableMotorsAfter];
            if(disableMotorsAfter > double.Epsilon &&
                m_Params[VehicleFloatParamId.DisableMotorsAbove] < pos.Z - hoverHeight)
            {
                HeightExceededTime += dt;
                if(disableMotorsAfter <= HeightExceededTime)
                {
                    angularTorque = Vector3.Zero;
                    linearForce = Vector3.Zero;
                }
            }
            else
            {
                HeightExceededTime = 0;
            }
            #endregion

            #region Add Hover Height Force
            linearForce.Z += hoverForce;
            #endregion

            #region Friction
            linearForce -= (currentState.Velocity).ElementMultiply(m_Params.OneByLinearFrictionTimescale * dt);
            angularTorque -= (currentState.AngularVelocity).ElementMultiply(m_Params.OneByAngularFrictionTimescale * dt);
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

            if ((flags & (VehicleFlags.ReactToWind | VehicleFlags.ReactToCurrents)) != 0)
            {
                double windCurrentMix;
                double halfBoundBoxSizeZ = currentState.BoundBox.Size.Z / 2;

                if (pos.Z - halfBoundBoxSizeZ > waterHeight || currentState.BoundBox.Size.Z < double.Epsilon)
                {
                    windCurrentMix = 1;
                }
                else if (pos.Z + halfBoundBoxSizeZ < waterHeight)
                {
                    windCurrentMix = 0;
                }
                else
                {
                    windCurrentMix = (pos.Z - halfBoundBoxSizeZ - waterHeight) /
                        currentState.BoundBox.Size.Z;
                }

                if ((flags & VehicleFlags.ReactToWind) != 0 && pos.Z + halfBoundBoxSizeZ > waterHeight)
                {
                    Vector3 windvelocity = scene.Environment.Wind[pos + new Vector3(0, 0, halfBoundBoxSizeZ / 2)];

                    #region Linear Wind Affector
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

                    AngularTorque += windvelocity.ElementMultiply(m_Params[VehicleVectorParamId.AngularWindEfficiency]) * dt * windCurrentMix;
                    #endregion
                }

                if ((flags & VehicleFlags.ReactToCurrents) != 0 && pos.Z - halfBoundBoxSizeZ / 2 < waterHeight)
                {
                    /* yes, wind model also provides current model */
                    Vector3 currentvelocity = scene.Environment.Wind[pos - new Vector3(0, 0, halfBoundBoxSizeZ / 2)];

                    #region Linear Current Affector
                    linearForce += (currentvelocity - velocity).ElementMultiply(m_Params[VehicleVectorParamId.LinearWindEfficiency]) * dt;
                    #endregion

                    #region Angular Current Affector
                    /* works opposite to wind as we are simulating its attacking force below center */
                    currentvelocity = new Vector3(currentvelocity.Y, -currentvelocity.X, 0);

                    if (angularVelocity.X * currentvelocity.X >= 0 &&
                        angularVelocity.X.PosIfNotNeg() * (angularVelocity.X - currentvelocity.X) > 0)
                    {
                        currentvelocity.X = 0;
                    }

                    if (angularVelocity.Y * currentvelocity.Y >= 0 &&
                        angularVelocity.Y.PosIfNotNeg() * (angularVelocity.Y - currentvelocity.Y) > 0)
                    {
                        currentvelocity.Y = 0;
                    }

                    AngularTorque += currentvelocity.ElementMultiply(m_Params[VehicleVectorParamId.AngularWindEfficiency]) * dt * (1 - windCurrentMix);
                    #endregion
                }
            }

            #region Banking Motor
            double invertedBankModifier = 1f;
            if((Vector3.UnitZ * angularOrientaton).Z < 0)
            {
                invertedBankModifier = m_Params[VehicleFloatParamId.InvertedBankingModifier];
            }
            angularTorque.Z -= (AngularTorque.X * ((double)1).Lerp(velocity.X, m_Params[VehicleFloatParamId.BankingMix])) * m_Params[VehicleFloatParamId.BankingEfficiency] * invertedBankModifier * m_Params.OneByBankingTimescale * dt;
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
            Vector3 linearDeflect;
            naturalVelocity = (naturalVelocity.X < 0 ? -Vector3.UnitX : Vector3.UnitX) * naturalVelocity.Length;
            linearDeflect = (naturalVelocity - velocity) * m_Params[VehicleFloatParamId.LinearDeflectionEfficiency] * m_Params.OneByLinearDeflectionTimescale * dt;

            if((flags & VehicleFlags.NoDeflectionUp) != 0 && linearDeflect.Z >= 0)
            {
                linearDeflect.Z = 0;
            }
            linearForce += linearDeflect;
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
