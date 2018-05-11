﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System;

namespace SilverSim.Scene.Physics.Common.Vehicle
{
    public class VehicleMotor
    {
        private readonly VehicleParams m_Params;
        private double HeightExceededTime;

        public Vector3 LinearMotorForce { get; private set; }
        public Vector3 AngularMotorTorque { get; private set; }
        public Vector3 WorldZTorque { get; private set; }
        public double HoverMotorForce { get; private set; }
        public Vector3 LinearFrictionForce { get; private set; }
        public Vector3 AngularFrictionTorque { get; private set; }
        public Vector3 VerticalAttractorTorque { get; private set; }
        public Vector3 LinearWindForce { get; private set; }
        public Vector3 AngularWindTorque { get; private set; }
        public Vector3 LinearCurrentForce { get; private set; }
        public Vector3 AngularCurrentTorque { get; private set; }
        public double BankingTorque { get; private set; }
        public Vector3 AngularDeflectionTorque { get; private set; }
        public Vector3 LinearDeflectionForce { get; private set; }

        internal VehicleMotor(VehicleParams param)
        {
            m_Params = param;
        }

        private bool IsMouselookSteerActive(VehicleFlags flags, PhysicsStateData currentState) =>
            ((flags & VehicleFlags.MouselookSteer) != 0 && currentState.IsAgentInMouselook) ||
                (flags & VehicleFlags.MousePointSteer) != 0;

        private bool IsMouselookBankActive(VehicleFlags flags, PhysicsStateData currentState) =>
            ((flags & VehicleFlags.MouselookBank) != 0 && currentState.IsAgentInMouselook) ||
                (flags & VehicleFlags.MousePointBank) != 0;

        public void Process(double dt, PhysicsStateData currentState, SceneInterface scene, double mass, double gravityConstant)
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
                Quaternion localCam = currentState.CameraRotation / referenceFrame;
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
            Vector3 linearMotorDirection = m_Params[VehicleVectorParamId.LinearMotorDirection];
            Vector3 angularMotorDirection = m_Params[VehicleVectorParamId.AngularMotorDirection] + mouselookAngularInput;
            Vector3 linearMotorError = linearMotorDirection - velocity;
            Vector3 angularMotorError = angularMotorDirection - m_Params[VehicleVectorParamId.AngularMotorDirection] - angularVelocity;

            Vector3 oneByLinearMotorTimescale = Vector3.Zero;
            Vector3 oneByAngularMotorTimescale = Vector3.Zero;

            #region Linear motor timescales
            Vector3 oneByLinearMotorAccelPosTimescale = m_Params.OneByLinearMotorAccelPosTimescale;
            Vector3 oneByLinearMotorDecelPosTimescale = m_Params.OneByLinearMotorDecelPosTimescale;
            Vector3 oneByLinearMotorAccelNegTimescale = m_Params.OneByLinearMotorAccelNegTimescale;
            Vector3 oneByLinearMotorDecelNegTimescale = m_Params.OneByLinearMotorDecelNegTimescale;

            if (linearMotorDirection.X * velocity.X < 0 && Math.Abs(linearMotorDirection.X) > double.Epsilon)
            {
                oneByLinearMotorTimescale.X = linearMotorDirection.X < 0 ? oneByLinearMotorAccelNegTimescale.X : oneByLinearMotorAccelPosTimescale.X;
            }
            else if(Math.Abs(linearMotorDirection.X) <= double.Epsilon)
            {
                oneByLinearMotorTimescale.X = velocity.X < 0 ? oneByLinearMotorDecelNegTimescale.X : oneByLinearMotorDecelPosTimescale.X;
            }
            else if(linearMotorDirection.X >= 0)
            {
                oneByLinearMotorTimescale.X = linearMotorError.X * velocity.X < 0 ? oneByLinearMotorDecelPosTimescale.X : oneByLinearMotorAccelPosTimescale.X;
            }
            else
            {
                oneByLinearMotorTimescale.X = linearMotorError.X * velocity.X < 0 ? oneByLinearMotorDecelNegTimescale.X : oneByLinearMotorAccelNegTimescale.X;
            }

            if (linearMotorDirection.Y * velocity.Y < 0 && Math.Abs(linearMotorDirection.Y) > double.Epsilon)
            {
                oneByLinearMotorTimescale.Y = linearMotorDirection.Y < 0 ? oneByLinearMotorAccelNegTimescale.Y : oneByLinearMotorAccelPosTimescale.Y;
            }
            else if (Math.Abs(linearMotorDirection.Y) <= double.Epsilon)
            {
                oneByLinearMotorTimescale.Y = velocity.Y < 0 ? oneByLinearMotorDecelNegTimescale.Y : oneByLinearMotorDecelPosTimescale.Y;
            }
            else if (linearMotorDirection.Y >= 0)
            {
                oneByLinearMotorTimescale.Y = linearMotorError.Y * velocity.Y < 0 ? oneByLinearMotorDecelPosTimescale.Y : oneByLinearMotorAccelPosTimescale.Y;
            }
            else
            {
                oneByLinearMotorTimescale.Y = linearMotorError.Y * velocity.Y < 0 ? oneByLinearMotorDecelNegTimescale.Y : oneByLinearMotorAccelNegTimescale.Y;
            }

            if (linearMotorDirection.Z * velocity.Z < 0 && Math.Abs(linearMotorDirection.Z) > double.Epsilon)
            {
                oneByLinearMotorTimescale.Z = linearMotorDirection.Z < 0 ? oneByLinearMotorAccelNegTimescale.Z : oneByLinearMotorAccelPosTimescale.Z;
            }
            else if (Math.Abs(linearMotorDirection.Z) <= double.Epsilon)
            {
                oneByLinearMotorTimescale.Z = velocity.Z < 0 ? oneByLinearMotorDecelNegTimescale.Z : oneByLinearMotorDecelPosTimescale.Z;
            }
            else if (linearMotorDirection.Z >= 0)
            {
                oneByLinearMotorTimescale.Z = linearMotorError.Z * velocity.Z < 0 ? oneByLinearMotorDecelPosTimescale.Z : oneByLinearMotorAccelPosTimescale.Z;
            }
            else
            {
                oneByLinearMotorTimescale.Z = linearMotorError.Z * velocity.Z < 0 ? oneByLinearMotorDecelNegTimescale.Z : oneByLinearMotorAccelNegTimescale.Z;
            }
            #endregion

            #region Angular motor timescales
            Vector3 oneByAngularMotorAccelPosTimescale = m_Params.OneByAngularMotorAccelPosTimescale;
            Vector3 oneByAngularMotorDecelPosTimescale = m_Params.OneByAngularMotorDecelPosTimescale;
            Vector3 oneByAngularMotorAccelNegTimescale = m_Params.OneByAngularMotorAccelNegTimescale;
            Vector3 oneByAngularMotorDecelNegTimescale = m_Params.OneByAngularMotorDecelNegTimescale;

            if (angularMotorDirection.X * angularVelocity.X < 0 && Math.Abs(angularMotorDirection.X) > double.Epsilon)
            {
                oneByAngularMotorTimescale.X = angularMotorDirection.X < 0 ? oneByAngularMotorAccelNegTimescale.X : oneByAngularMotorAccelPosTimescale.X;
            }
            else if (Math.Abs(angularMotorDirection.X) <= double.Epsilon)
            {
                oneByAngularMotorTimescale.X = angularVelocity.X < 0 ? oneByAngularMotorDecelNegTimescale.X : oneByAngularMotorDecelPosTimescale.X;
            }
            else if (angularMotorDirection.X >= 0)
            {
                oneByAngularMotorTimescale.X = angularMotorError.X * angularVelocity.X < 0 ? oneByAngularMotorDecelPosTimescale.X : oneByAngularMotorAccelPosTimescale.X;
            }
            else
            {
                oneByAngularMotorTimescale.X = angularMotorError.X * angularVelocity.X < 0 ? oneByAngularMotorDecelNegTimescale.X : oneByAngularMotorAccelNegTimescale.X;
            }

            if (angularMotorDirection.Y * angularVelocity.Y < 0 && Math.Abs(angularMotorDirection.Y) > double.Epsilon)
            {
                oneByAngularMotorTimescale.Y = angularMotorDirection.Y < 0 ? oneByAngularMotorAccelNegTimescale.Y : oneByAngularMotorAccelPosTimescale.Y;
            }
            else if (Math.Abs(angularMotorDirection.Y) <= double.Epsilon)
            {
                oneByAngularMotorTimescale.Y = angularVelocity.Y < 0 ? oneByAngularMotorDecelNegTimescale.Y : oneByAngularMotorDecelPosTimescale.Y;
            }
            else if (angularMotorDirection.Y >= 0)
            {
                oneByAngularMotorTimescale.Y = angularMotorError.Y * angularVelocity.Y < 0 ? oneByAngularMotorDecelPosTimescale.Y : oneByAngularMotorAccelPosTimescale.Y;
            }
            else
            {
                oneByAngularMotorTimescale.Y = angularMotorError.Y * angularVelocity.Y < 0 ? oneByAngularMotorDecelNegTimescale.Y : oneByAngularMotorAccelNegTimescale.Y;
            }

            if (angularMotorDirection.Z * angularVelocity.Z < 0 && Math.Abs(angularMotorDirection.Z) > double.Epsilon)
            {
                oneByAngularMotorTimescale.Z = angularMotorDirection.Z < 0 ? oneByAngularMotorAccelNegTimescale.Z : oneByAngularMotorAccelPosTimescale.Z;
            }
            else if (Math.Abs(angularMotorDirection.Z) <= double.Epsilon)
            {
                oneByAngularMotorTimescale.Z = angularVelocity.Z < 0 ? oneByAngularMotorDecelNegTimescale.Z : oneByAngularMotorDecelPosTimescale.Z;
            }
            else if (angularMotorDirection.Z >= 0)
            {
                oneByAngularMotorTimescale.Z = angularMotorError.Z * angularVelocity.Z < 0 ? oneByAngularMotorDecelPosTimescale.Z : oneByAngularMotorAccelPosTimescale.Z;
            }
            else
            {
                oneByAngularMotorTimescale.Z = angularMotorError.Z * angularVelocity.Z < 0 ? oneByAngularMotorDecelNegTimescale.Z : oneByAngularMotorAccelNegTimescale.Z;
            }
            #endregion

            linearForce += LinearMotorForce = (m_Params[VehicleVectorParamId.LinearMotorDirection] - velocity).ElementMultiply(oneByLinearMotorTimescale * dt);
            angularTorque += AngularMotorTorque = (m_Params[VehicleVectorParamId.AngularMotorDirection] - angularVelocity + mouselookAngularInput).ElementMultiply(oneByAngularMotorTimescale * dt);

            if((m_Params.Flags & VehicleFlags.TorqueWorldZ) != 0)
            {
                /* translate Z to world (needed for motorcycles based on halcyon design) */
                double angZ = angularTorque.Z;
                angularTorque.Z = 0;
                Quaternion q = Quaternion.CreateFromEulers(0, 0, angZ);
                angularTorque += WorldZTorque = (q * angularOrientaton).GetEulerAngles();
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
            double paramHoverHeight = m_Params[VehicleFloatParamId.HoverHeight];
            double hoverHeight = scene.Terrain[pos];
            double waterHeight = scene.RegionSettings.WaterHeight;

            if ((flags & VehicleFlags.HoverGlobalHeight) != 0)
            {
                hoverHeight = paramHoverHeight;
                if(hoverHeight < waterHeight)
                {
                    hoverHeight = waterHeight;
                }
            }
            else
            {
                paramHoverHeight = Math.Min(100, paramHoverHeight);
                hoverHeight += paramHoverHeight;
                double waterHoverHeight = waterHeight + paramHoverHeight;

                if ((flags & VehicleFlags.HoverWaterOnly) != 0 ||
                    ((flags & VehicleFlags.HoverTerrainOnly) == 0 &&
                    hoverHeight < waterHoverHeight))
                {
                    hoverHeight = waterHoverHeight;
                }
            }

            if (Math.Abs(hoverHeight) > double.Epsilon && m_Params.IsHoverMotorEnabled)
            {
                /* The definition does not include negative hover height.
                 * But since we are allowing negative terain height, it makes an useful feature.
                 */
                hoverForce = ((hoverHeight + 0.21728 - pos.Z) - velocity.Z) * m_Params[VehicleFloatParamId.HoverEfficiency] * m_Params.OneByHoverTimescale * dt;
                if ((m_Params.Flags & VehicleFlags.HoverUpOnly) != 0 && hoverForce < 0)
                {
                    hoverForce = 0;
                }
            }
            else
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
            linearForce.Z += HoverMotorForce = hoverForce;
            #endregion

            #region Friction
            linearForce -= LinearFrictionForce = (currentState.Velocity).ElementMultiply(m_Params.OneByLinearFrictionTimescale * dt);
            angularTorque -= AngularFrictionTorque = (currentState.AngularVelocity).ElementMultiply(m_Params.OneByAngularFrictionTimescale * dt);
            #endregion

            #region Vertical Attractor
            /* vertical attractor is a angular motor 
            VEHICLE_FLAG_LIMIT_ROLL_ONLY affects this one to be only affected on roll axis
            */
            Vector3 vaTimescale = m_Params[VehicleVectorParamId.AngularDeflectionTimescale];
            if (vaTimescale.X < 300 || vaTimescale.Y < 300)
            {
                Vector3 upwardDirection = Vector3.UnitZ * angularOrientaton;
                Vector3 forwardDirection = Vector3.UnitX * angularOrientaton;
                double roll = Math.Atan2(upwardDirection.Y, upwardDirection.Z);
                double pitch;
                if(upwardDirection.Y < 0)
                {
                    pitch = Math.Atan2(-forwardDirection.Y, forwardDirection.X);
                }
                else
                {
                    pitch = Math.Atan2(forwardDirection.Y, forwardDirection.X);
                }
                Vector3 angularError = new Vector3(roll, pitch, 0) - angularVelocity;
                Vector3 vertAttractorTorque = angularError.ElementMultiply(m_Params[VehicleVectorParamId.VerticalAttractionEfficiency].ElementMultiply(m_Params.OneByVerticalAttractionTimescale) * dt);

                if (vaTimescale.X < 300)
                {
                    double rollboundary = Math.Min(Math.Abs(roll), Math.Abs(vertAttractorTorque.X));
                    angularTorque.X = angularTorque.X + vertAttractorTorque.X.Clamp(-rollboundary, rollboundary);
                }
                else
                {
                    vertAttractorTorque.X = 0;
                }

                if ((flags & VehicleFlags.LimitRollOnly) == 0 && vaTimescale.Y < 300)
                {
                    double pitchboundary = Math.Min(Math.Abs(pitch), Math.Abs(vertAttractorTorque.Y));
                    angularTorque.Y = angularTorque.Y + vertAttractorTorque.Y.Clamp(-pitchboundary, pitchboundary);
                }
                else
                {
                    vertAttractorTorque.Y = 0;
                }
                VerticalAttractorTorque = vertAttractorTorque;
            }
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
                    linearForce += LinearWindForce = (windvelocity - velocity).ElementMultiply(m_Params[VehicleVectorParamId.LinearWindEfficiency]) * dt;
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

                    AngularTorque += AngularWindTorque = windvelocity.ElementMultiply(m_Params[VehicleVectorParamId.AngularWindEfficiency]) * dt * windCurrentMix;
                    #endregion
                }

                if ((flags & VehicleFlags.ReactToCurrents) != 0 && pos.Z - halfBoundBoxSizeZ / 2 < waterHeight)
                {
                    /* yes, wind model also provides current model */
                    Vector3 currentvelocity = scene.Environment.Wind[pos - new Vector3(0, 0, halfBoundBoxSizeZ / 2)];

                    #region Linear Current Affector
                    linearForce += LinearCurrentForce = (currentvelocity - velocity).ElementMultiply(m_Params[VehicleVectorParamId.LinearWindEfficiency]) * dt;
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

                    AngularTorque += AngularCurrentTorque = currentvelocity.ElementMultiply(m_Params[VehicleVectorParamId.AngularWindEfficiency]) * dt * (1 - windCurrentMix);
                    #endregion
                }
            }

            #region Banking Motor
            if (m_Params[VehicleVectorParamId.VerticalAttractionTimescale].X < 300)
            {
                double invertedBankModifier = 1f;
                if ((Vector3.UnitZ * angularOrientaton).Z < 0)
                {
                    invertedBankModifier = m_Params[VehicleFloatParamId.InvertedBankingModifier];
                }
                angularTorque.X -= BankingTorque = (AngularTorque.Z * 1.0.Mix(velocity.X, m_Params[VehicleFloatParamId.BankingMix])) * m_Params[VehicleFloatParamId.BankingEfficiency] * invertedBankModifier * m_Params.OneByBankingTimescale * dt;
            }
            #endregion

            #region Angular Deflection
            /* Angular deflection reorients the vehicle to the velocity vector */
            Vector3 deflect = velocity * Math.Sign(velocity.X);
            if(Math.Abs(deflect.X) < 0.01)
            {
                deflect.X = 0.01;
                deflect.Y = velocity.Y;
                deflect.Z = velocity.Z;
            }
            Vector3 angdeflecteff = (m_Params[VehicleVectorParamId.AngularDeflectionEfficiency].ElementMultiply(m_Params.OneByAngularDeflectionTimescale) * dt).ComponentMin(1);
            Vector3 angularDeflectionTorque = Vector3.Zero;
            if(Math.Abs(deflect.Z) > 0.01)
            {
                angularDeflectionTorque.Y = -Math.Atan2(deflect.Z, deflect.X) * angdeflecteff.Y;
            }
            if(Math.Abs(deflect.Y) > 0.01)
            {
                angularDeflectionTorque.Z = -Math.Atan2(deflect.Y, deflect.X) * angdeflecteff.Z;
            }
            AngularDeflectionTorque = angularDeflectionTorque;
            angularTorque += angularDeflectionTorque;
            #endregion

            #region Linear Deflection
            /* Linear deflection deflects the affecting force along the reference x-axis */
            var eulerDiff = new Vector3
            {
                Z = -Math.Atan2(velocity.Y, velocity.X)
            };
            if ((flags & VehicleFlags.NoDeflectionUp) != 0)
            {
                eulerDiff.Y = -Math.Atan2(velocity.Z, velocity.X);
            }

            eulerDiff = eulerDiff.ElementMultiply((m_Params[VehicleVectorParamId.LinearDeflectionEfficiency].ElementMultiply(m_Params.OneByLinearDeflectionTimescale) * dt).ComponentMin(1));

            linearForce += LinearDeflectionForce = velocity * Quaternion.CreateFromEulers(eulerDiff) - velocity;
            #endregion

            #region Motor Decay
            m_Params.DecayDirections(dt);
            #endregion

            #region Buoyancy
            /* we simply act against the physics effect of the BuoyancyMotor */
            linearForce.Z += m_Params[VehicleFloatParamId.Buoyancy] * mass * gravityConstant;
            #endregion

            LinearForce = linearForce;
            AngularTorque = angularTorque * referenceFrame;
        }

        public Vector3 LinearForce { get; private set; }
        public Vector3 AngularTorque { get; private set; }
    }

    public static class VehicleMotorExtension
    {
        public static VehicleMotor GetMotor(this VehicleParams param) => new VehicleMotor(param);
    }
}
