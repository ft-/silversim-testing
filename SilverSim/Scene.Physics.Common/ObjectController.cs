// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Physics.Common.Vehicle;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Physics.Common
{
    [SuppressMessage("Gendarme.Rules.Concurrency", "DoNotLockOnThisOrTypesRule")]
    public abstract class ObjectController : CommonPhysicsController, IPhysicsObject
    {
        protected ObjectGroup m_Group;
        protected VehicleMotor m_Vehicle;
        bool m_Phantom;
        bool m_ContributesToCollisionSurfaceAsChild;
        bool m_VolumeDetect;
        readonly PhysicsStateData m_StateData;
        readonly object m_Lock = new object();
        protected UUID SceneID { get; private set; }

        protected ObjectController(ObjectGroup part, UUID sceneID)
        {
            SceneID = sceneID;
            m_StateData = new PhysicsStateData(part, sceneID);
            m_Group = part;
            m_Phantom = true;
            m_Vehicle = m_Group.VehicleParams.GetMotor();
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

        public abstract void UpdateCollisionInfo();

        public abstract bool IsPhysicsActive { get; set; } /* disables updates of object */
        public bool IsPhantom
        {
            get
            {
                return m_Phantom;
            }
            set
            {
                m_Phantom = value;
                UpdateCollisionInfo();
            }
        }

        public bool IsVolumeDetect 
        {
            get
            {
                return m_VolumeDetect;
            }
            set
            {
                m_VolumeDetect = value;
                UpdateCollisionInfo();
            }
        }

        public bool IsAgentCollisionActive 
        {
            get
            {
                return true;
            }

            set
            {
                /* nothing to do for objects */
            }
        }

        double m_Mass = 1;

        public double Mass
        {
            get
            {
                lock(m_Lock)
                {
                    return m_Mass;
                }
            }
        }

        #region Vehicle Calculation

        Vector3 m_AppliedForce = Vector3.Zero;
        Vector3 m_AppliedTorque = Vector3.Zero;

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

        Vector3 m_LinearImpulse = Vector3.Zero;
        public void SetLinearImpulse(Vector3 value)
        { 
            lock(m_Lock)
            {
                m_LinearImpulse = value;
            }
        }

        Vector3 m_AngularImpulse = Vector3.Zero;
        public void SetAngularImpulse(Vector3 value)
        {
            lock(m_Lock)
            {
                m_AngularImpulse = value;
            }
        }

        Vector3 m_AppliedInertia = Vector3.One;
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

        Vector3 m_CenterOfGravityOffset = Vector3.Zero;

        public void UpdatePhysicsProperties()
        {
            Vector3 inertia = Vector3.One / 100000; /* introduce a very little mass to prevent 0 */
            double totalLinearMass = 0.000001; /* introduce a very little mass to prevent 0 */
            foreach(ObjectPart part in m_Group.Values)
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
            List<PositionalForce> forces = new List<PositionalForce>();
            vehicleTorque = Vector3.Zero;
            if (!IsPhysicsActive)
            {
                return forces;
            }

            forces.Add(new PositionalForce(BuoyancyMotor(m_Group), Vector3.Zero));
            forces.Add(new PositionalForce(GravityMotor(m_Group), Vector3.Zero));
            forces.Add(new PositionalForce(HoverMotor(m_Group), Vector3.Zero));

            foreach (KeyValuePair<UUID, Vector3> kvp in m_Group.AttachedForces)
            {
                ObjectPart part;
                if (m_Group.TryGetValue(kvp.Key, out part))
                {
                    forces.Add(new PositionalForce(kvp.Value, part.LocalPosition));
                }
            }

            VehicleParams vehicleParams = m_Group.VehicleParams;
            if (vehicleParams.VehicleType != VehicleType.None)
            {

                m_Vehicle.Process(dt, m_StateData, m_Group.Scene, Mass);
                forces.Add(new PositionalForce(m_Vehicle.LinearForce, Vector3.Zero));
                vehicleTorque = m_Vehicle.AngularTorque;
            }

            lock (m_Lock)
            {
                forces.Add(new PositionalForce(m_LinearImpulse, Vector3.Zero));
                m_LinearImpulse = Vector3.Zero;
                vehicleTorque += m_AngularImpulse;
                m_AngularImpulse = Vector3.Zero;
                forces.Add(new PositionalForce(m_AppliedForce, Vector3.Zero));
                vehicleTorque += m_AppliedTorque;
            }

            return forces;
        }

        public abstract void Process(double dt);

        #endregion
    }
}
