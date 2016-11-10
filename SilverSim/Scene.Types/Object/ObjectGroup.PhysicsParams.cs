// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Threading;
using SilverSim.Types;
using System;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectGroup
    {
        public RwLockedDictionary<UUID, IPhysicsObject> PhysicsActors
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public IPhysicsObject PhysicsActor
        {
            get
            {
                return RootPart.PhysicsActor;
            }
        }


        public void PhysicsUpdate(PhysicsStateData value)
        {
            throw new NotSupportedException();
        }

        public bool IsRotateXEnabled
        {
            get
            {
                return RootPart.IsRotateXEnabled;
            }
            set
            {
                RootPart.IsRotateXEnabled = value;
            }
        }

        public bool IsRotateYEnabled
        {
            get
            {
                return RootPart.IsRotateYEnabled;
            }
            set
            {
                RootPart.IsRotateYEnabled = value;
            }
        }

        public bool IsRotateZEnabled
        {
            get
            {
                return RootPart.IsRotateZEnabled;
            }
            set
            {
                RootPart.IsRotateZEnabled = value;
            }
        }

        object m_PhysicsLinksetUpdateLock = new object();

        public bool IsPhantom
        {
            get
            {
                return RootPart.IsPhantom;
            }
            set
            {
                lock (m_PhysicsLinksetUpdateLock)
                {
                    foreach (ObjectPart part in Values)
                    {
                        part.IsPhantom = value;
                    }
                }
            }
        }

        public bool IsPhysics
        {
            get
            {
                return RootPart.IsPhysics;
            }
            set
            {
                lock (m_PhysicsLinksetUpdateLock)
                {
                    foreach (ObjectPart part in Values)
                    {
                        part.IsPhysics = value;
                    }
                }
            }
        }

        public bool IsVolumeDetect
        {
            get
            {
                return RootPart.IsVolumeDetect;
            }
            set
            {
                lock (m_PhysicsLinksetUpdateLock)
                {
                    foreach (ObjectPart part in Values)
                    {
                        part.IsVolumeDetect = value;
                    }
                }
            }
        }

        public double Buoyancy 
        {
            get
            {
                return RootPart.Buoyancy;
            }
            set
            {
                RootPart.Buoyancy = value;
            }
        }

        public VehicleType VehicleType 
        {
            get
            {
                return RootPart.VehicleType;
            }
            set
            {
                RootPart.VehicleType = value;
            }
        }
        public VehicleFlags VehicleFlags
        {
            get
            {
                return RootPart.VehicleFlags;
            }
            set
            {
                RootPart.VehicleFlags = value;
            }
        }

        public void SetVehicleFlags(VehicleFlags value)
        {
            RootPart.SetVehicleFlags(value);
        }

        public void ClearVehicleFlags(VehicleFlags value) 
        {
            RootPart.ClearVehicleFlags(value);
        }

        public Quaternion this[VehicleRotationParamId id]
        {
            get
            {
                return RootPart[id];
            }
            set
            {
                RootPart[id] = value;
            }
        }

        public Vector3 this[VehicleVectorParamId id]
        {
            get
            {
                return RootPart[id];
            }
            set
            {
                RootPart[id] = value;
            }
        }

        public double this[VehicleFloatParamId id]
        {
            get
            {
                return RootPart[id];
            }
            set
            {
                RootPart[id] = value;
            }
        }
    }
}
