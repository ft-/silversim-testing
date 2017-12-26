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
using SilverSim.Threading;
using SilverSim.Types;
using System;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectGroup
    {
        public RwLockedDictionary<UUID, IPhysicsObject> PhysicsActors
        {
            get { throw new NotSupportedException(); }
        }

        public IPhysicsObject PhysicsActor => RootPart.PhysicsActor;

        public void PhysicsUpdate(PhysicsStateData value)
        {
            throw new NotSupportedException();
        }

        public bool IsRotateXEnabled
        {
            get { return RootPart.IsRotateXEnabled; }

            set { RootPart.IsRotateXEnabled = value; }
        }

        public bool IsRotateYEnabled
        {
            get { return RootPart.IsRotateYEnabled; }

            set { RootPart.IsRotateYEnabled = value; }
        }

        public bool IsRotateZEnabled
        {
            get { return RootPart.IsRotateZEnabled; }

            set { RootPart.IsRotateZEnabled = value; }
        }

        private readonly object m_PhysicsLinksetUpdateLock = new object();

        public ObjectPart.CollisionFilterParam CollisionFilter
        {
            get { return RootPart.CollisionFilter; }
            set { RootPart.CollisionFilter = value; }
        }

        public bool IsPhantom
        {
            get { return RootPart.IsPhantom; }

            set
            {
                lock (m_PhysicsLinksetUpdateLock)
                {
                    foreach (ObjectPart part in Values)
                    {
                        if (part.IsPhantom != value)
                        {
                            part.IsPhantom = value;
                        }
                    }
                }
            }
        }

        public bool IsPhysics
        {
            get { return RootPart.IsPhysics; }

            set
            {
                lock (m_PhysicsLinksetUpdateLock)
                {
                    foreach (ObjectPart part in Values)
                    {
                        if (part.IsPhysics != value)
                        {
                            part.IsPhysics = value;
                        }
                    }
                }
            }
        }

        public bool IsVolumeDetect
        {
            get { return RootPart.IsVolumeDetect; }

            set
            {
                lock (m_PhysicsLinksetUpdateLock)
                {
                    foreach (ObjectPart part in Values)
                    {
                        if (part.IsVolumeDetect != value)
                        {
                            part.IsVolumeDetect = value;
                        }
                    }
                }
            }
        }

        public double Buoyancy
        {
            get { return RootPart.Buoyancy; }

            set { RootPart.Buoyancy = value; }
        }

        public VehicleType VehicleType
        {
            get { return RootPart.VehicleType; }

            set { RootPart.VehicleType = value; }
        }

        public VehicleFlags VehicleFlags
        {
            get { return RootPart.VehicleFlags; }

            set { RootPart.VehicleFlags = value; }
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
            get { return RootPart[id]; }

            set { RootPart[id] = value; }
        }

        public Vector3 this[VehicleVectorParamId id]
        {
            get { return RootPart[id]; }

            set { RootPart[id] = value; }
        }

        public double this[VehicleFloatParamId id]
        {
            get { return RootPart[id]; }

            set { RootPart[id] = value; }
        }
    }
}
