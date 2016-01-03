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

        public readonly VehicleParams VehicleParams = new VehicleParams();

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
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public VehicleFlags SetVehicleFlags
        { 
            set
            {
                VehicleParams.SetFlags = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public VehicleFlags ClearVehicleFlags 
        {
            set
            {
                VehicleParams.ClearFlags = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
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
