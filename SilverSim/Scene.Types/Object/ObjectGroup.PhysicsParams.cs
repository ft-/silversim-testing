// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using ThreadedClasses;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectGroup
    {
        readonly RwLockedDictionary<UUID, IPhysicsObject> m_PhysicsActors = new RwLockedDictionary<UUID, IPhysicsObject>();

        public RwLockedDictionary<UUID, IPhysicsObject> PhysicsActors
        {
            get
            {
                return m_PhysicsActors;
            }
        }

        public IPhysicsObject PhysicsActor
        {
            get
            {
                lock (this)
                {
                    IPhysicsObject obj;
                    SceneInterface scene = Scene;
                    if (scene == null || !m_PhysicsActors.TryGetValue(scene.ID, out obj))
                    {
                        obj = DummyPhysicsObject.SharedInstance;
                    }
                    return obj;
                }
            }
        }


        /* property here instead of a method. A lot more clear that we update something. */
        readonly object m_PhysicsUpdateLock = new object();
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public PhysicsStateData PhysicsUpdate
        {
            set
            {
                lock (m_PhysicsUpdateLock)
                {
                    if (Scene.ID == value.SceneID)
                    {
                        Position = value.Position;
                        Rotation = value.Rotation;
                        Velocity = value.Velocity;
                        AngularVelocity = value.AngularVelocity;
                        Acceleration = value.Acceleration;
                        AngularAcceleration = value.AngularAcceleration;
                    }
                }
            }
        }

        #region Fields
        bool m_IsPhantom;
        bool m_IsPhysics;
        bool m_IsVolumeDetect;
        double m_Buoyancy;

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
        struct VehicleParams
        {
            public VehicleType VehicleType;

            public Quaternion ReferenceFrame;

            public Vector3 AngularFrictionTimescale;
            public Vector3 AngularMotorDirection;
            public Vector3 LinearFrictionTimescale;
            public Vector3 LinearMotorDirection;
            public Vector3 LinearMotorOffset;

            public double AngularDeflectionEfficiency;
            public double AngularDeflectionTimescale;
            public double AngularMotorDecayTimescale;
            public double AngularMotorTimescale;
            public double BankingEfficiency;
            public double BankingMix;
            public double BankingTimescale;
            public double Buoyancy;
            public double HoverHeight;
            public double HoverEfficiency;
            public double HoverTimescale;
            public double LinearDeflectionEfficiency;
            public double LinearDeflectionTimescale;
            public double LinearMotorDecayTimescale;
            public double LinearMotorTimescale;
            public double VerticalAttractionEfficiency;
            public double VerticalAttractionTimescale;
            public VehicleFlags Flags;
        }
        VehicleParams m_Vehicle = new VehicleParams();

        #endregion

        public bool IsPhantom
        {
            get
            {
                return m_IsPhantom;
            }
            set
            {
                m_IsPhantom = value;
                PhysicsActor.IsPhantom = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public bool IsPhysics
        {
            get
            {
                return m_IsPhysics;
            }
            set
            {
                m_IsPhysics = value;
                PhysicsActor.IsPhysicsActive = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public bool IsVolumeDetect
        {
            get
            {
                return m_IsVolumeDetect;
            }
            set
            {
                m_IsVolumeDetect = value;
                PhysicsActor.IsVolumeDetect = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public double Buoyancy 
        {
            get
            {
                return m_Buoyancy;
            }
            set
            {
                m_Buoyancy = value;
                PhysicsActor.Buoyancy = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public VehicleType VehicleType 
        {
            get
            {
                return m_Vehicle.VehicleType;
            }
            set
            {
                lock (this)
                {
                    switch (value)
                    {
                        case VehicleType.None:
                            break;

                        case VehicleType.Sled:
                            m_Vehicle.LinearFrictionTimescale = new Vector3(30, 1, 1000);
                            m_Vehicle.AngularFrictionTimescale = new Vector3(1000, 1000, 1000);
                            m_Vehicle.LinearMotorDirection = new Vector3(0, 0, 0);
                            m_Vehicle.LinearMotorTimescale = 1000;
                            m_Vehicle.LinearMotorDecayTimescale = 120;
                            m_Vehicle.AngularMotorDirection = new Vector3(0, 0, 0);
                            m_Vehicle.AngularMotorTimescale = 1000;
                            m_Vehicle.AngularMotorDecayTimescale = 120;
                            m_Vehicle.HoverHeight = 0;
                            m_Vehicle.HoverEfficiency = 10;
                            m_Vehicle.HoverTimescale = 10;
                            m_Vehicle.Buoyancy = 0;
                            m_Vehicle.LinearDeflectionEfficiency = 1;
                            m_Vehicle.LinearDeflectionTimescale = 1;
                            m_Vehicle.AngularDeflectionEfficiency = 0;
                            m_Vehicle.AngularDeflectionTimescale = 10;
                            m_Vehicle.VerticalAttractionEfficiency = 1;
                            m_Vehicle.VerticalAttractionTimescale = 1000;
                            m_Vehicle.BankingEfficiency = 0;
                            m_Vehicle.BankingMix = 1;
                            m_Vehicle.BankingTimescale = 10;
                            m_Vehicle.ReferenceFrame = Quaternion.Identity;
                            m_Vehicle.Flags = VehicleFlags.NoDeflectionUp | VehicleFlags.LimitRollOnly | VehicleFlags.LimitMotorUp;
                            break;

                        case VehicleType.Car:
                            m_Vehicle.LinearFrictionTimescale = new Vector3(100, 2, 1000);
                            m_Vehicle.AngularFrictionTimescale = new Vector3(1000, 1000, 1000);
                            m_Vehicle.LinearMotorDirection = new Vector3(0, 0, 0);
                            m_Vehicle.LinearMotorTimescale = 1;
                            m_Vehicle.LinearMotorDecayTimescale = 60;
                            m_Vehicle.AngularMotorDirection = new Vector3(0, 0, 0);
                            m_Vehicle.AngularMotorTimescale = 1;
                            m_Vehicle.AngularMotorDecayTimescale = 0.8;
                            m_Vehicle.HoverHeight = 0;
                            m_Vehicle.HoverEfficiency = 0;
                            m_Vehicle.HoverTimescale = 1000;
                            m_Vehicle.Buoyancy = 0;
                            m_Vehicle.LinearDeflectionEfficiency = 1;
                            m_Vehicle.LinearDeflectionTimescale = 2;
                            m_Vehicle.AngularDeflectionEfficiency = 0;
                            m_Vehicle.AngularDeflectionTimescale = 10;
                            m_Vehicle.VerticalAttractionEfficiency = 1;
                            m_Vehicle.VerticalAttractionTimescale = 10;
                            m_Vehicle.BankingEfficiency = -0.2;
                            m_Vehicle.BankingMix = 1;
                            m_Vehicle.BankingTimescale = 1;
                            m_Vehicle.ReferenceFrame = Quaternion.Identity;
                            m_Vehicle.Flags = Types.Physics.Vehicle.VehicleFlags.NoDeflectionUp | Types.Physics.Vehicle.VehicleFlags.LimitRollOnly | Types.Physics.Vehicle.VehicleFlags.HoverUpOnly | Types.Physics.Vehicle.VehicleFlags.LimitMotorUp;
                            break;

                        case VehicleType.Boat:
                            m_Vehicle.LinearFrictionTimescale = new Vector3(10, 3, 2);
                            m_Vehicle.AngularFrictionTimescale = new Vector3(10, 10, 10);
                            m_Vehicle.LinearMotorDirection = new Vector3(0, 0, 0);
                            m_Vehicle.LinearMotorTimescale = 5;
                            m_Vehicle.LinearMotorDecayTimescale = 60;
                            m_Vehicle.AngularMotorDirection = new Vector3(0, 0, 0);
                            m_Vehicle.AngularMotorTimescale = 4;
                            m_Vehicle.AngularMotorDecayTimescale = 4;
                            m_Vehicle.HoverHeight = 0;
                            m_Vehicle.HoverEfficiency = 0.4;
                            m_Vehicle.HoverTimescale = 2;
                            m_Vehicle.Buoyancy = 1;
                            m_Vehicle.LinearDeflectionEfficiency = 0.5;
                            m_Vehicle.LinearDeflectionTimescale = 3;
                            m_Vehicle.AngularDeflectionEfficiency = 0.5;
                            m_Vehicle.AngularDeflectionTimescale = 5;
                            m_Vehicle.VerticalAttractionEfficiency = 0.5;
                            m_Vehicle.VerticalAttractionTimescale = 5;
                            m_Vehicle.BankingEfficiency = -0.3;
                            m_Vehicle.BankingMix = 0.8;
                            m_Vehicle.BankingTimescale = 1;
                            m_Vehicle.ReferenceFrame = Quaternion.Identity;
                            m_Vehicle.Flags = Types.Physics.Vehicle.VehicleFlags.NoDeflectionUp | Types.Physics.Vehicle.VehicleFlags.HoverWaterOnly | Types.Physics.Vehicle.VehicleFlags.HoverUpOnly | Types.Physics.Vehicle.VehicleFlags.LimitMotorUp;
                            break;

                        case VehicleType.Airplane:
                            m_Vehicle.LinearFrictionTimescale = new Vector3(200, 10, 5);
                            m_Vehicle.AngularFrictionTimescale = new Vector3(20, 20, 20);
                            m_Vehicle.LinearMotorDirection = new Vector3(0, 0, 0);
                            m_Vehicle.LinearMotorTimescale = 2;
                            m_Vehicle.LinearMotorDecayTimescale = 60;
                            m_Vehicle.AngularMotorDirection = new Vector3(0, 0, 0);
                            m_Vehicle.AngularMotorTimescale = 4;
                            m_Vehicle.AngularMotorDecayTimescale = 8;
                            m_Vehicle.HoverHeight = 0;
                            m_Vehicle.HoverEfficiency = 0.5;
                            m_Vehicle.HoverTimescale = 1000;
                            m_Vehicle.Buoyancy = 0;
                            m_Vehicle.LinearDeflectionEfficiency = 0.5;
                            m_Vehicle.LinearDeflectionTimescale = 0.5;
                            m_Vehicle.AngularDeflectionEfficiency = 1;
                            m_Vehicle.AngularDeflectionTimescale = 2;
                            m_Vehicle.VerticalAttractionEfficiency = 0.9;
                            m_Vehicle.VerticalAttractionTimescale = 2;
                            m_Vehicle.BankingEfficiency = 1;
                            m_Vehicle.BankingMix = 0.7;
                            m_Vehicle.BankingTimescale = 2;
                            m_Vehicle.ReferenceFrame = Quaternion.Identity;
                            m_Vehicle.Flags = Types.Physics.Vehicle.VehicleFlags.LimitRollOnly;
                            break;

                        case Types.Physics.Vehicle.VehicleType.Balloon:
                            m_Vehicle.LinearFrictionTimescale = new Vector3(5, 5, 5);
                            m_Vehicle.AngularFrictionTimescale = new Vector3(10, 10, 10);
                            m_Vehicle.LinearMotorDirection = new Vector3(0, 0, 0);
                            m_Vehicle.LinearMotorTimescale = 5;
                            m_Vehicle.LinearMotorDecayTimescale = 60;
                            m_Vehicle.AngularMotorDirection = new Vector3(0, 0, 0);
                            m_Vehicle.AngularMotorTimescale = 6;
                            m_Vehicle.AngularMotorDecayTimescale = 10;
                            m_Vehicle.HoverHeight = 5;
                            m_Vehicle.HoverEfficiency = 0.8;
                            m_Vehicle.HoverTimescale = 10;
                            m_Vehicle.Buoyancy = 1;
                            m_Vehicle.LinearDeflectionEfficiency = 0;
                            m_Vehicle.LinearDeflectionTimescale = 5;
                            m_Vehicle.AngularDeflectionEfficiency = 0;
                            m_Vehicle.AngularDeflectionTimescale = 5;
                            m_Vehicle.VerticalAttractionEfficiency = 1;
                            m_Vehicle.VerticalAttractionTimescale = 1000;
                            m_Vehicle.BankingEfficiency = 0;
                            m_Vehicle.BankingMix = 0.7;
                            m_Vehicle.BankingTimescale = 5;
                            m_Vehicle.ReferenceFrame = Quaternion.Identity;
                            m_Vehicle.Flags = Types.Physics.Vehicle.VehicleFlags.None;
                            break;

                        default:
                            throw new InvalidOperationException();
                    }
                    m_Vehicle.VehicleType = value;
                    PhysicsActor.VehicleType = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }
        public VehicleFlags VehicleFlags
        {
            get
            {
                return m_Vehicle.Flags;
            }
            set
            {
                lock (this)
                {
                    m_Vehicle.Flags = value;
                    PhysicsActor.VehicleFlags = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public VehicleFlags SetVehicleFlags
        { 
            set
            {
                lock(this)
                {
                    m_Vehicle.Flags |= value;
                    PhysicsActor.SetVehicleFlags = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public VehicleFlags ClearVehicleFlags 
        {
            set
            {
                lock (this)
                {
                    m_Vehicle.Flags &= (~value);
                    PhysicsActor.ClearVehicleFlags = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public Quaternion this[VehicleRotationParamId id]
        {
            get
            {
                switch (id)
                {
                    case VehicleRotationParamId.ReferenceFrame:
                        return m_Vehicle.ReferenceFrame;

                    default:
                        throw new KeyNotFoundException();
                }
            }
            set
            {
                lock (this)
                {
                    switch (id)
                    {
                        case VehicleRotationParamId.ReferenceFrame:
                            m_Vehicle.ReferenceFrame = value;
                            break;

                        default:
                            throw new KeyNotFoundException();
                    }
                    PhysicsActor[id] = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public Vector3 this[VehicleVectorParamId id]
        {
            get
            {
                switch (id)
                {
                    case VehicleVectorParamId.AngularFrictionTimescale:
                        return m_Vehicle.AngularFrictionTimescale;

                    case VehicleVectorParamId.AngularMotorDirection:
                        return m_Vehicle.AngularMotorDirection;

                    case VehicleVectorParamId.LinearFrictionTimescale:
                        return m_Vehicle.LinearFrictionTimescale;

                    case VehicleVectorParamId.LinearMotorDirection:
                        return m_Vehicle.LinearMotorDirection;

                    case VehicleVectorParamId.LinearMotorOffset:
                        return m_Vehicle.LinearMotorOffset;

                    default:
                        throw new KeyNotFoundException();
                }
            }
            set
            {
                lock (this)
                {
                    switch (id)
                    {
                        case VehicleVectorParamId.AngularFrictionTimescale:
                            m_Vehicle.AngularFrictionTimescale = value;
                            break;

                        case VehicleVectorParamId.AngularMotorDirection:
                            m_Vehicle.AngularMotorDirection = value;
                            break;

                        case VehicleVectorParamId.LinearFrictionTimescale:
                            m_Vehicle.LinearFrictionTimescale = value;
                            break;

                        case VehicleVectorParamId.LinearMotorDirection:
                            m_Vehicle.LinearMotorDirection = value;
                            break;

                        case VehicleVectorParamId.LinearMotorOffset:
                            m_Vehicle.LinearMotorOffset = value;
                            break;

                        default:
                            throw new KeyNotFoundException();
                    }
                    PhysicsActor[id] = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public double this[VehicleFloatParamId id]
        {
            get
            {
                switch (id)
                {
                    case VehicleFloatParamId.AngularDeflectionEfficiency:
                        return m_Vehicle.AngularDeflectionEfficiency;

                    case VehicleFloatParamId.AngularDeflectionTimescale:
                        return m_Vehicle.AngularDeflectionTimescale;

                    case VehicleFloatParamId.AngularMotorDecayTimescale:
                        return m_Vehicle.AngularMotorDecayTimescale;

                    case VehicleFloatParamId.AngularMotorTimescale:
                        return m_Vehicle.AngularMotorTimescale;

                    case VehicleFloatParamId.BankingEfficiency:
                        return m_Vehicle.BankingEfficiency;

                    case VehicleFloatParamId.BankingMix:
                        return m_Vehicle.BankingMix;

                    case VehicleFloatParamId.BankingTimescale:
                        return m_Vehicle.BankingTimescale;

                    case VehicleFloatParamId.Buoyancy:
                        return m_Vehicle.Buoyancy;

                    case VehicleFloatParamId.HoverHeight:
                        return m_Vehicle.HoverHeight;

                    case VehicleFloatParamId.HoverEfficiency:
                        return m_Vehicle.HoverEfficiency;

                    case VehicleFloatParamId.HoverTimescale:
                        return m_Vehicle.HoverTimescale;

                    case VehicleFloatParamId.LinearDeflectionEfficiency:
                        return m_Vehicle.LinearDeflectionEfficiency;

                    case VehicleFloatParamId.LinearDeflectionTimescale:
                        return m_Vehicle.LinearDeflectionTimescale;

                    case VehicleFloatParamId.LinearMotorDecayTimescale:
                        return m_Vehicle.LinearMotorDecayTimescale;

                    case VehicleFloatParamId.LinearMotorTimescale:
                        return m_Vehicle.LinearMotorTimescale;

                    case VehicleFloatParamId.VerticalAttractionEfficiency:
                        return m_Vehicle.VerticalAttractionEfficiency;

                    case VehicleFloatParamId.VerticalAttractionTimescale:
                        return m_Vehicle.VerticalAttractionTimescale;

                    default:
                        throw new KeyNotFoundException();
                }
            }
            set
            {
                lock (this)
                {
                    switch (id)
                    {
                        case VehicleFloatParamId.AngularDeflectionEfficiency:
                            m_Vehicle.AngularDeflectionEfficiency = value;
                            break;

                        case VehicleFloatParamId.AngularDeflectionTimescale:
                            m_Vehicle.AngularDeflectionTimescale = value;
                            break;

                        case VehicleFloatParamId.AngularMotorDecayTimescale:
                            m_Vehicle.AngularMotorDecayTimescale = value;
                            break;

                        case VehicleFloatParamId.AngularMotorTimescale:
                            m_Vehicle.AngularMotorTimescale = value;
                            break;

                        case VehicleFloatParamId.BankingEfficiency:
                            m_Vehicle.BankingEfficiency = value;
                            break;

                        case VehicleFloatParamId.BankingMix:
                            m_Vehicle.BankingMix = value;
                            break;

                        case VehicleFloatParamId.BankingTimescale:
                            m_Vehicle.BankingTimescale = value;
                            break;

                        case VehicleFloatParamId.Buoyancy:
                            m_Vehicle.Buoyancy = value;
                            break;

                        case VehicleFloatParamId.HoverHeight:
                            m_Vehicle.HoverHeight = value;
                            break;

                        case VehicleFloatParamId.HoverEfficiency:
                            m_Vehicle.HoverEfficiency = value;
                            break;

                        case VehicleFloatParamId.HoverTimescale:
                            m_Vehicle.HoverTimescale = value;
                            break;

                        case VehicleFloatParamId.LinearDeflectionEfficiency:
                            m_Vehicle.LinearDeflectionEfficiency = value;
                            break;

                        case VehicleFloatParamId.LinearDeflectionTimescale:
                            m_Vehicle.LinearDeflectionTimescale = value;
                            break;

                        case VehicleFloatParamId.LinearMotorDecayTimescale:
                            m_Vehicle.LinearMotorDecayTimescale = value;
                            break;

                        case VehicleFloatParamId.LinearMotorTimescale:
                            m_Vehicle.LinearMotorTimescale = value;
                            break;

                        case VehicleFloatParamId.VerticalAttractionEfficiency:
                            m_Vehicle.VerticalAttractionEfficiency = value;
                            break;

                        case VehicleFloatParamId.VerticalAttractionTimescale:
                            m_Vehicle.VerticalAttractionTimescale = value;
                            break;

                        default:
                            throw new KeyNotFoundException();
                    }
                    PhysicsActor[id] = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

    }
}
