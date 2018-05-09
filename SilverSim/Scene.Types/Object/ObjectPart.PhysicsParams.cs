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

using SilverSim.Scene.Types.Object.Localization;
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

        private ReferenceBoxed<double> m_Mass = 1;
        private ReferenceBoxed<double> m_PhysicsDensity = 1000f;
        private ReferenceBoxed<double> m_PhysicsFriction = 0.6f;
        private ReferenceBoxed<double> m_PhysicsRestitution = 0.5f;
        private ReferenceBoxed<double> m_PhysicsGravityMultiplier = 1f;

        public double PhysicsDensity
        {
            get
            {
                return m_PhysicsDensity;
            }
            set
            {
                m_PhysicsDensity = value;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(0);
            }
        }

        public double Mass
        {
            get
            {
                return m_Mass;
            }
            set
            {
                m_Mass = (value < double.Epsilon) ? double.Epsilon : value;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(0);
            }
        }

        public double PhysicsFriction
        {
            get
            {
                return m_PhysicsFriction;
            }
            set
            {
                m_PhysicsFriction = value;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(0);
            }
        }

        public double PhysicsRestitution
        {
            get
            {
                return m_PhysicsRestitution;
            }
            set
            {
                m_PhysicsRestitution = value;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(0);
            }
        }

        public double PhysicsGravityMultiplier
        {
            get
            {
                return m_PhysicsGravityMultiplier;
            }
            set
            {
                m_PhysicsGravityMultiplier = value;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(0);
            }
        }
        #endregion

        private readonly RwLockedDictionary<UUID, IPhysicsObject> m_PhysicsActors = new RwLockedDictionary<UUID, IPhysicsObject>();

        public RwLockedDictionary<UUID, IPhysicsObject> PhysicsActors => m_PhysicsActors;

        public IPhysicsObject PhysicsActor
        {
            get
            {
                IPhysicsObject obj;
                SceneInterface scene = ObjectGroup?.Scene;
                if (scene == null || !m_PhysicsActors.TryGetValue(scene.ID, out obj))
                {
                    obj = DummyPhysicsObject.SharedInstance;
                }
                return obj;
            }
        }

        private readonly object m_PhysicsUpdateLock = new object();
        public void PhysicsUpdate(PhysicsStateData value)
        {
            lock (m_PhysicsUpdateLock)
            {
                var group = ObjectGroup;
                if(group == null)
                {
                    return;
                }
                var scene = group.Scene;
                if (scene != null && scene.ID == value.SceneID)
                {
                    try
                    {
                        if (m_IsSandbox &&
                            ObjectGroup?.RootPart == this &&
                            HasHitSandboxLimit(value.Position))
                        {
                            throw new HitSandboxLimitException();
                        }
                        lock (m_DataLock)
                        {
                            m_LocalPosition = value.Position;
                            m_LocalRotation = value.Rotation;
                            m_Velocity = value.Velocity;
                            m_AngularVelocity = value.AngularVelocity;
                            foreach(ObjectPartLocalizedInfo l in Localizations)
                            {
                                l.PhysicsUpdate(value);
                            }
                        }
                        Acceleration = value.Acceleration;
                        AngularAcceleration = value.AngularAcceleration;
                        TriggerOnPhysicsPositionChange();
                    }
                    catch (HitSandboxLimitException)
                    {
                        AngularVelocity = Vector3.Zero;
                        Acceleration = Vector3.Zero;
                        AngularAcceleration = Vector3.Zero;
                    }
                }
            }
        }

        #region Fields
        private bool m_IsPhantom;
        private bool m_IsPhysics;
        private bool m_IsVolumeDetect;
        private double m_Buoyancy;
        private bool m_IsRotateXEnabled = true;
        private bool m_IsRotateYEnabled = true;
        private bool m_IsRotateZEnabled = true;

        public readonly VehicleParams VehicleParams;

        #endregion

        public bool IsRotateXEnabled
        {
            get { return m_IsRotateXEnabled; }

            set
            {
                m_IsRotateXEnabled = value;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public bool IsRotateYEnabled
        {
            get { return m_IsRotateYEnabled; }

            set
            {
                m_IsRotateYEnabled = value;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public bool IsRotateZEnabled
        {
            get { return m_IsRotateZEnabled; }

            set
            {
                m_IsRotateZEnabled = value;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public bool IsPhantom
        {
            get { return m_IsPhantom; }

            set
            {
                m_IsPhantom = value;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public bool IsPhysics
        {
            get { return m_IsPhysics; }

            set
            {
                m_IsPhysics = value;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public bool IsVolumeDetect
        {
            get { return m_IsVolumeDetect; }
            set
            {
                m_IsVolumeDetect = value;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public double Buoyancy
        {
            get { return m_Buoyancy; }

            set
            {
                m_Buoyancy = value;
                PhysicsActor.Buoyancy = value;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public VehicleType VehicleType
        {
            get { return VehicleParams.VehicleType; }

            set
            {
                VehicleParams.VehicleType = value;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public VehicleFlags VehicleFlags
        {
            get { return VehicleParams.Flags; }

            set
            {
                VehicleParams.Flags = value;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public void SetVehicleFlags(VehicleFlags value)
        {
            VehicleParams.SetFlags(value);
            IncrementPhysicsParameterUpdateSerial();
            TriggerOnUpdate(UpdateChangedFlags.Physics);
        }

        public void ClearVehicleFlags(VehicleFlags value)
        {
            VehicleParams.ClearFlags(value);
            IncrementPhysicsParameterUpdateSerial();
            TriggerOnUpdate(UpdateChangedFlags.Physics);
        }

        public Quaternion this[VehicleRotationParamId id]
        {
            get { return VehicleParams[id]; }

            set
            {
                VehicleParams[id] = value;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public Vector3 this[VehicleVectorParamId id]
        {
            get { return VehicleParams[id]; }

            set
            {
                VehicleParams[id] = value;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public double this[VehicleFloatParamId id]
        {
            get { return VehicleParams[id]; }

            set
            {
                VehicleParams[id] = value;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }
    }
}
