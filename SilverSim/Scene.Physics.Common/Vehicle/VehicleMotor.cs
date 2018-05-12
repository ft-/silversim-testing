// SilverSim is distributed under the terms of the
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
using System.Threading;

namespace SilverSim.Scene.Physics.Common.Vehicle
{
    public class VehicleMotor
    {
        private readonly VehicleParams m_Params;

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
            LinearForce = Vector3.Zero;
            AngularTorque = Vector3.Zero;
            if (m_Params.VehicleType == VehicleType.None)
            {
                /* disable vehicle */
                m_Params[VehicleFloatParamId.HeightExceededTime] = 0;
                return;
            }

            VehicleFlags flags = m_Params.Flags;
            Vector3 linearBodyForce = Vector3.Zero;
            Vector3 angularBodyTorque = Vector3.Zero;

            #region Transform Reference Frame
            Quaternion referenceFrame = m_Params[VehicleRotationParamId.ReferenceFrame];
            Vector3 velocity = currentState.Velocity / referenceFrame;
            Vector3 angularVelocity = currentState.AngularVelocity / referenceFrame;
            Quaternion angularOrientation = currentState.Rotation / referenceFrame;
            #endregion

            Vector3 mouselookAngularInput = Vector3.Zero;
            if ((flags & (VehicleFlags.MouselookBank | VehicleFlags.MouselookSteer | VehicleFlags.MousePointBank | VehicleFlags.MousePointSteer)) != 0)
            {
                Quaternion localCam = currentState.CameraRotation / referenceFrame;
                mouselookAngularInput = (localCam / angularOrientation).GetEulerAngles();
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

            #region MoveToTarget inputs
            Vector3 targetPos = Vector3.Zero;
            Vector3 linearTargetDirection = Vector3.Zero;
            Vector3 angularTargetDirection = Vector3.Zero;
            TargetList targetList = m_TargetList;
            if(targetList?.TryGetTarget(out targetPos) ?? false)
            {
                Vector3 linearError = targetPos - currentState.Position;
                Vector3 angularError = Quaternion.RotBetween(Vector3.UnitX, linearError).GetEulerAngles();
                Vector3 linearEpsilon = m_Params[VehicleVectorParamId.LinearMoveToTargetEpsilon];
                Vector3 angularEpsilon = m_Params[VehicleVectorParamId.AngularMoveToTargetEpsilon];
                Vector3 linearMaxOutput = m_Params[VehicleVectorParamId.LinearMoveToTargetMaxOutput];
                Vector3 angularMaxOutput = m_Params[VehicleVectorParamId.AngularMoveToTargetMaxOutput];

                bool linearTargetReached = (Math.Abs(linearError.X) < linearEpsilon.X || linearEpsilon.X < 0) &&
                    (Math.Abs(linearError.Y) < linearEpsilon.Y || linearEpsilon.Y < 0) &&
                    (Math.Abs(linearError.Z) < linearEpsilon.Z || linearEpsilon.Z < 0);
                bool angularTargetReached = (Math.Abs(angularError.X) < angularEpsilon.X || angularEpsilon.X < 0) &&
                    (Math.Abs(angularError.Y) < angularEpsilon.Y || angularEpsilon.Y < 0) &&
                    (Math.Abs(angularError.Z) < angularEpsilon.Z || angularEpsilon.Z < 0);

                /* limiters */
                linearError = linearError.ComponentClamp(-linearMaxOutput, linearMaxOutput);
                angularError = angularError.ComponentClamp(-angularMaxOutput, angularMaxOutput);

                /* timescalers */
                linearTargetDirection = linearError.ElementMultiply((m_Params[VehicleVectorParamId.LinearMoveToTargetEfficiency].ElementMultiply(m_Params.OneByLinearMoveToTargetTimescale) * dt).ComponentMin(1));
                angularTargetDirection = angularError.ElementMultiply((m_Params[VehicleVectorParamId.AngularMoveToTargetEfficiency].ElementMultiply(m_Params.OneByAngularMoveToTargetTimescale) * dt).ComponentMin(1));

                if (linearTargetReached && angularTargetReached && !m_TargetList.NextTarget() &&
                    (m_Params.Flags & VehicleFlags.StopMoveToTargetAtEnd) != 0)
                {
                    /* remove target list if not already changed */
                    Interlocked.CompareExchange(ref m_TargetList, null, targetList);
                }
            }
            #endregion

            #region Motor Inputs
            Vector3 linearMotorDirection = m_Params[VehicleVectorParamId.LinearMotorDirection] + linearTargetDirection;
            Vector3 angularMotorDirection = m_Params[VehicleVectorParamId.AngularMotorDirection] + mouselookAngularInput + angularTargetDirection;
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

            linearBodyForce += LinearMotorForce = (m_Params[VehicleVectorParamId.LinearMotorDirection] - velocity).ElementMultiply(oneByLinearMotorTimescale * dt);
            angularBodyTorque += AngularMotorTorque = (m_Params[VehicleVectorParamId.AngularMotorDirection] - angularVelocity + mouselookAngularInput).ElementMultiply(oneByAngularMotorTimescale * dt);

            if((m_Params.Flags & VehicleFlags.TorqueWorldZ) != 0)
            {
                /* translate Z to world (needed for motorcycles based on halcyon design) */
                double angZ = angularBodyTorque.Z;
                angularBodyTorque.Z = 0;
                Quaternion q = Quaternion.CreateFromEulers(0, 0, angZ);
                angularBodyTorque += WorldZTorque = (q * angularOrientation).GetEulerAngles();
            }
            #endregion

            #region Motor Limiting
            if ((m_Params.Flags & VehicleFlags.LimitMotorDown) != 0 && linearBodyForce.Z < 0)
            {
                linearBodyForce.Z = 0;
            }
            if ((m_Params.Flags & VehicleFlags.LimitMotorUp) != 0 && linearBodyForce.Z > 0)
            {
                linearBodyForce.Z = 0;
            }
            #endregion

            #region Hover Height Influence Calculation
            double hoverForce;
            Vector3 pos = currentState.Position;
            double paramHoverHeight = m_Params[VehicleFloatParamId.HoverHeight];
            double hoverHeight = 0;
            double terrainHeight = scene.Terrain[pos];
            double waterHeight = scene.RegionSettings.WaterHeight;

            if ((flags & VehicleFlags.HoverGlobalHeight) != 0)
            {
                hoverHeight = Math.Max(paramHoverHeight, terrainHeight);
            }
            else
            {
                paramHoverHeight = Math.Min(100, paramHoverHeight);
                hoverHeight = terrainHeight + paramHoverHeight;
                double waterHoverHeight = waterHeight + paramHoverHeight;

                if ((flags & VehicleFlags.HoverWaterOnly) != 0 ||
                    ((flags & VehicleFlags.HoverTerrainOnly) == 0 &&
                    hoverHeight < waterHoverHeight))
                {
                    hoverHeight = waterHoverHeight;
                }
            }

            if (m_Params.IsHoverMotorEnabled)
            {
                /* The definition does not include negative hover height.
                 * But since we are allowing negative terain height, it makes an useful feature.
                 */
                double hoverVelocity = (hoverHeight + 0.21728 - pos.Z) - velocity.Z;
                if((m_Params.Flags & VehicleFlags.HoverUpOnly) != 0 && hoverVelocity < 0)
                {
                    hoverVelocity = 0;
                }
                hoverForce = hoverVelocity * m_Params[VehicleFloatParamId.HoverEfficiency] * m_Params.OneByHoverTimescale * dt;
            }
            else
            {
                hoverForce = 0;
            }
            #endregion

            #region Disable Motor Logic (neat idea based on halcyon simulator)
            double disableMotorsAfter = m_Params[VehicleFloatParamId.DisableMotorsAfter];
            double heightExceededTime = m_Params[VehicleFloatParamId.HeightExceededTime];
            if (disableMotorsAfter > double.Epsilon &&
                m_Params[VehicleFloatParamId.DisableMotorsAbove] < pos.Z - hoverHeight)
            {
                heightExceededTime += dt;
                m_Params[VehicleFloatParamId.HeightExceededTime] = dt;
                if(disableMotorsAfter <= heightExceededTime)
                {
                    angularBodyTorque = Vector3.Zero;
                    linearBodyForce = Vector3.Zero;
                }
            }
            else
            {
                m_Params[VehicleFloatParamId.HeightExceededTime] = 0;
            }
            #endregion

            #region Add Hover Height Force
            linearBodyForce.Z += HoverMotorForce = hoverForce;
            #endregion

            #region Friction
            linearBodyForce += LinearFrictionForce = (-currentState.Velocity).ElementMultiply((m_Params.OneByLinearFrictionTimescale * dt).ComponentMin(1));
            angularBodyTorque += AngularFrictionTorque = (-currentState.AngularVelocity).ElementMultiply((m_Params.OneByAngularFrictionTimescale * dt).ComponentMin(1));
            #endregion

            #region Vertical Attractor
            /* vertical attractor is a angular motor 
            VEHICLE_FLAG_LIMIT_ROLL_ONLY affects this one to be only affected on roll axis
            */
            Vector3 vaTimescale = m_Params[VehicleVectorParamId.VerticalAttractionTimescale];
            double roll = 0;
            if (vaTimescale.X < 300 || vaTimescale.Y < 300)
            {
                Vector3 angularError = angularOrientation.GetEulerAngles();
                if(angularError.X > Math.PI)
                {
                    angularError.X -= Math.PI;
                }
                if (angularError.Y > Math.PI)
                {
                    angularError.Y -= Math.PI;
                }
                roll = angularError.X;
                angularError = -angularError-angularVelocity;
                Vector3 vertAttractorTorque = angularError.ElementMultiply((m_Params[VehicleVectorParamId.VerticalAttractionEfficiency].ElementMultiply(m_Params.OneByVerticalAttractionTimescale) * dt).ComponentMin(1));

                if (vaTimescale.X < 300)
                {
                    angularBodyTorque.X = angularBodyTorque.X + vertAttractorTorque.X;
                }
                else
                {
                    vertAttractorTorque.X = 0;
                }

                if ((flags & VehicleFlags.LimitRollOnly) == 0 && vaTimescale.Y < 300)
                {
                    angularBodyTorque.Y = angularBodyTorque.Y + vertAttractorTorque.Y;
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
                    linearBodyForce += LinearWindForce = (windvelocity - velocity).ElementMultiply(m_Params[VehicleVectorParamId.LinearWindEfficiency]) * dt;
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
                    linearBodyForce += LinearCurrentForce = (currentvelocity - velocity).ElementMultiply(m_Params[VehicleVectorParamId.LinearWindEfficiency]) * dt;
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
                if ((Vector3.UnitZ * angularOrientation).Z < 0)
                {
                    invertedBankModifier = m_Params[VehicleFloatParamId.InvertedBankingModifier];
                }
                angularBodyTorque.Z += BankingTorque = (roll * 1.0.Mix(velocity.X, m_Params[VehicleFloatParamId.BankingMix])) * m_Params[VehicleFloatParamId.BankingEfficiency] * invertedBankModifier * m_Params.OneByBankingTimescale * dt;
            }
            #endregion

            #region Angular Deflection
            /* Angular deflection reorients the vehicle to the velocity vector */
            Vector3 deflect = velocity;
            if(deflect.X < 0)
            {
                deflect = -deflect;
            }
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
                angularDeflectionTorque.Y = Math.Atan2(deflect.Z, deflect.X) * angdeflecteff.Y;
            }
            if(Math.Abs(deflect.Y) > 0.01)
            {
                angularDeflectionTorque.Z = Math.Atan2(deflect.Y, deflect.X) * angdeflecteff.Z;
            }
            AngularDeflectionTorque = angularDeflectionTorque;
            angularBodyTorque += angularDeflectionTorque;
            #endregion

            #region Linear Deflection
            /* Linear deflection deflects the affecting force along the reference x-axis */
            var eulerDiff = new Vector3
            {
                Z = Math.Atan2(velocity.Y, velocity.X)
            };
            if ((flags & VehicleFlags.NoDeflectionUp) != 0)
            {
                eulerDiff.Y = Math.Atan2(velocity.Z, velocity.X);
            }

            eulerDiff = eulerDiff.ElementMultiply((m_Params[VehicleVectorParamId.LinearDeflectionEfficiency].ElementMultiply(m_Params.OneByLinearDeflectionTimescale) * dt).ComponentMin(1));

            linearBodyForce += LinearDeflectionForce = velocity * Quaternion.CreateFromEulers(eulerDiff) - velocity;
            #endregion

            #region Motor Decay
            m_Params.DecayDirections(dt);
            #endregion

            #region Buoyancy
            /* we simply act against the physics effect of the BuoyancyMotor */
            linearBodyForce.Z += m_Params[VehicleFloatParamId.Buoyancy] * mass * gravityConstant;
            #endregion

            LinearForce += linearBodyForce;
            AngularTorque += angularBodyTorque * referenceFrame;
        }

        public Vector3 LinearForce { get; private set; }
        public Vector3 AngularTorque { get; private set; }

        private sealed class TargetList
        {
            public readonly Vector3[] m_MoveToTargetList;
            private int m_MoveToTargetListPosition;

            public TargetList(Vector3[] targetList)
            {
                if(targetList == null)
                {
                    throw new ArgumentOutOfRangeException();
                }
                m_MoveToTargetList = targetList;
            }

            public bool TryGetTarget(out Vector3 targetPos)
            {
                int pos = m_MoveToTargetListPosition;
                if(pos < m_MoveToTargetList.Length)
                {
                    targetPos = m_MoveToTargetList[pos];
                    return true;
                }
                else
                {
                    targetPos = Vector3.Zero;
                    return false;
                }
            }

            public bool NextTarget() => Interlocked.Increment(ref m_MoveToTargetListPosition) < m_MoveToTargetList.Length;
        }

        private TargetList m_TargetList;

        public bool ActivateTargetList(Vector3[] moveToTargetList)
        {
            if(m_Params.VehicleType == VehicleType.None)
            {
                return false;
            }

            m_TargetList = new TargetList(moveToTargetList);
            return true;
        }
    }

    public static class VehicleMotorExtension
    {
        public static VehicleMotor GetMotor(this VehicleParams param) => new VehicleMotor(param);
    }
}
