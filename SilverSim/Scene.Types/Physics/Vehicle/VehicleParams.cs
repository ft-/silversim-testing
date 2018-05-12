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

using SilverSim.Scene.Types.Object;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SilverSim.Scene.Types.Physics.Vehicle
{
    internal class TimescaleData<T>
    {
        public T Timescale { get; }
        public T OneByTimescale { get; }

        protected TimescaleData(T timescale, T onebytimescale)
        {
            Timescale = timescale;
            OneByTimescale = onebytimescale;
        }

        public static TimescaleData<Vector3> Create(Vector3 value) => new TimescaleData<Vector3>(
            value,
            Vector3.One.ElementDivide(value.ComponentMax(double.Epsilon)));

        public static TimescaleData<double> Create(double value) => new TimescaleData<double>(
            value,
            1.0 / Math.Max(double.Epsilon, value));
    }

    internal sealed class TimescaleVectorData : TimescaleData<Vector3>
    {
        private TimescaleVectorData(Vector3 timescale)
            : base(timescale, Vector3.One.ElementDivide(timescale.ComponentMax(double.Epsilon)))
        {
        }

        public static implicit operator TimescaleVectorData(Vector3 timescale) => new TimescaleVectorData(timescale);
        public static implicit operator TimescaleVectorData(double timescale) => new TimescaleVectorData(new Vector3(timescale));
    }

    internal sealed class TimescaleDoubleData : TimescaleData<double>
    {
        private TimescaleDoubleData(double timescale)
            : base(timescale, 1.0 / Math.Max(double.Epsilon, timescale))
        {
        }

        public static implicit operator TimescaleDoubleData(double timescale) => new TimescaleDoubleData(timescale);
    }

    public sealed class VehicleParams
    {
        private readonly ObjectPart m_Part;

        private sealed class ReferenceVectorBoxed
        {
            private readonly Vector3 Value;

            public ReferenceVectorBoxed()
            {
            }

            public ReferenceVectorBoxed(Vector3 value)
            {
                Value = value;
            }

            public ReferenceVectorBoxed(double value)
            {
                Value = new Vector3(value);
            }

            public static implicit operator Vector3(ReferenceVectorBoxed reference) => reference?.Value ?? default(Vector3);
            public static implicit operator ReferenceVectorBoxed(Vector3 value) => new ReferenceVectorBoxed(value);
            public static implicit operator ReferenceVectorBoxed(double value) => new ReferenceVectorBoxed(value);
        }

        public VehicleParams(ObjectPart part)
        {
            m_Part = part;
        }

        public VehicleParams(ObjectPart part, VehicleParams src)
        {
            m_Part = part;
            m_VehicleType = src.m_VehicleType;
            m_ReferenceFrame = src.m_ReferenceFrame;
            m_AngularFrictionTimescale = src.m_AngularFrictionTimescale;
            m_AngularMotorDirection = src.m_AngularMotorDirection;
            m_LinearFrictionTimescale = src.m_LinearFrictionTimescale;
            m_LinearMotorDirection = src.m_LinearMotorDirection;
            m_LinearMotorOffset = src.m_LinearMotorOffset;
            m_AngularDeflectionEfficiency = src.m_AngularDeflectionEfficiency;
            m_AngularDeflectionTimescale = src.m_AngularDeflectionTimescale;
            m_AngularMotorDecayTimescale = src.m_AngularMotorDecayTimescale;
            m_AngularMotorAccelPosTimescale = src.m_AngularMotorAccelPosTimescale;
            m_AngularMotorDecelPosTimescale = src.m_AngularMotorDecelPosTimescale;
            m_AngularMotorAccelNegTimescale = src.m_AngularMotorAccelNegTimescale;
            m_AngularMotorDecelNegTimescale = src.m_AngularMotorDecelNegTimescale;
            m_BankingEfficiency = src.m_BankingEfficiency;
            m_BankingMix = src.m_BankingMix;
            m_BankingTimescale = src.m_BankingTimescale;
            m_Buoyancy = src.m_Buoyancy;
            m_HoverHeight = src.m_HoverHeight;
            m_HoverEfficiency = src.m_HoverEfficiency;
            m_HoverTimescale = src.m_HoverTimescale;
            m_LinearDeflectionEfficiency = src.m_LinearDeflectionEfficiency;
            m_LinearDeflectionTimescale = src.m_LinearDeflectionTimescale;
            m_LinearMotorDecayTimescale = src.m_LinearMotorDecayTimescale;
            m_LinearMotorAccelPosTimescale = src.m_LinearMotorAccelPosTimescale;
            m_LinearMotorDecelPosTimescale = src.m_LinearMotorDecelPosTimescale;
            m_LinearMotorAccelNegTimescale = src.m_LinearMotorAccelNegTimescale;
            m_LinearMotorDecelNegTimescale = src.m_LinearMotorDecelNegTimescale;
            m_VerticalAttractionEfficiency = src.m_VerticalAttractionEfficiency;
            m_VerticalAttractionTimescale = src.m_VerticalAttractionTimescale;
            m_FlagsStore = src.m_FlagsStore;
            m_LinearWindEfficiency = src.m_LinearWindEfficiency;
            m_AngularWindEfficiency = src.m_AngularWindEfficiency;
            m_MouselookAzimuth = src.m_MouselookAzimuth;
            m_MouselookAltitude = src.m_MouselookAltitude;
            m_BankingAzimuth = src.m_BankingAzimuth;
            m_DisableMotorsAbove = src.m_DisableMotorsAbove;
            m_DisableMotorsAfter = src.m_DisableMotorsAfter;
            m_InvertedBankingModifier = src.m_InvertedBankingModifier;
        }

        private VehicleType m_VehicleType;

        private ReferenceBoxed<Quaternion> m_ReferenceFrame = Quaternion.Identity;

        private TimescaleVectorData m_AngularFrictionTimescale = 0;
        public Vector3 OneByAngularFrictionTimescale => m_AngularFrictionTimescale.OneByTimescale;

        private ReferenceVectorBoxed m_AngularMotorDirection = 0;
        private TimescaleVectorData m_LinearFrictionTimescale = 0;
        public Vector3 OneByLinearFrictionTimescale => m_LinearFrictionTimescale.OneByTimescale;

        private ReferenceVectorBoxed m_LinearMotorDirection = 0;
        private ReferenceVectorBoxed m_LinearMotorOffset = 0;

        private ReferenceVectorBoxed m_AngularDeflectionEfficiency = 0;
        private TimescaleVectorData m_AngularDeflectionTimescale = 1;
        public Vector3 OneByAngularDeflectionTimescale => m_AngularDeflectionTimescale.OneByTimescale;

        private TimescaleVectorData m_AngularMotorDecayTimescale = 120;
        public Vector3 OneByAngularMotorDecayTimescale => m_AngularMotorDecayTimescale.OneByTimescale;

        private TimescaleVectorData m_AngularMotorAccelPosTimescale = 0;
        public Vector3 OneByAngularMotorAccelPosTimescale => m_AngularMotorAccelPosTimescale.OneByTimescale;

        private TimescaleVectorData m_AngularMotorDecelPosTimescale = 0;
        public Vector3 OneByAngularMotorDecelPosTimescale => m_AngularMotorDecelPosTimescale.OneByTimescale;

        private TimescaleVectorData m_AngularMotorAccelNegTimescale = 0;
        public Vector3 OneByAngularMotorAccelNegTimescale => m_AngularMotorAccelNegTimescale.OneByTimescale;

        private TimescaleVectorData m_AngularMotorDecelNegTimescale = 0;
        public Vector3 OneByAngularMotorDecelNegTimescale => m_AngularMotorDecelNegTimescale.OneByTimescale;

        private ReferenceBoxed<double> m_BankingEfficiency = 0;
        private ReferenceBoxed<double> m_BankingMix = 0;
        private TimescaleDoubleData m_BankingTimescale = 0;
        public double OneByBankingTimescale => m_BankingTimescale.OneByTimescale;

        private ReferenceBoxed<double> m_HeightExceededTime = 0;

        private ReferenceBoxed<double> m_Buoyancy = 0;
        private ReferenceBoxed<double> m_HoverHeight = 0;
        private ReferenceBoxed<double> m_HoverEfficiency = 0;
        private TimescaleDoubleData m_HoverTimescale = 0.0;
        public double OneByHoverTimescale => m_HoverTimescale.OneByTimescale;

        private ReferenceVectorBoxed m_LinearDeflectionEfficiency = 0;
        private TimescaleVectorData m_LinearDeflectionTimescale = 1;
        public Vector3 OneByLinearDeflectionTimescale => m_LinearDeflectionTimescale.OneByTimescale;

        private TimescaleVectorData m_LinearMotorDecayTimescale = 120;
        public Vector3 OneByLinearMotorDecayTimescale => m_LinearMotorDecayTimescale.OneByTimescale;

        private TimescaleVectorData m_LinearMotorAccelPosTimescale = 0;
        public Vector3 OneByLinearMotorAccelPosTimescale => m_LinearMotorAccelPosTimescale.OneByTimescale;

        private TimescaleVectorData m_LinearMotorDecelPosTimescale = 0;
        public Vector3 OneByLinearMotorDecelPosTimescale => m_LinearMotorDecelPosTimescale.OneByTimescale;

        private TimescaleVectorData m_LinearMotorAccelNegTimescale = 0;
        public Vector3 OneByLinearMotorAccelNegTimescale => m_LinearMotorAccelNegTimescale.OneByTimescale;

        private TimescaleVectorData m_LinearMotorDecelNegTimescale = 0;
        public Vector3 OneByLinearMotorDecelNegTimescale => m_LinearMotorDecelNegTimescale.OneByTimescale;

        private ReferenceVectorBoxed m_VerticalAttractionEfficiency = 0;
        private TimescaleVectorData m_VerticalAttractionTimescale = 1000;
        public Vector3 OneByVerticalAttractionTimescale => m_VerticalAttractionTimescale.OneByTimescale;

        private int m_FlagsStore;

        private ReferenceVectorBoxed m_LinearWindEfficiency = 0;
        private ReferenceVectorBoxed m_AngularWindEfficiency = 0;

        private ReferenceBoxed<double> m_MouselookAzimuth = 0;
        private ReferenceBoxed<double> m_MouselookAltitude = 0;
        private ReferenceBoxed<double> m_BankingAzimuth = 0;
        private ReferenceBoxed<double> m_DisableMotorsAbove = 0;
        private ReferenceBoxed<double> m_DisableMotorsAfter = 0;
        private ReferenceBoxed<double> m_InvertedBankingModifier = 0;

        private ReferenceVectorBoxed m_LinearMoveToTargetEfficiency = 1;
        private TimescaleVectorData m_LinearMoveToTargetTimescale = 1;
        public Vector3 OneByLinearMoveToTargetTimescale => m_LinearMoveToTargetTimescale.OneByTimescale;
        private ReferenceVectorBoxed m_AngularMoveToTargetEfficiency = new Vector3(0, 1, 1);
        private TimescaleVectorData m_AngularMoveToTargetTimescale = 1;
        public Vector3 OneByAngularMoveToTargetTimescale => m_AngularMoveToTargetTimescale.OneByTimescale;
        private ReferenceVectorBoxed m_LinearMoveToTargetEpsilon = 0.1;
        private ReferenceVectorBoxed m_AngularMoveToTargetEpsilon = 1.DegToRad();
        private ReferenceVectorBoxed m_LinearMoveToTargetMaxOutput = 1;
        private ReferenceVectorBoxed m_AngularMoveToTargetMaxOutput = Math.PI;

        public void DecayDirections(double dt)
        {
            Vector3 angularMotorDirection = m_AngularMotorDirection;
            Vector3 linearMotorDirection = m_LinearMotorDirection;
            m_AngularMotorDirection = angularMotorDirection - angularMotorDirection.ElementMultiply((m_AngularMotorDecayTimescale.OneByTimescale * dt).ComponentMin(1));
            m_LinearMotorDirection = linearMotorDirection - linearMotorDirection.ElementMultiply((m_LinearMotorDecayTimescale.OneByTimescale * dt).ComponentMin(1));
        }

        public bool IsHoverMotorEnabled => m_HoverTimescale.Timescale < 300;

        public VehicleType VehicleType
        {
            get { return m_VehicleType; }

            set
            {
                m_HeightExceededTime = 0;

                m_LinearMoveToTargetEfficiency = new Vector3(1, 1, 0);
                m_LinearMoveToTargetTimescale = 1;
                m_LinearMoveToTargetEpsilon = new Vector3(0.1, 0.1, -1);
                m_LinearMoveToTargetMaxOutput = 1;

                m_AngularMoveToTargetEfficiency = new Vector3(0, 0, 1);
                m_AngularMoveToTargetTimescale = 1;
                m_AngularMoveToTargetEpsilon = new Vector3(-1, -1, 1.DegToRad());
                m_AngularMoveToTargetMaxOutput = Math.PI;

                switch (value)
                {
                    case VehicleType.None:
                        break;

                    case VehicleType.Sled:
                        m_LinearFrictionTimescale = new Vector3(30, 1, 1000);
                        m_AngularFrictionTimescale = 1000;
                        m_LinearMotorDirection = 0;
                        m_LinearMotorAccelPosTimescale = 1000;
                        m_LinearMotorDecelPosTimescale = 1000;
                        m_LinearMotorAccelNegTimescale = 1000;
                        m_LinearMotorDecelNegTimescale = 1000;
                        m_LinearMotorDecayTimescale = 120;
                        m_AngularMotorDirection = 0;
                        m_AngularMotorAccelPosTimescale = 1000;
                        m_AngularMotorDecelPosTimescale = 1000;
                        m_AngularMotorAccelNegTimescale = 1000;
                        m_AngularMotorDecelNegTimescale = 1000;
                        m_AngularMotorDecayTimescale = 120;
                        m_HoverHeight = 0;
                        m_HoverEfficiency = 1;
                        m_HoverTimescale = 10.0;
                        m_Buoyancy = 0;
                        m_LinearDeflectionEfficiency = 1;
                        m_LinearDeflectionTimescale = 1;
                        m_AngularDeflectionEfficiency = 0;
                        m_AngularDeflectionTimescale = 10.0;
                        m_VerticalAttractionEfficiency = 1;
                        m_VerticalAttractionTimescale = 1000;
                        m_BankingEfficiency = 0;
                        m_BankingMix = 1;
                        m_BankingTimescale = 10.0;
                        m_ReferenceFrame = Quaternion.Identity;
                        m_AngularWindEfficiency = 0;
                        m_LinearWindEfficiency = 0;

                        m_InvertedBankingModifier = 1.0;
                        m_BankingMix = 1.0;
                        m_BankingTimescale = 10.0;
                        m_MouselookAltitude = Math.PI / 4.0;
                        m_MouselookAzimuth = Math.PI / 4.0;
                        m_BankingAzimuth = Math.PI / 2.0;
                        m_DisableMotorsAbove = 0.0;
                        m_DisableMotorsAfter = 0.0;

                        Flags = VehicleFlags.NoDeflectionUp | VehicleFlags.LimitRollOnly | VehicleFlags.LimitMotorUp;
                        break;

                    case VehicleType.Car:
                        m_LinearFrictionTimescale = new Vector3(100, 2, 1000);
                        m_AngularFrictionTimescale = 1000;
                        m_LinearMotorDirection = 0;
                        m_LinearMotorAccelPosTimescale = 1;
                        m_LinearMotorDecelPosTimescale = 1;
                        m_LinearMotorAccelNegTimescale = 1;
                        m_LinearMotorDecelNegTimescale = 1;
                        m_LinearMotorDecayTimescale = 60;
                        m_AngularMotorDirection = 0;
                        m_AngularMotorAccelPosTimescale = 1;
                        m_AngularMotorDecelPosTimescale = 1;
                        m_AngularMotorAccelNegTimescale = 1;
                        m_AngularMotorDecelNegTimescale = 1;
                        m_AngularMotorDecayTimescale = 0.8;
                        m_HoverHeight = 0;
                        m_HoverEfficiency = 0;
                        m_HoverTimescale = 1000.0;
                        m_Buoyancy = 0;
                        m_LinearDeflectionEfficiency = 1;
                        m_LinearDeflectionTimescale = 2;
                        m_AngularDeflectionEfficiency = 0;
                        m_AngularDeflectionTimescale = 10;
                        m_VerticalAttractionEfficiency = 1;
                        m_VerticalAttractionTimescale = 10;
                        m_BankingEfficiency = -0.2;
                        m_BankingMix = 1;
                        m_BankingTimescale = 1.0;
                        m_ReferenceFrame = Quaternion.Identity;
                        m_AngularWindEfficiency = 0;
                        m_LinearWindEfficiency = 0;

                        m_InvertedBankingModifier = 1.0;
                        m_BankingMix = 1.0;
                        m_BankingTimescale = 1.0;
                        m_MouselookAltitude = Math.PI / 4.0;
                        m_MouselookAzimuth = Math.PI / 4.0;
                        m_BankingAzimuth = Math.PI / 2.0;
                        m_DisableMotorsAbove = 0.75;
                        m_DisableMotorsAfter = 2.5f;

                        Flags = VehicleFlags.NoDeflectionUp | VehicleFlags.LimitRollOnly | VehicleFlags.HoverUpOnly | VehicleFlags.LimitMotorUp;
                        break;

                    case VehicleType.Boat:
                        m_LinearFrictionTimescale = new Vector3(10, 3, 2);
                        m_AngularFrictionTimescale = 10;
                        m_LinearMotorDirection = 0;
                        m_LinearMotorAccelPosTimescale = 5;
                        m_LinearMotorDecelPosTimescale = 5;
                        m_LinearMotorAccelNegTimescale = 5;
                        m_LinearMotorDecelNegTimescale = 5;
                        m_LinearMotorDecayTimescale = 60;
                        m_AngularMotorDirection = 0;
                        m_AngularMotorAccelPosTimescale = 4;
                        m_AngularMotorDecelPosTimescale = 4;
                        m_AngularMotorAccelNegTimescale = 4;
                        m_AngularMotorDecelNegTimescale = 4;
                        m_AngularMotorDecayTimescale = 4;
                        m_HoverHeight = 0;
                        m_HoverEfficiency = 0.4;
                        m_HoverTimescale = 2.0;
                        m_Buoyancy = 1;
                        m_LinearDeflectionEfficiency = 0.5;
                        m_LinearDeflectionTimescale = 3.0;
                        m_AngularDeflectionEfficiency = 0.5;
                        m_AngularDeflectionTimescale = 5.0;
                        m_VerticalAttractionEfficiency = 0.5;
                        m_VerticalAttractionTimescale = 5.0;
                        m_BankingEfficiency = -0.3;
                        m_BankingMix = 0.8;
                        m_BankingTimescale = 1.0;
                        m_ReferenceFrame = Quaternion.Identity;
                        m_AngularWindEfficiency = Vector3.Zero;
                        m_LinearWindEfficiency = Vector3.Zero;

                        m_InvertedBankingModifier = 1.0;
                        m_BankingMix = 0.5;
                        m_BankingTimescale = 0.2;
                        m_MouselookAltitude = Math.PI / 4.0;
                        m_MouselookAzimuth = Math.PI / 4.0;
                        m_BankingAzimuth = Math.PI / 2.0;
                        m_DisableMotorsAbove = 0.0;
                        m_DisableMotorsAfter = 0.0;

                        Flags = VehicleFlags.NoDeflectionUp | VehicleFlags.HoverWaterOnly | VehicleFlags.HoverUpOnly | VehicleFlags.LimitMotorUp;
                        break;

                    case VehicleType.Airplane:
                        m_LinearMoveToTargetEfficiency = 1;
                        m_LinearMoveToTargetEpsilon = new Vector3(0.1, 0.1, 0.1);
                        m_AngularMoveToTargetEfficiency = new Vector3(1, 1, 1);
                        m_AngularMoveToTargetEpsilon = 1.DegToRad();

                        m_LinearFrictionTimescale = new Vector3(200, 10, 5);
                        m_AngularFrictionTimescale = 20;
                        m_LinearMotorDirection = 0;
                        m_LinearMotorAccelPosTimescale = 2;
                        m_LinearMotorDecelPosTimescale = 2;
                        m_LinearMotorAccelNegTimescale = 2;
                        m_LinearMotorDecelNegTimescale = 2;
                        m_LinearMotorDecayTimescale = 60;
                        m_AngularMotorDirection = 0;
                        m_AngularMotorAccelPosTimescale = 4;
                        m_AngularMotorDecelPosTimescale = 4;
                        m_AngularMotorAccelNegTimescale = 4;
                        m_AngularMotorDecelNegTimescale = 4;
                        m_AngularMotorDecayTimescale = 8;
                        m_HoverHeight = 0;
                        m_HoverEfficiency = 0.5;
                        m_HoverTimescale = 1000.0;
                        m_Buoyancy = 0;
                        m_LinearDeflectionEfficiency = 0.5;
                        m_LinearDeflectionTimescale = 0.5;
                        m_AngularDeflectionEfficiency = 1;
                        m_AngularDeflectionTimescale = 2.0;
                        m_VerticalAttractionEfficiency = 0.9;
                        m_VerticalAttractionTimescale = 2.0;
                        m_BankingEfficiency = 1;
                        m_BankingMix = 0.7;
                        m_BankingTimescale = 2.0;
                        m_ReferenceFrame = Quaternion.Identity;
                        m_AngularWindEfficiency = Vector3.Zero;
                        m_LinearWindEfficiency = Vector3.Zero;

                        m_InvertedBankingModifier = 1.0;
                        m_BankingMix = 0.7;
                        m_BankingTimescale = 1.0;
                        m_MouselookAltitude = Math.PI / 4.0;
                        m_MouselookAzimuth = Math.PI / 4.0;
                        m_BankingAzimuth = Math.PI / 2.0;
                        m_DisableMotorsAbove = 0.0;
                        m_DisableMotorsAfter = 0.0;

                        Flags = VehicleFlags.LimitRollOnly | VehicleFlags.TorqueWorldZ;
                        break;

                    case VehicleType.Balloon:
                        m_LinearMoveToTargetEfficiency = new Vector3(1, 1, 1);
                        m_LinearMoveToTargetEpsilon = new Vector3(0.1, 0.1, 0.1);
                        m_AngularMoveToTargetEfficiency = -1;
                        m_AngularMoveToTargetEpsilon = -1;

                        m_AngularMoveToTargetEfficiency = 0;
                        m_AngularMoveToTargetEpsilon = Math.PI;
                        m_LinearFrictionTimescale = 5;
                        m_AngularFrictionTimescale = 10;
                        m_LinearMotorDirection = 0;
                        m_LinearMotorAccelPosTimescale = 5;
                        m_LinearMotorDecelPosTimescale = 5;
                        m_LinearMotorAccelNegTimescale = 5;
                        m_LinearMotorDecelNegTimescale = 5;
                        m_LinearMotorDecayTimescale = 60;
                        m_AngularMotorDirection = 0;
                        m_AngularMotorAccelPosTimescale = 6;
                        m_AngularMotorDecelPosTimescale = 6;
                        m_AngularMotorAccelNegTimescale = 6;
                        m_AngularMotorDecelNegTimescale = 6;
                        m_AngularMotorDecayTimescale = 10;
                        m_HoverHeight = 5;
                        m_HoverEfficiency = 0.8;
                        m_HoverTimescale = 10.0;
                        m_Buoyancy = 1;
                        m_LinearDeflectionEfficiency = 0;
                        m_LinearDeflectionTimescale = 5.0;
                        m_AngularDeflectionEfficiency = 0;
                        m_AngularDeflectionTimescale = 5.0;
                        m_VerticalAttractionEfficiency = 1;
                        m_VerticalAttractionTimescale = 1000.0;
                        m_BankingEfficiency = 0;
                        m_BankingMix = 0.7;
                        m_BankingTimescale = 5.0;
                        m_ReferenceFrame = Quaternion.Identity;
                        m_AngularWindEfficiency = 0.01;
                        m_LinearWindEfficiency = 0.1;

                        m_InvertedBankingModifier = 1.0;
                        m_BankingMix = 0.5;
                        m_BankingTimescale = 5.0;
                        m_MouselookAltitude = Math.PI / 4.0;
                        m_MouselookAzimuth = Math.PI / 4.0;
                        m_BankingAzimuth = Math.PI / 2.0;
                        m_DisableMotorsAbove = 0.0;
                        m_DisableMotorsAfter = 0.0;

                        Flags = VehicleFlags.ReactToWind;
                        break;

                    case VehicleType.Motorcycle:    // Halcyon based vehicle type
                        m_LinearFrictionTimescale = new Vector3(100.0, 0.1, 10.0);
                        m_AngularFrictionTimescale = new Vector3(3.0, 0.2, 10.0);
                        m_LinearMotorDirection = 0;
                        m_AngularMotorDirection = 0;
                        m_LinearMotorOffset = new Vector3(0.0, 0.0, -0.1);
                        m_LinearMotorAccelPosTimescale = new Vector3(0.5, 1.0, 1.0);
                        m_LinearMotorDecelPosTimescale = new Vector3(0.5, 1.0, 1.0);
                        m_LinearMotorAccelNegTimescale = new Vector3(0.5, 1.0, 1.0);
                        m_LinearMotorDecelNegTimescale = new Vector3(0.5, 1.0, 1.0);
                        m_AngularMotorAccelPosTimescale = new Vector3(0.1, 0.1, 0.05);
                        m_AngularMotorDecelPosTimescale = new Vector3(0.1, 0.1, 0.05);
                        m_AngularMotorAccelNegTimescale = new Vector3(0.1, 0.1, 0.05);
                        m_AngularMotorDecelNegTimescale = new Vector3(0.1, 0.1, 0.05);
                        m_LinearMotorDecayTimescale = new Vector3(10.0, 1.0, 1.0);
                        m_AngularMotorDecayTimescale = new Vector3(0.2, 0.8, 0.1);
                        m_LinearWindEfficiency = 0;
                        m_AngularWindEfficiency = 0;

                        m_HoverHeight = 0.0;
                        m_HoverEfficiency = 0.0;
                        m_HoverTimescale = 1000.0;
                        m_Buoyancy = 0.0;
                        m_LinearDeflectionEfficiency = 1;
                        m_LinearDeflectionTimescale = 2.0;
                        m_AngularDeflectionEfficiency = 0.8;
                        m_AngularDeflectionTimescale = 2.0;
                        m_VerticalAttractionEfficiency = 1;
                        m_VerticalAttractionTimescale = 1.0;
                        m_BankingEfficiency = 0.95;
                        m_ReferenceFrame = Quaternion.Identity;

                        m_InvertedBankingModifier = -0.5;
                        m_BankingMix = 0.5;
                        m_BankingTimescale = 0.1;
                        m_MouselookAltitude = Math.PI / 4.0;
                        m_MouselookAzimuth = Math.PI / 4.0;
                        m_BankingAzimuth = Math.PI / 2.0;
                        m_DisableMotorsAbove = 1.5;
                        m_DisableMotorsAfter = 2.5;

                        Flags = VehicleFlags.NoDeflectionUp | VehicleFlags.HoverUpOnly | VehicleFlags.LimitMotorUp | VehicleFlags.LimitMotorDown |
                                        VehicleFlags.LimitRollOnly | VehicleFlags.TorqueWorldZ;
                        break;

                    case VehicleType.Sailboat:  // Halcyon-based vehicle type
                        m_LinearFrictionTimescale = new Vector3(200.0, 0.5, 3.0);
                        m_AngularFrictionTimescale = new Vector3(10.0, 1.0, 0.2);
                        m_LinearMotorDirection = 0;
                        m_AngularMotorDirection = 0;
                        m_LinearMotorOffset = 0;
                        m_LinearMotorAccelPosTimescale = new Vector3(1.0, 5.0, 5.0);
                        m_LinearMotorDecelPosTimescale = new Vector3(1.0, 5.0, 5.0);
                        m_LinearMotorAccelNegTimescale = new Vector3(1.0, 5.0, 5.0);
                        m_LinearMotorDecelNegTimescale = new Vector3(1.0, 5.0, 5.0);
                        m_AngularMotorAccelPosTimescale = new Vector3(2.0, 2.0, 0.1);
                        m_AngularMotorDecelPosTimescale = new Vector3(2.0, 2.0, 0.1);
                        m_AngularMotorAccelNegTimescale = new Vector3(2.0, 2.0, 0.1);
                        m_AngularMotorDecelNegTimescale = new Vector3(2.0, 2.0, 0.1);
                        m_LinearMotorDecayTimescale = new Vector3(1.0, 10.0, 10.0);
                        m_AngularMotorDecayTimescale = new Vector3(0.3, 0.3, 0.1);
                        m_LinearWindEfficiency = new Vector3(0.02, 0.001, 0.0);
                        m_AngularWindEfficiency = new Vector3(0.1, 0.01, 0.0);

                        m_HoverHeight = 0.0001;
                        m_HoverEfficiency = 0.8;
                        m_HoverTimescale = 0.5;
                        m_Buoyancy = 0.0;
                        m_LinearDeflectionEfficiency = 0.5;
                        m_LinearDeflectionTimescale = 3.0;
                        m_AngularDeflectionEfficiency = 0.5;
                        m_AngularDeflectionTimescale = 5.0;
                        m_VerticalAttractionEfficiency = 0.5;
                        m_VerticalAttractionTimescale = 0.3;
                        m_BankingEfficiency = 0.8;
                        m_InvertedBankingModifier = -0.2;
                        m_BankingMix = 0.5f;
                        m_BankingTimescale = 0.5;
                        m_MouselookAltitude = Math.PI / 4.0f;
                        m_MouselookAzimuth = Math.PI / 4.0f;
                        m_BankingAzimuth = Math.PI / 2.0f;
                        m_DisableMotorsAbove = 0.0f;
                        m_DisableMotorsAfter = 0.0f;

                        m_ReferenceFrame = Quaternion.Identity;

                        Flags = VehicleFlags.NoDeflectionUp | VehicleFlags.HoverWaterOnly |
                            VehicleFlags.LimitMotorUp | VehicleFlags.LimitMotorDown |
                            VehicleFlags.ReactToWind | VehicleFlags.ReactToCurrents |
                            VehicleFlags.TorqueWorldZ;
                        break;

                    default:
                        throw new InvalidOperationException();
                }

                m_VehicleType = value;
                m_Part.IncSerialNumber();
            }
        }

        public VehicleFlags Flags
        {
            get { return (VehicleFlags)m_FlagsStore; }

            set
            {
                m_FlagsStore = (int)value;
                m_Part.IncSerialNumber();
            }
        }

        public void SetFlags(VehicleFlags value)
        {
            int setflags = (int)value;
            int oldFlagsStore = m_FlagsStore;
            int newFlagsStore;
            do
            {
                newFlagsStore = oldFlagsStore | setflags;
                oldFlagsStore = Interlocked.CompareExchange(ref m_FlagsStore, newFlagsStore, oldFlagsStore) | setflags;
            } while (newFlagsStore != oldFlagsStore);
            m_Part.IncSerialNumber();
        }

        public void ClearFlags(VehicleFlags value)
        {
            int clrflags = ~(int)value;
            int oldFlagsStore = m_FlagsStore;
            int newFlagsStore;
            do
            {
                newFlagsStore = oldFlagsStore & ~clrflags;
                oldFlagsStore = Interlocked.CompareExchange(ref m_FlagsStore, newFlagsStore, oldFlagsStore) & ~clrflags;
            } while (newFlagsStore != oldFlagsStore);
            m_Part.IncSerialNumber();
        }

        public Quaternion this[VehicleRotationParamId id]
        {
            get
            {
                switch (id)
                {
                    case VehicleRotationParamId.ReferenceFrame:
                        return m_ReferenceFrame;

                    default:
                        throw new KeyNotFoundException();
                }
            }
            set
            {
                switch (id)
                {
                    case VehicleRotationParamId.ReferenceFrame:
                        m_ReferenceFrame = value;
                        m_Part.IncSerialNumber();
                        break;

                    default:
                        throw new KeyNotFoundException();
                }
            }
        }

        public Vector3 this[VehicleVectorParamId id]
        {
            get
            {
                switch (id)
                {
                    case VehicleVectorParamId.AngularDeflectionEfficiency:
                        return m_AngularDeflectionEfficiency;

                    case VehicleVectorParamId.AngularDeflectionTimescale:
                        return m_AngularDeflectionTimescale.Timescale;

                    case VehicleVectorParamId.LinearDeflectionTimescale:
                        return m_LinearDeflectionTimescale.Timescale;

                    case VehicleVectorParamId.LinearDeflectionEfficiency:
                        return m_LinearDeflectionEfficiency;

                    case VehicleVectorParamId.VerticalAttractionEfficiency:
                        return m_VerticalAttractionEfficiency;

                    case VehicleVectorParamId.VerticalAttractionTimescale:
                        return m_VerticalAttractionTimescale.Timescale;

                    case VehicleVectorParamId.AngularFrictionTimescale:
                        return m_AngularFrictionTimescale.Timescale;

                    case VehicleVectorParamId.AngularMotorDirection:
                        return m_AngularMotorDirection;

                    case VehicleVectorParamId.LinearFrictionTimescale:
                        return m_LinearFrictionTimescale.Timescale;

                    case VehicleVectorParamId.LinearMotorDirection:
                        return m_LinearMotorDirection;

                    case VehicleVectorParamId.LinearMotorOffset:
                        return m_LinearMotorOffset;

                    case VehicleVectorParamId.AngularMotorDecayTimescale:
                        return m_AngularMotorDecayTimescale.Timescale;

                    case VehicleVectorParamId.AngularMotorTimescale:
                    case VehicleVectorParamId.AngularMotorAccelPosTimescale:
                        return m_AngularMotorAccelPosTimescale.Timescale;

                    case VehicleVectorParamId.AngularMotorDecelPosTimescale:
                        return m_AngularMotorDecelPosTimescale.Timescale;

                    case VehicleVectorParamId.AngularMotorAccelNegTimescale:
                        return m_AngularMotorAccelNegTimescale.Timescale;

                    case VehicleVectorParamId.AngularMotorDecelNegTimescale:
                        return m_AngularMotorDecelNegTimescale.Timescale;

                    case VehicleVectorParamId.LinearMotorDecayTimescale:
                        return m_LinearMotorDecayTimescale.Timescale;

                    case VehicleVectorParamId.LinearMotorTimescale:
                    case VehicleVectorParamId.LinearMotorAccelPosTimescale:
                        return m_LinearMotorAccelPosTimescale.Timescale;

                    case VehicleVectorParamId.LinearMotorDecelPosTimescale:
                        return m_LinearMotorDecelPosTimescale.Timescale;

                    case VehicleVectorParamId.LinearMotorAccelNegTimescale:
                        return m_LinearMotorAccelNegTimescale.Timescale;

                    case VehicleVectorParamId.LinearMotorDecelNegTimescale:
                        return m_LinearMotorDecelNegTimescale.Timescale;

                    case VehicleVectorParamId.AngularWindEfficiency:
                        return m_AngularWindEfficiency;

                    case VehicleVectorParamId.LinearWindEfficiency:
                        return m_LinearWindEfficiency;

                    case VehicleVectorParamId.LinearMoveToTargetEfficiency:
                        return m_LinearMoveToTargetEfficiency;

                    case VehicleVectorParamId.LinearMoveToTargetTimescale:
                        return m_LinearMoveToTargetTimescale.Timescale;

                    case VehicleVectorParamId.LinearMoveToTargetEpsilon:
                        return m_LinearMoveToTargetEpsilon;

                    case VehicleVectorParamId.LinearMoveToTargetMaxOutput:
                        return m_LinearMoveToTargetMaxOutput;

                    case VehicleVectorParamId.AngularMoveToTargetEfficiency:
                        return m_AngularMoveToTargetEfficiency;

                    case VehicleVectorParamId.AngularMoveToTargetTimescale:
                        return m_AngularMoveToTargetTimescale.Timescale;

                    case VehicleVectorParamId.AngularMoveToTargetEpsilon:
                        return m_AngularMoveToTargetEpsilon;

                    case VehicleVectorParamId.AngularMoveToTargetMaxOutput:
                        return m_AngularMoveToTargetMaxOutput;

                    default:
                        throw new KeyNotFoundException();
                }
            }
            set
            {
                switch (id)
                {
                    case VehicleVectorParamId.AngularDeflectionEfficiency:
                        m_AngularDeflectionEfficiency = value.ComponentClamp(0, 1);
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.AngularDeflectionTimescale:
                        m_AngularDeflectionTimescale = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.LinearDeflectionEfficiency:
                        m_LinearDeflectionEfficiency = value.ComponentClamp(0, 1);
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.LinearDeflectionTimescale:
                        m_LinearDeflectionTimescale = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.VerticalAttractionEfficiency:
                        m_VerticalAttractionEfficiency = value.ComponentClamp(0f, 1f);
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.VerticalAttractionTimescale:
                        m_VerticalAttractionTimescale = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.AngularFrictionTimescale:
                        m_AngularFrictionTimescale = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.AngularMotorDirection:
                        m_AngularMotorDirection = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.LinearFrictionTimescale:
                        m_LinearFrictionTimescale = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.LinearMotorDirection:
                        m_LinearMotorDirection = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.LinearMotorOffset:
                        m_LinearMotorOffset = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.AngularMotorDecayTimescale:
                        m_AngularMotorDecayTimescale = value.ComponentMin(120);
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.AngularMotorTimescale:
                        m_AngularMotorAccelPosTimescale = value;
                        m_AngularMotorDecelPosTimescale = m_AngularMotorAccelPosTimescale;
                        m_AngularMotorAccelNegTimescale = m_AngularMotorAccelPosTimescale;
                        m_AngularMotorDecelNegTimescale = m_AngularMotorAccelPosTimescale;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.AngularMotorAccelPosTimescale:
                        m_AngularMotorAccelPosTimescale = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.AngularMotorDecelPosTimescale:
                        m_AngularMotorDecelPosTimescale = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.AngularMotorAccelNegTimescale:
                        m_AngularMotorAccelNegTimescale = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.AngularMotorDecelNegTimescale:
                        m_AngularMotorDecelNegTimescale = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.LinearMotorDecayTimescale:
                        m_LinearMotorDecayTimescale = value.ComponentMin(120);
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.LinearMotorTimescale:
                        m_LinearMotorAccelPosTimescale = value;
                        m_LinearMotorDecelPosTimescale = m_LinearMotorAccelPosTimescale;
                        m_LinearMotorAccelNegTimescale = m_LinearMotorAccelPosTimescale;
                        m_LinearMotorDecelNegTimescale = m_LinearMotorAccelPosTimescale;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.LinearMotorAccelPosTimescale:
                        m_LinearMotorAccelPosTimescale = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.LinearMotorDecelPosTimescale:
                        m_LinearMotorDecelPosTimescale = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.LinearMotorAccelNegTimescale:
                        m_LinearMotorAccelNegTimescale = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.LinearMotorDecelNegTimescale:
                        m_LinearMotorDecelNegTimescale = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.AngularWindEfficiency:
                        m_AngularWindEfficiency = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.LinearWindEfficiency:
                        m_LinearWindEfficiency = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.LinearMoveToTargetEfficiency:
                        m_LinearMoveToTargetEfficiency = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.LinearMoveToTargetTimescale:
                        m_LinearMoveToTargetTimescale = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.LinearMoveToTargetEpsilon:
                        m_LinearMoveToTargetEpsilon = value; /* negative for disable */
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.LinearMoveToTargetMaxOutput:
                        m_LinearMoveToTargetMaxOutput = value.ComponentMax(0);
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.AngularMoveToTargetEfficiency:
                        m_AngularMoveToTargetEfficiency = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.AngularMoveToTargetTimescale:
                        m_AngularMoveToTargetTimescale = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.AngularMoveToTargetEpsilon:
                        m_AngularMoveToTargetEpsilon = value; /* negative for disable */
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleVectorParamId.AngularMoveToTargetMaxOutput:
                        m_AngularMoveToTargetMaxOutput = value.ComponentMax(0);
                        m_Part.IncSerialNumber();
                        break;

                    default:
                        throw new KeyNotFoundException();
                }
            }
        }

        public double this[VehicleFloatParamId id]
        {
            get
            {
                switch (id)
                {
                    case VehicleFloatParamId.BankingEfficiency:
                        return m_BankingEfficiency;

                    case VehicleFloatParamId.BankingMix:
                        return m_BankingMix;

                    case VehicleFloatParamId.BankingTimescale:
                        return m_BankingTimescale.Timescale;

                    case VehicleFloatParamId.Buoyancy:
                        return m_Buoyancy;

                    case VehicleFloatParamId.HoverHeight:
                        return m_HoverHeight;

                    case VehicleFloatParamId.HoverEfficiency:
                        return m_HoverEfficiency;

                    case VehicleFloatParamId.HoverTimescale:
                        return m_HoverTimescale.Timescale;

                    case VehicleFloatParamId.MouselookAzimuth:
                        return m_MouselookAzimuth;

                    case VehicleFloatParamId.MouselookAltitude:
                        return m_MouselookAltitude;

                    case VehicleFloatParamId.BankingAzimuth:
                        return m_BankingAzimuth;

                    case VehicleFloatParamId.DisableMotorsAbove:
                        return m_DisableMotorsAbove;

                    case VehicleFloatParamId.DisableMotorsAfter:
                        return m_DisableMotorsAfter;

                    case VehicleFloatParamId.InvertedBankingModifier:
                        return m_InvertedBankingModifier;

                    case VehicleFloatParamId.HeightExceededTime:
                        return m_HeightExceededTime;

                    default:
                        throw new KeyNotFoundException();
                }
            }
            set
            {
                switch (id)
                {
                    case VehicleFloatParamId.BankingEfficiency:
                        m_BankingEfficiency = value.Clamp(-1f, 1f);
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleFloatParamId.BankingMix:
                        m_BankingMix = value.Clamp(0f, 1f);
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleFloatParamId.BankingTimescale:
                        m_BankingTimescale = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleFloatParamId.Buoyancy:
                        m_Buoyancy = value.Clamp(-1f, 1f);
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleFloatParamId.HoverHeight:
                        m_HoverHeight = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleFloatParamId.HoverEfficiency:
                        m_HoverEfficiency = value.Clamp(0f, 1f);
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleFloatParamId.HoverTimescale:
                        m_HoverTimescale = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleFloatParamId.MouselookAzimuth:
                        m_MouselookAzimuth = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleFloatParamId.MouselookAltitude:
                        m_MouselookAltitude = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleFloatParamId.BankingAzimuth:
                        m_BankingAzimuth = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleFloatParamId.DisableMotorsAbove:
                        m_DisableMotorsAbove = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleFloatParamId.DisableMotorsAfter:
                        m_DisableMotorsAfter = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleFloatParamId.InvertedBankingModifier:
                        m_InvertedBankingModifier = value;
                        m_Part.IncSerialNumber();
                        break;

                    case VehicleFloatParamId.HeightExceededTime:
                        m_HeightExceededTime = value;
                        break;

                    default:
                        throw new KeyNotFoundException();
                }
            }
        }

        public byte[] ToSerialization()
        {
            Map m = new Map
            {
                { "Type", (int)m_VehicleType },
                { "Flags", m_FlagsStore },
                { "ReferenceFrame", (Quaternion)m_ReferenceFrame },
                { "AngularFrictionTimescale", m_AngularFrictionTimescale.Timescale },
                { "AngularMotorDirection", (Vector3)m_AngularMotorDirection },
                { "LinearFrictionTimescale", m_LinearFrictionTimescale.Timescale },
                { "LinearMotorDirection", (Vector3)m_LinearMotorDirection },
                { "LinearMotorOffset", (Vector3)m_LinearMotorOffset },
                { "AngularDeflectionEfficiency", (Vector3)m_AngularDeflectionEfficiency },
                { "AngularDeflectionTimescale", m_AngularDeflectionTimescale.Timescale },
                { "AngularMotorDecayTimescale", m_AngularMotorDecayTimescale.Timescale },
                { "AngularMotorAccelPosTimescale", m_AngularMotorAccelPosTimescale.Timescale },
                { "AngularMotorDecelPosTimescale", m_AngularMotorDecelPosTimescale.Timescale },
                { "AngularMotorAccelNegTimescale", m_AngularMotorAccelNegTimescale.Timescale },
                { "AngularMotorDecelNegTimescale", m_AngularMotorDecelNegTimescale.Timescale },
                { "BankingEfficiency", m_BankingEfficiency },
                { "BankingMix", m_BankingMix },
                { "BankingTimescale", m_BankingTimescale.Timescale },
                { "Buoyancy", m_Buoyancy },
                { "HoverHeight", m_HoverHeight },
                { "HoverEfficiency", m_HoverEfficiency },
                { "HoverTimescale", m_HoverTimescale.Timescale },
                { "LinearDeflectionEfficiency", (Vector3)m_LinearDeflectionEfficiency },
                { "LinearDeflectionTimescale", m_LinearDeflectionTimescale.Timescale },
                { "LinearMotorDecayTimescale", m_LinearMotorDecayTimescale.Timescale },
                { "LinearMotorAccelPosTimescale", m_LinearMotorAccelPosTimescale.Timescale },
                { "LinearMotorDecelPosTimescale", m_LinearMotorDecelPosTimescale.Timescale },
                { "LinearMotorAccelNegTimescale", m_LinearMotorAccelNegTimescale.Timescale },
                { "LinearMotorDecelNegTimescale", m_LinearMotorDecelNegTimescale.Timescale },
                { "VerticalAttractionEfficiency", (Vector3)m_VerticalAttractionEfficiency },
                { "VerticalAttractionTimescale", m_VerticalAttractionTimescale.Timescale },
                { "LinearWindEfficiency", (Vector3)m_LinearWindEfficiency },
                { "AngularWindEfficiency", (Vector3)m_AngularWindEfficiency },
                { "MouselookAzimuth", m_MouselookAzimuth },
                { "MouselookAltitude", m_MouselookAltitude },
                { "BankingAzimuth", m_BankingAzimuth },
                { "DisableMotorsAbove", m_DisableMotorsAbove },
                { "DisableMotorsAfter", m_DisableMotorsAfter },
                { "InvertedBankingModifier", m_InvertedBankingModifier },
                { "HeightExceededTime", m_HeightExceededTime },
                { "LinearMoveToTargetEfficiency", (Vector3)m_LinearMoveToTargetEfficiency },
                { "LinearMoveToTargetTimescale", m_LinearMoveToTargetTimescale.Timescale },
                { "LinearMoveToTargetMaxOutput", (Vector3)m_LinearMoveToTargetMaxOutput },
                { "AngularMoveToTargetEfficiency", (Vector3)m_AngularMoveToTargetEfficiency },
                { "AngularMoveToTargetTimescale", m_AngularMoveToTargetTimescale.Timescale },
                { "AngularMoveToTargetMaxOutput", (Vector3)m_AngularMoveToTargetMaxOutput }
            };

            using (var ms = new MemoryStream())
            {
                LlsdBinary.Serialize(m, ms);
                return ms.ToArray();
            }
        }
    }
}
