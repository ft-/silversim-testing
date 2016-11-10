// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;

namespace SilverSim.Scene.Types.Object
{
    partial class ObjectPart
    {
        #region Physics Properties

        double m_Mass = 1;
        double m_PhysicsDensity = 1000f;
        double m_PhysicsFriction = 0.6f;
        double m_PhysicsRestitution = 0.5f;
        double m_PhysicsGravityMultiplier = 1f;

        public double PhysicsDensity
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_PhysicsDensity;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_PhysicsDensity = value;
                }
                IsChanged = m_IsChangedEnabled;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(0);
            }
        }

        public double Mass
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_Mass;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_Mass = (value < double.Epsilon) ? double.Epsilon : value;
                }
            }
        }

        public double PhysicsFriction
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_PhysicsFriction;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_PhysicsFriction = value;
                }
                IsChanged = m_IsChangedEnabled;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(0);
            }
        }

        public double PhysicsRestitution
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_PhysicsRestitution;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_PhysicsRestitution = value;
                }
                IsChanged = m_IsChangedEnabled;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(0);
            }
        }


        public double PhysicsGravityMultiplier
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_PhysicsGravityMultiplier;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_PhysicsGravityMultiplier = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }
        #endregion

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
                lock (m_DataLock)
                {
                    IPhysicsObject obj;
                    ObjectGroup group = ObjectGroup;
                    SceneInterface scene = null;
                    if (null == group)
                    {
                        scene = group.Scene;
                    }
                    if (scene == null || !m_PhysicsActors.TryGetValue(scene.ID, out obj))
                    {
                        obj = DummyPhysicsObject.SharedInstance;
                    }
                    return obj;
                }
            }
        }


        readonly object m_PhysicsUpdateLock = new object();
        public void PhysicsUpdate(PhysicsStateData value)
        {
            lock (m_PhysicsUpdateLock)
            {
                ObjectGroup group = ObjectGroup;
                if(null == group)
                {
                    return;
                }
                SceneInterface scene = group.Scene;
                if (null != scene && scene.ID == value.SceneID)
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

        #region Fields
        bool m_IsPhantom;
        bool m_IsPhysics;
        bool m_IsVolumeDetect;
        double m_Buoyancy;
        bool m_IsRotateXEnabled = true;
        bool m_IsRotateYEnabled = true;
        bool m_IsRotateZEnabled = true;

        public readonly VehicleParams VehicleParams = new VehicleParams();

        #endregion

        public bool IsRotateXEnabled
        {
            get
            {
                return m_IsRotateXEnabled;
            }
            set
            {
                m_IsRotateXEnabled = value;
                PhysicsActor.IsRotateXEnabled = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public bool IsRotateYEnabled
        {
            get
            {
                return m_IsRotateYEnabled;
            }
            set
            {
                m_IsRotateYEnabled = value;
                PhysicsActor.IsRotateYEnabled = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public bool IsRotateZEnabled
        {
            get
            {
                return m_IsRotateZEnabled;
            }
            set
            {
                m_IsRotateZEnabled = value;
                PhysicsActor.IsRotateZEnabled = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

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
                return VehicleParams.VehicleType;
            }
            set
            {
                VehicleParams.VehicleType = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }
        public VehicleFlags VehicleFlags
        {
            get
            {
                return VehicleParams.Flags;
            }
            set
            {
                VehicleParams.Flags = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public void SetVehicleFlags(VehicleFlags value)
        {
            VehicleParams.SetFlags(value);
            IsChanged = m_IsChangedEnabled;
            TriggerOnUpdate(UpdateChangedFlags.Physics);
        }

        public void ClearVehicleFlags(VehicleFlags value)
        {
            VehicleParams.ClearFlags(value);
            IsChanged = m_IsChangedEnabled;
            TriggerOnUpdate(UpdateChangedFlags.Physics);
        }

        public Quaternion this[VehicleRotationParamId id]
        {
            get
            {
                return VehicleParams[id];
            }
            set
            {
                VehicleParams[id] = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public Vector3 this[VehicleVectorParamId id]
        {
            get
            {
                return VehicleParams[id];
            }
            set
            {
                VehicleParams[id] = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public double this[VehicleFloatParamId id]
        {
            get
            {
                return VehicleParams[id];
            }
            set
            {
                VehicleParams[id] = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }
    }
}
