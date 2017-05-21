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

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Physics.Vehicle
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
    public sealed class VehicleParams
    {
        private VehicleType m_VehicleType;

        private Quaternion m_ReferenceFrame;
        private Vector3 m_AngularFrictionTimescale;
        private Vector3 m_OneByAngularFrictonTimescale = Vector3.One;
        public Vector3 OneByAngularFrictionTimescale
        {
            get
            {
                lock(m_VehicleParamLock)
                {
                    return m_OneByAngularFrictonTimescale;
                }
            }
        }

        private Vector3 m_AngularMotorDirection;
        private Vector3 m_LinearFrictionTimescale;
        private Vector3 m_OneByLinearFrictionTimescale = Vector3.One;
        public Vector3 OneByLinearFrictionTimescale
        {
            get
            {
                lock(m_VehicleParamLock)
                {
                    return m_OneByLinearFrictionTimescale;
                }
            }
        }

        private Vector3 m_LinearMotorDirection;
        private Vector3 m_LinearMotorOffset;

        private double m_AngularDeflectionEfficiency;
        private double m_AngularDeflectionTimescale;
        private double m_OneByAngularDeflectionTimescale = 1;
        public double OneByAngularDeflectionTimescale
        {
            get
            {
                lock (m_VehicleParamLock)
                {
                    return m_OneByAngularDeflectionTimescale;
                }
            }
        }

        private Vector3 m_AngularMotorDecayTimescale;
        private Vector3 m_OneByAngularMotorDecayTimescale = Vector3.One;
        public Vector3 OneByAngularMotorDecayTimescale
        {
            get
            {
                lock (m_VehicleParamLock)
                {
                    return m_OneByAngularMotorDecayTimescale;
                }
            }
        }

        private Vector3 m_AngularMotorTimescale;
        private Vector3 m_OneByAngularMotorTimescale = Vector3.One;
        public Vector3 OneByAngularMotorTimescale
        {
            get
            {
                lock(m_VehicleParamLock)
                {
                    return m_OneByAngularMotorTimescale;
                }
            }
        }

        private double m_BankingEfficiency;
        private double m_BankingMix;
        private double m_BankingTimescale;
        private double m_OneByBankingTimescale = 1;
        public double OneByBankingTimescale
        {
            get
            {
                lock(m_VehicleParamLock)
                {
                    return m_OneByBankingTimescale;
                }
            }
        }

        private double m_Buoyancy;
        private double m_HoverHeight;
        private double m_HoverEfficiency;
        private double m_HoverTimescale;
        private double m_OneByHoverTimescale = 1;
        public double OneByHoverTimescale
        {
            get
            {
                lock(m_VehicleParamLock)
                {
                    return m_OneByHoverTimescale;
                }
            }
        }

        private double m_LinearDeflectionEfficiency;
        private double m_LinearDeflectionTimescale;
        private double m_OneByLinearDeflectionTimescale = 1;
        public double OneByLinearDeflectionTimescale
        {
            get
            {
                lock (m_VehicleParamLock)
                {
                    return m_OneByLinearDeflectionTimescale;
                }
            }
        }

        private Vector3 m_LinearMotorDecayTimescale;
        private Vector3 m_OneByLinearMotorDecayTimescale = Vector3.One;
        public Vector3 OneByLinearMotorDecayTimescale
        {
            get
            {
                lock(m_VehicleParamLock)
                {
                    return m_OneByLinearMotorDecayTimescale;
                }
            }
        }

        private Vector3 m_LinearMotorTimescale;
        private Vector3 m_OneByLinearMotorTimescale = Vector3.One;
        public Vector3 OneByLinearMotorTimescale
        {
            get
            {
                lock(m_VehicleParamLock)
                {
                    return m_OneByLinearMotorTimescale;
                }
            }
        }

        private double m_VerticalAttractionEfficiency;
        private double m_VerticalAttractionTimescale;
        private double m_OneByVerticalAttractionTimescale = 1;
        public double OneByVerticalAttractionTimescale
        {
            get
            {
                lock(m_VehicleParamLock)
                {
                    return m_OneByVerticalAttractionTimescale;
                }
            }
        }

        private VehicleFlags m_Flags;

        private Vector3 m_LinearWindEfficiency;
        private Vector3 m_AngularWindEfficiency;

        private double m_MouselookAzimuth;
        private double m_MouselookAltitude;
        private double m_BankingAzimuth;
        private double m_DisableMotorsAbove;
        private double m_DisableMotorsAfter;
        private double m_InvertedBankingModifier;

        private readonly object m_VehicleParamLock = new object();

        public void DecayDirections(double dt)
        {
            lock(m_VehicleParamLock)
            {
                m_AngularMotorDirection = m_AngularMotorDirection.ElementDivide(Vector3.One.ElementDivide(m_AngularMotorDecayTimescale) * dt);
                m_LinearMotorDirection = m_LinearMotorDirection.ElementDivide(Vector3.One.ElementDivide(m_LinearMotorDecayTimescale) * dt);
            }
        }

        public VehicleType VehicleType
        {
            get { return m_VehicleType; }

            set
            {
                lock (m_VehicleParamLock)
                {
                    switch (value)
                    {
                        case VehicleType.None:
                            break;

                        case VehicleType.Sled:
                            m_LinearFrictionTimescale = new Vector3(30, 1, 1000);
                            m_AngularFrictionTimescale = new Vector3(1000, 1000, 1000);
                            m_LinearMotorDirection = new Vector3(0, 0, 0);
                            m_LinearMotorTimescale = new Vector3(1000, 1000, 1000);
                            m_LinearMotorDecayTimescale = new Vector3(120, 120, 120);
                            m_AngularMotorDirection = new Vector3(0, 0, 0);
                            m_AngularMotorTimescale = new Vector3(1000, 1000, 1000);
                            m_AngularMotorDecayTimescale = new Vector3(120, 120, 120);
                            m_HoverHeight = 0;
                            m_HoverEfficiency = 10;
                            m_HoverTimescale = 10;
                            m_Buoyancy = 0;
                            m_LinearDeflectionEfficiency = 1;
                            m_LinearDeflectionTimescale = 1;
                            m_AngularDeflectionEfficiency = 0;
                            m_AngularDeflectionTimescale = 10;
                            m_VerticalAttractionEfficiency = 1;
                            m_VerticalAttractionTimescale = 1000;
                            m_BankingEfficiency = 0;
                            m_BankingMix = 1;
                            m_BankingTimescale = 10;
                            m_ReferenceFrame = Quaternion.Identity;
                            m_AngularWindEfficiency = Vector3.Zero;
                            m_LinearWindEfficiency = Vector3.Zero;

                            m_InvertedBankingModifier = 1.0f;
                            m_BankingMix = 1.0f;
                            m_BankingTimescale = 10.0f;
                            m_MouselookAltitude = Math.PI / 4.0f;
                            m_MouselookAzimuth = Math.PI / 4.0f;
                            m_BankingAzimuth = (float)Math.PI / 2.0f;
                            m_DisableMotorsAbove = 0.0f;
                            m_DisableMotorsAfter = 0.0f;

                            m_Flags = VehicleFlags.NoDeflectionUp | VehicleFlags.LimitRollOnly | VehicleFlags.LimitMotorUp;
                            break;

                        case VehicleType.Car:
                            m_LinearFrictionTimescale = new Vector3(100, 2, 1000);
                            m_AngularFrictionTimescale = new Vector3(1000, 1000, 1000);
                            m_LinearMotorDirection = new Vector3(0, 0, 0);
                            m_LinearMotorTimescale = new Vector3(1, 1, 1);
                            m_LinearMotorDecayTimescale = new Vector3(60, 60, 60);
                            m_AngularMotorDirection = new Vector3(0, 0, 0);
                            m_AngularMotorTimescale = new Vector3(1, 1, 1);
                            m_AngularMotorDecayTimescale = new Vector3(0.8, 0.8, 0.8);
                            m_HoverHeight = 0;
                            m_HoverEfficiency = 0;
                            m_HoverTimescale = 1000;
                            m_Buoyancy = 0;
                            m_LinearDeflectionEfficiency = 1;
                            m_LinearDeflectionTimescale = 2;
                            m_AngularDeflectionEfficiency = 0;
                            m_AngularDeflectionTimescale = 10;
                            m_VerticalAttractionEfficiency = 1;
                            m_VerticalAttractionTimescale = 10;
                            m_BankingEfficiency = -0.2;
                            m_BankingMix = 1;
                            m_BankingTimescale = 1;
                            m_ReferenceFrame = Quaternion.Identity;
                            m_AngularWindEfficiency = Vector3.Zero;
                            m_LinearWindEfficiency = Vector3.Zero;

                            m_InvertedBankingModifier = 1.0f;
                            m_BankingMix = 1.0f;
                            m_BankingTimescale = 1.0f;
                            m_MouselookAltitude = (float)Math.PI / 4.0f;
                            m_MouselookAzimuth = (float)Math.PI / 4.0f;
                            m_BankingAzimuth = (float)Math.PI / 2.0f;
                            m_DisableMotorsAbove = 0.75f;
                            m_DisableMotorsAfter = 2.5f;

                            m_Flags = VehicleFlags.NoDeflectionUp | VehicleFlags.LimitRollOnly | VehicleFlags.HoverUpOnly | VehicleFlags.LimitMotorUp;
                            break;

                        case VehicleType.Boat:
                            m_LinearFrictionTimescale = new Vector3(10, 3, 2);
                            m_AngularFrictionTimescale = new Vector3(10, 10, 10);
                            m_LinearMotorDirection = new Vector3(0, 0, 0);
                            m_LinearMotorTimescale = new Vector3(5, 5, 5);
                            m_LinearMotorDecayTimescale = new Vector3(60, 60, 60);
                            m_AngularMotorDirection = new Vector3(0, 0, 0);
                            m_AngularMotorTimescale = new Vector3(4, 4, 4);
                            m_AngularMotorDecayTimescale = new Vector3(4, 4, 4);
                            m_HoverHeight = 0;
                            m_HoverEfficiency = 0.4;
                            m_HoverTimescale = 2;
                            m_Buoyancy = 1;
                            m_LinearDeflectionEfficiency = 0.5;
                            m_LinearDeflectionTimescale = 3;
                            m_AngularDeflectionEfficiency = 0.5;
                            m_AngularDeflectionTimescale = 5;
                            m_VerticalAttractionEfficiency = 0.5;
                            m_VerticalAttractionTimescale = 5;
                            m_BankingEfficiency = -0.3;
                            m_BankingMix = 0.8;
                            m_BankingTimescale = 1;
                            m_ReferenceFrame = Quaternion.Identity;
                            m_AngularWindEfficiency = Vector3.Zero;
                            m_LinearWindEfficiency = Vector3.Zero;

                            m_InvertedBankingModifier = 1.0f;
                            m_BankingMix = 0.5f;
                            m_BankingTimescale = 0.2f;
                            m_MouselookAltitude = (float)Math.PI / 4.0f;
                            m_MouselookAzimuth = (float)Math.PI / 4.0f;
                            m_BankingAzimuth = (float)Math.PI / 2.0f;
                            m_DisableMotorsAbove = 0.0f;
                            m_DisableMotorsAfter = 0.0f;

                            m_Flags = VehicleFlags.NoDeflectionUp | VehicleFlags.HoverWaterOnly | VehicleFlags.HoverUpOnly | VehicleFlags.LimitMotorUp;
                            break;

                        case VehicleType.Airplane:
                            m_LinearFrictionTimescale = new Vector3(200, 10, 5);
                            m_AngularFrictionTimescale = new Vector3(20, 20, 20);
                            m_LinearMotorDirection = new Vector3(0, 0, 0);
                            m_LinearMotorTimescale = new Vector3(2, 2, 2);
                            m_LinearMotorDecayTimescale = new Vector3(60, 60, 60);
                            m_AngularMotorDirection = new Vector3(0, 0, 0);
                            m_AngularMotorTimescale = new Vector3(4, 4, 4);
                            m_AngularMotorDecayTimescale = new Vector3(8, 8, 8);
                            m_HoverHeight = 0;
                            m_HoverEfficiency = 0.5;
                            m_HoverTimescale = 1000;
                            m_Buoyancy = 0;
                            m_LinearDeflectionEfficiency = 0.5;
                            m_LinearDeflectionTimescale = 0.5;
                            m_AngularDeflectionEfficiency = 1;
                            m_AngularDeflectionTimescale = 2;
                            m_VerticalAttractionEfficiency = 0.9;
                            m_VerticalAttractionTimescale = 2;
                            m_BankingEfficiency = 1;
                            m_BankingMix = 0.7;
                            m_BankingTimescale = 2;
                            m_ReferenceFrame = Quaternion.Identity;
                            m_AngularWindEfficiency = Vector3.Zero;
                            m_LinearWindEfficiency = Vector3.Zero;

                            m_InvertedBankingModifier = 1.0f;
                            m_BankingMix = 0.7f;
                            m_BankingTimescale = 1.0f;
                            m_MouselookAltitude = Math.PI / 4.0f;
                            m_MouselookAzimuth = Math.PI / 4.0f;
                            m_BankingAzimuth = Math.PI / 2.0f;
                            m_DisableMotorsAbove = 0.0f;
                            m_DisableMotorsAfter = 0.0f;

                            m_Flags = VehicleFlags.LimitRollOnly | VehicleFlags.TorqueWorldZ;
                            break;

                        case VehicleType.Balloon:
                            m_LinearFrictionTimescale = new Vector3(5, 5, 5);
                            m_AngularFrictionTimescale = new Vector3(10, 10, 10);
                            m_LinearMotorDirection = new Vector3(0, 0, 0);
                            m_LinearMotorTimescale = new Vector3(5, 5, 5);
                            m_LinearMotorDecayTimescale = new Vector3(60, 60, 60);
                            m_AngularMotorDirection = new Vector3(0, 0, 0);
                            m_AngularMotorTimescale = new Vector3(6, 6, 6);
                            m_AngularMotorDecayTimescale = new Vector3(10, 10, 10);
                            m_HoverHeight = 5;
                            m_HoverEfficiency = 0.8;
                            m_HoverTimescale = 10;
                            m_Buoyancy = 1;
                            m_LinearDeflectionEfficiency = 0;
                            m_LinearDeflectionTimescale = 5;
                            m_AngularDeflectionEfficiency = 0;
                            m_AngularDeflectionTimescale = 5;
                            m_VerticalAttractionEfficiency = 1;
                            m_VerticalAttractionTimescale = 1000;
                            m_BankingEfficiency = 0;
                            m_BankingMix = 0.7;
                            m_BankingTimescale = 5;
                            m_ReferenceFrame = Quaternion.Identity;
                            m_AngularWindEfficiency = new Vector3(0.01, 0.01, 0.01);
                            m_LinearWindEfficiency = new Vector3(0.1, 0.1, 0.1);

                            m_InvertedBankingModifier = 1.0f;
                            m_BankingMix = 0.5f;
                            m_BankingTimescale = 5.0f;
                            m_MouselookAltitude = (float)Math.PI / 4.0f;
                            m_MouselookAzimuth = (float)Math.PI / 4.0f;
                            m_BankingAzimuth = (float)Math.PI / 2.0f;
                            m_DisableMotorsAbove = 0.0f;
                            m_DisableMotorsAfter = 0.0f;

                            m_Flags = VehicleFlags.ReactToWind;
                            break;

                        case VehicleType.Motorcycle:    // Halcyon based vehicle type
                            m_LinearFrictionTimescale = new Vector3(100.0f, 0.1f, 10.0f);
                            m_AngularFrictionTimescale = new Vector3(3.0f, 0.2f, 10.0f);
                            m_LinearMotorDirection = Vector3.Zero;
                            m_AngularMotorDirection = Vector3.Zero;
                            m_LinearMotorOffset = new Vector3(0.0f, 0.0f, -0.1f);
                            m_LinearMotorTimescale = new Vector3(0.5f, 1.0f, 1.0f);
                            m_AngularMotorTimescale = new Vector3(0.1f, 0.1f, 0.05f);
                            m_LinearMotorDecayTimescale = new Vector3(10.0f, 1.0f, 1.0f);
                            m_AngularMotorDecayTimescale = new Vector3(0.2f, 0.8f, 0.1f);
                            m_LinearWindEfficiency = Vector3.Zero;
                            m_AngularWindEfficiency = Vector3.Zero;

                            m_HoverHeight = 0.0f;
                            m_HoverEfficiency = 0.0f;
                            m_HoverTimescale = 1000.0f;
                            m_Buoyancy = 0.0f;
                            m_LinearDeflectionEfficiency = 1.0f;
                            m_LinearDeflectionTimescale = 2.0f;
                            m_AngularDeflectionEfficiency = 0.8f;
                            m_AngularDeflectionTimescale = 2.0f;
                            m_VerticalAttractionEfficiency = 1.0f;
                            m_VerticalAttractionTimescale = 1.0f;
                            m_BankingEfficiency = 0.95f;
                            m_ReferenceFrame = Quaternion.Identity;

                            m_InvertedBankingModifier = -0.5f;
                            m_BankingMix = 0.5f;
                            m_BankingTimescale = 0.1f;
                            m_MouselookAltitude = (float)Math.PI / 4.0f;
                            m_MouselookAzimuth = (float)Math.PI / 4.0f;
                            m_BankingAzimuth = (float)Math.PI / 2.0f;
                            m_DisableMotorsAbove = 1.5f;
                            m_DisableMotorsAfter = 2.5f;

                            m_Flags = VehicleFlags.NoDeflectionUp | VehicleFlags.HoverUpOnly | VehicleFlags.LimitMotorUp | VehicleFlags.LimitMotorDown |
                                            VehicleFlags.LimitRollOnly | VehicleFlags.TorqueWorldZ;
                            break;

                        case VehicleType.Sailboat:  // Halcyon-based vehicle type
                            m_LinearFrictionTimescale = new Vector3(200.0f, 0.5f, 3.0f);
                            m_AngularFrictionTimescale = new Vector3(10.0f, 1.0f, 0.2f);
                            m_LinearMotorDirection = Vector3.Zero;
                            m_AngularMotorDirection = Vector3.Zero;
                            m_LinearMotorOffset = Vector3.Zero;
                            m_LinearMotorTimescale = new Vector3(1.0f, 5.0f, 5.0f);
                            m_AngularMotorTimescale = new Vector3(2.0f, 2.0f, 0.1f);
                            m_LinearMotorDecayTimescale = new Vector3(1.0f, 10.0f, 10.0f);
                            m_AngularMotorDecayTimescale = new Vector3(0.3f, 0.3f, 0.1f);
                            m_LinearWindEfficiency = new Vector3(0.02f, 0.001f, 0.0f);
                            m_AngularWindEfficiency = new Vector3(0.1f, 0.01f, 0.0f);

                            m_HoverHeight = 0.0001f;
                            m_HoverEfficiency = 0.8f;
                            m_HoverTimescale = 0.5f;
                            m_Buoyancy = 0.0f;
                            m_LinearDeflectionEfficiency = 0.5f;
                            m_LinearDeflectionTimescale = 3.0f;
                            m_AngularDeflectionEfficiency = 0.5f;
                            m_AngularDeflectionTimescale = 5.0f;
                            m_VerticalAttractionEfficiency = 0.5f;
                            m_VerticalAttractionTimescale = 0.3f;
                            m_BankingEfficiency = 0.8f;
                            m_InvertedBankingModifier = -0.2f;
                            m_BankingMix = 0.5f;
                            m_BankingTimescale = 0.5f;
                            m_MouselookAltitude = Math.PI / 4.0f;
                            m_MouselookAzimuth = Math.PI / 4.0f;
                            m_BankingAzimuth = Math.PI / 2.0f;
                            m_DisableMotorsAbove = 0.0f;
                            m_DisableMotorsAfter = 0.0f;

                            m_ReferenceFrame = Quaternion.Identity;

                            m_Flags = VehicleFlags.NoDeflectionUp | VehicleFlags.HoverWaterOnly |
                                VehicleFlags.LimitMotorUp | VehicleFlags.LimitMotorDown |
                                VehicleFlags.ReactToWind | VehicleFlags.ReactToCurrents |
                                VehicleFlags.TorqueWorldZ;
                            break;

                        default:
                            throw new InvalidOperationException();
                    }

                    if (value != VehicleType.None)
                    {
                        m_OneByAngularDeflectionTimescale = 1 / m_AngularDeflectionEfficiency;
                        m_OneByAngularFrictonTimescale = Vector3.One.ElementDivide(m_AngularFrictionTimescale);
                        m_OneByAngularMotorDecayTimescale = Vector3.One.ElementDivide(m_AngularMotorDecayTimescale);
                        m_OneByAngularMotorTimescale = Vector3.One.ElementDivide(m_AngularMotorTimescale);
                        m_OneByBankingTimescale = 1 / m_BankingTimescale;
                        m_OneByHoverTimescale = 1 / m_HoverTimescale;
                        m_OneByLinearDeflectionTimescale = 1 / m_LinearDeflectionTimescale;
                        m_OneByLinearFrictionTimescale = Vector3.One.ElementDivide(m_LinearFrictionTimescale);
                        m_OneByLinearMotorDecayTimescale = Vector3.One.ElementDivide(m_LinearMotorDecayTimescale);
                        m_OneByLinearMotorTimescale = Vector3.One.ElementDivide(m_LinearMotorTimescale);
                        m_OneByVerticalAttractionTimescale = 1 / m_VerticalAttractionTimescale;
                    }
                    m_VehicleType = value;
                }
            }
        }

        public VehicleFlags Flags
        {
            get { return m_Flags; }

            set
            {
                lock (m_VehicleParamLock)
                {
                    m_Flags = value;
                }
            }
        }

        public void SetFlags(VehicleFlags value)
        {
            lock (m_VehicleParamLock)
            {
                m_Flags |= value;
            }
        }

        public void ClearFlags(VehicleFlags value)
        {
            lock (m_VehicleParamLock)
            {
                m_Flags &= ~value;
            }
        }

        public Quaternion this[VehicleRotationParamId id]
        {
            get
            {
                lock(m_VehicleParamLock)
                {
                    switch (id)
                    {
                        case VehicleRotationParamId.ReferenceFrame:
                            return m_ReferenceFrame;

                        default:
                            throw new KeyNotFoundException();
                    }
                }
            }
            set
            {
                lock (m_VehicleParamLock)
                {
                    switch (id)
                    {
                        case VehicleRotationParamId.ReferenceFrame:
                            m_ReferenceFrame = value;
                            break;

                        default:
                            throw new KeyNotFoundException();
                    }
                }
            }
        }

        public Vector3 this[VehicleVectorParamId id]
        {
            get
            {
                lock(m_VehicleParamLock)
                {
                    switch (id)
                    {
                        case VehicleVectorParamId.AngularFrictionTimescale:
                            return m_AngularFrictionTimescale;

                        case VehicleVectorParamId.AngularMotorDirection:
                            return m_AngularMotorDirection;

                        case VehicleVectorParamId.LinearFrictionTimescale:
                            return m_LinearFrictionTimescale;

                        case VehicleVectorParamId.LinearMotorDirection:
                            return m_LinearMotorDirection;

                        case VehicleVectorParamId.LinearMotorOffset:
                            return m_LinearMotorOffset;

                        case VehicleVectorParamId.AngularMotorDecayTimescale:
                            return m_AngularMotorDecayTimescale;

                        case VehicleVectorParamId.AngularMotorTimescale:
                            return m_AngularMotorTimescale;

                        case VehicleVectorParamId.LinearMotorDecayTimescale:
                            return m_LinearMotorDecayTimescale;

                        case VehicleVectorParamId.LinearMotorTimescale:
                            return m_LinearMotorTimescale;

                        case VehicleVectorParamId.AngularWindEfficiency:
                            return m_AngularWindEfficiency;

                        case VehicleVectorParamId.LinearWindEfficiency:
                            return m_LinearWindEfficiency;

                        default:
                            throw new KeyNotFoundException();
                    }
                }
            }
            set
            {
                lock (m_VehicleParamLock)
                {
                    switch (id)
                    {
                        case VehicleVectorParamId.AngularFrictionTimescale:
                            if (value.X > double.Epsilon && value.Y > double.Epsilon && value.Z > double.Epsilon)
                            {
                                m_OneByAngularFrictonTimescale = Vector3.One.ElementDivide(value);
                                m_AngularFrictionTimescale = value;
                            }
                            break;

                        case VehicleVectorParamId.AngularMotorDirection:
                            m_AngularMotorDirection = value;
                            break;

                        case VehicleVectorParamId.LinearFrictionTimescale:
                            m_OneByLinearFrictionTimescale = Vector3.One.ElementDivide(value);
                            m_LinearFrictionTimescale = value;
                            break;

                        case VehicleVectorParamId.LinearMotorDirection:
                            m_LinearMotorDirection = value;
                            break;

                        case VehicleVectorParamId.LinearMotorOffset:
                            m_LinearMotorOffset = value;
                            break;

                        case VehicleVectorParamId.AngularMotorDecayTimescale:
                            m_OneByLinearMotorDecayTimescale = Vector3.One.ElementDivide(value);
                            m_AngularMotorDecayTimescale = value;
                            break;

                        case VehicleVectorParamId.AngularMotorTimescale:
                            if (value.X > double.Epsilon && value.Y > double.Epsilon && value.Z > double.Epsilon)
                            {
                                m_OneByAngularMotorTimescale = Vector3.One.ElementDivide(value);
                                m_AngularMotorTimescale = value;
                            }
                            break;

                        case VehicleVectorParamId.LinearMotorDecayTimescale:
                            if (value.X > double.Epsilon && value.Y > double.Epsilon && value.Z > double.Epsilon)
                            {
                                m_OneByLinearMotorDecayTimescale = Vector3.One.ElementDivide(value);
                                m_LinearMotorDecayTimescale = value;
                            }
                            break;

                        case VehicleVectorParamId.LinearMotorTimescale:
                            if (value.X > double.Epsilon && value.Y > double.Epsilon && value.Z > double.Epsilon)
                            {
                                m_OneByLinearMotorTimescale = Vector3.One.ElementDivide(value);
                                m_LinearMotorTimescale = value;
                            }
                            break;

                        case VehicleVectorParamId.AngularWindEfficiency:
                            m_AngularWindEfficiency = value;
                            break;

                        case VehicleVectorParamId.LinearWindEfficiency:
                            m_LinearWindEfficiency = value;
                            break;

                        default:
                            throw new KeyNotFoundException();
                    }
                }
            }
        }

        public double this[VehicleFloatParamId id]
        {
            get
            {
                lock(m_VehicleParamLock)
                {
                    switch (id)
                    {
                        case VehicleFloatParamId.AngularDeflectionEfficiency:
                            return m_AngularDeflectionEfficiency;

                        case VehicleFloatParamId.AngularDeflectionTimescale:
                            return m_AngularDeflectionTimescale;

                        case VehicleFloatParamId.LinearDeflectionTimescale:
                            return m_LinearDeflectionTimescale;

                        case VehicleFloatParamId.LinearDeflectionEfficiency:
                            return m_LinearDeflectionEfficiency;

                        case VehicleFloatParamId.BankingEfficiency:
                            return m_BankingEfficiency;

                        case VehicleFloatParamId.BankingMix:
                            return m_BankingMix;

                        case VehicleFloatParamId.BankingTimescale:
                            return m_BankingTimescale;

                        case VehicleFloatParamId.Buoyancy:
                            return m_Buoyancy;

                        case VehicleFloatParamId.HoverHeight:
                            return m_HoverHeight;

                        case VehicleFloatParamId.HoverEfficiency:
                            return m_HoverEfficiency;

                        case VehicleFloatParamId.HoverTimescale:
                            return m_HoverTimescale;

                        case VehicleFloatParamId.VerticalAttractionEfficiency:
                            return m_VerticalAttractionEfficiency;

                        case VehicleFloatParamId.VerticalAttractionTimescale:
                            return m_VerticalAttractionTimescale;

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

                        default:
                            throw new KeyNotFoundException();
                    }
                }
            }
            set
            {
                lock (m_VehicleParamLock)
                {
                    switch (id)
                    {
                        case VehicleFloatParamId.AngularDeflectionEfficiency:
                            m_AngularDeflectionEfficiency = value;
                            break;

                        case VehicleFloatParamId.AngularDeflectionTimescale:
                            m_OneByAngularDeflectionTimescale = 1 / value;
                            m_AngularDeflectionTimescale = value;
                            break;

                        case VehicleFloatParamId.LinearDeflectionEfficiency:
                            m_LinearDeflectionEfficiency = value.Clamp(0f, 1f);
                            break;

                        case VehicleFloatParamId.LinearDeflectionTimescale:
                            if (value > double.Epsilon)
                            {
                                m_OneByLinearDeflectionTimescale = 1 / value;
                                m_LinearDeflectionTimescale = value;
                            }
                            break;

                        case VehicleFloatParamId.BankingEfficiency:
                            m_BankingEfficiency = value.Clamp(-1f, 1f);
                            break;

                        case VehicleFloatParamId.BankingMix:
                            m_BankingMix = value.Clamp(0f, 1f);
                            break;

                        case VehicleFloatParamId.BankingTimescale:
                            if (value > double.Epsilon)
                            {
                                m_OneByBankingTimescale = 1 / value;
                                m_BankingTimescale = value;
                            }
                            break;

                        case VehicleFloatParamId.Buoyancy:
                            m_Buoyancy = value.Clamp(-1f, 1f);
                            break;

                        case VehicleFloatParamId.HoverHeight:
                            m_HoverHeight = value;
                            break;

                        case VehicleFloatParamId.HoverEfficiency:
                            m_HoverEfficiency = value.Clamp(0f, 1f);
                            break;

                        case VehicleFloatParamId.HoverTimescale:
                            if (value > double.Epsilon)
                            {
                                m_OneByHoverTimescale = 1 / value;
                                m_HoverTimescale = value;
                            }
                            break;

                        case VehicleFloatParamId.VerticalAttractionEfficiency:
                            m_VerticalAttractionEfficiency = value.Clamp(0f, 1f);
                            break;

                        case VehicleFloatParamId.VerticalAttractionTimescale:
                            if (value > double.Epsilon)
                            {
                                m_OneByVerticalAttractionTimescale = 1 / value;
                                m_VerticalAttractionTimescale = value;
                            }
                            break;

                        case VehicleFloatParamId.MouselookAzimuth:
                            m_MouselookAzimuth = value;
                            break;

                        case VehicleFloatParamId.MouselookAltitude:
                            m_MouselookAltitude = value;
                            break;

                        case VehicleFloatParamId.BankingAzimuth:
                            m_BankingAzimuth = value;
                            break;

                        case VehicleFloatParamId.DisableMotorsAbove:
                            m_DisableMotorsAbove = value;
                            break;

                        case VehicleFloatParamId.DisableMotorsAfter:
                            m_DisableMotorsAfter = value;
                            break;

                        case VehicleFloatParamId.InvertedBankingModifier:
                            m_InvertedBankingModifier = value;
                            break;

                        default:
                            throw new KeyNotFoundException();
                    }
                }
            }
        }
    }
}
