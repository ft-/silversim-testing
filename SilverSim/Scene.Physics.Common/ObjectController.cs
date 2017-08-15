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

using SilverSim.Scene.Physics.Common.Vehicle;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Scene.Physics.Common
{
    public abstract class ObjectController : CommonPhysicsController, IPhysicsObject
    {
        protected ObjectPart m_Part;
        protected VehicleMotor m_Vehicle;
        protected readonly PhysicsStateData m_StateData;
        private readonly object m_Lock = new object();
        protected UUID SceneID { get; }

        protected override SceneInterface.LocationInfoProvider LocationInfoProvider { get; }

        public abstract Vector3 Torque { get; }
        public abstract Vector3 Force { get; }

        protected ObjectController(ObjectPart part, UUID sceneID, SceneInterface.LocationInfoProvider locInfoProvider)
        {
            SceneID = sceneID;
            m_StateData = new PhysicsStateData(part, sceneID);
            m_Part = part;
            m_Vehicle = part.VehicleParams.GetMotor();
            LocationInfoProvider = locInfoProvider;
        }

        public void TransferState(IPhysicsObject target, Vector3 positionOffset)
        {
            lock (m_Lock)
            {
                IsPhysicsActive = false;
                target.ReceiveState(m_StateData, positionOffset);
                IsPhysicsActive = true;
            }
        }

        public void ReceiveState(PhysicsStateData data, Vector3 positionOffset)
        {
            lock (m_Lock)
            {
                IsPhysicsActive = false;
                m_StateData.Position = data.Position + positionOffset;
                m_StateData.Rotation = data.Rotation;
                m_StateData.Velocity = data.Velocity;
                m_StateData.AngularVelocity = data.AngularVelocity;
                m_StateData.Acceleration = data.Acceleration;
                m_StateData.AngularAcceleration = data.AngularAcceleration;
                IsPhysicsActive = true;
            }
        }

        public abstract bool IsPhysicsActive { get; set; } /* disables updates of object */

        public bool IsAgentCollisionActive
        {
            get { return true; }

            set
            {
                /* nothing to do for objects */
            }
        }

        private double m_Mass = 1;

        public double Mass
        {
            get
            {
                lock(m_Lock)
                {
                    foreach(ObjectPart part in m_Part.ObjectGroup.ValuesByKey1)
                    {
                        m_Mass += part.Mass;
                    }
                    return m_Mass;
                }
            }
        }

        #region Vehicle Calculation

        private Vector3 m_AppliedForce = Vector3.Zero;
        private Vector3 m_AppliedTorque = Vector3.Zero;

        public void SetAppliedForce(Vector3 value)
        {
            lock (m_Lock)
            {
                m_AppliedForce = value;
            }
        }

        public void SetAppliedTorque(Vector3 value)
        {
            lock (m_Lock)
            {
                m_AppliedTorque = value;
            }
        }

        private Vector3 m_LinearImpulse = Vector3.Zero;
        public void SetLinearImpulse(Vector3 value)
        {
            lock(m_Lock)
            {
                m_LinearImpulse = value;
            }
        }

        private Vector3 m_AngularImpulse = Vector3.Zero;
        public void SetAngularImpulse(Vector3 value)
        {
            lock(m_Lock)
            {
                m_AngularImpulse = value;
            }
        }

        private Vector3 m_AppliedInertia = Vector3.One;
        protected bool m_AppliedInertiaUpdate;
        public Vector3 AppliedInertia
        {
            get
            {
                lock (m_Lock)
                {
                    return m_AppliedInertia;
                }
            }
        }

        /* to be used by physics object */
        public void GetMassAndInertia(out double mass, out Vector3 inertia)
        {
            lock(m_Lock)
            {
                mass = m_Mass;
                inertia = m_AppliedInertia;
                m_AppliedInertiaUpdate = false;
            }
        }

        private Vector3 m_CenterOfGravityOffset = Vector3.Zero;

        public void UpdatePhysicsProperties()
        {
            Vector3 inertia = Vector3.One / 100000; /* introduce a very little mass to prevent 0 */
            double totalLinearMass = 0.000001; /* introduce a very little mass to prevent 0 */
            foreach(ObjectPart part in m_Part.ObjectGroup.Values)
            {
                Vector3 pos = part.LocalPosition;
                double m = part.Mass;
                totalLinearMass += m;
                inertia += pos.ElementMultiply(pos) * m;
            }

            lock(m_Lock)
            {
                m_Mass = totalLinearMass;
                m_AppliedInertia = inertia;
                m_AppliedInertiaUpdate = true;
            }
        }

        protected List<PositionalForce> CalculateForces(double dt, out Vector3 vehicleTorque)
        {
            ObjectGroup grp = m_Part.ObjectGroup;
            var forces = new List<PositionalForce>();
            vehicleTorque = Vector3.Zero;
            if (grp == null || grp.RootPart != m_Part)
            {
                return forces;
            }
            if (!IsPhysicsActive)
            {
                return forces;
            }

            forces.Add(BuoyancyMotor(m_Part, Vector3.Zero));
            forces.Add(GravityMotor(m_Part, Vector3.Zero));
            forces.Add(HoverMotor(m_Part, Vector3.Zero));

            foreach (KeyValuePair<UUID, Vector3> kvp in grp.AttachedForces)
            {
                ObjectPart part;
                if (grp.TryGetValue(kvp.Key, out part))
                {
                    forces.Add(new PositionalForce("AttachedForces", kvp.Value, part.LocalPosition));
                }
            }

            VehicleParams vehicleParams = m_Part.VehicleParams;
            if (vehicleParams.VehicleType != VehicleType.None)
            {
                m_Vehicle.Process(dt, m_StateData, grp.Scene, Mass, m_Part.PhysicsGravityMultiplier * CombinedGravityAccelerationConstant);
                forces.Add(new PositionalForce("LinearForce", m_Vehicle.LinearForce, Vector3.Zero));
                vehicleTorque = m_Vehicle.AngularTorque;
            }

            lock (m_Lock)
            {
                forces.Add(new PositionalForce("LinearImpulse", m_LinearImpulse, Vector3.Zero));
                m_LinearImpulse = Vector3.Zero;
                vehicleTorque += m_AngularImpulse;
                m_AngularImpulse = Vector3.Zero;
                forces.Add(new PositionalForce("AppliedForce", m_AppliedForce, Vector3.Zero));
                vehicleTorque += m_AppliedTorque;
            }

            vehicleTorque += LookAtMotor(m_Part);

            return forces;
        }

        public abstract void Process(double dt);

        #endregion
    }
}
