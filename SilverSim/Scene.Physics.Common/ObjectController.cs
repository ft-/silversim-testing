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

        protected ObjectController(ObjectGroup part, UUID sceneID)
        {
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

        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public abstract Vector3 DeltaLinearVelocity { set; }
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public abstract Vector3 DeltaAngularVelocity { set; }
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public abstract Vector3 ControlTargetVelocity { set; }
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

        public bool ContributesToCollisionSurfaceAsChild
        {
            get
            {
                return m_ContributesToCollisionSurfaceAsChild;
            }
            set
            {
                m_ContributesToCollisionSurfaceAsChild = value;
                UpdateCollisionInfo();
            }
        }

        #region Vehicle Calculation

        Vector3 m_AppliedForce = Vector3.Zero;
        Vector3 m_AppliedTorque = Vector3.Zero;

        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public Vector3 AppliedForce 
        { 
            set
            {
                lock (m_Lock)
                {
                    m_AppliedForce = value;
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public Vector3 AppliedTorque 
        { 
            set
            {
                lock (m_Lock)
                {
                    m_AppliedTorque = value;
                }
            }
        }

        Vector3 m_LinearImpulse = Vector3.Zero;
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public Vector3 LinearImpulse 
        { 
            set
            {
                lock(m_Lock)
                {
                    m_LinearImpulse = value;
                }
            }
        }

        Vector3 m_AngularImpulse = Vector3.Zero;
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public Vector3 AngularImpulse 
        {
            set
            {
                lock(m_Lock)
                {
                    m_AngularImpulse = value;
                }
            }
        }

        Vector3 m_AppliedInertia = Vector3.One;
        bool m_AppliedInertiaUpdate;
        public Vector3 AppliedInertia
        {
            get
            {
                lock(m_Lock)
                {
                    return m_AppliedInertia;
                }
            }
        }

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

        Vector3 m_CurrentInertia = Vector3.One;

        void ProcessAdvancedPhysics(double dt, PhysicsStateData currentState, out Vector3 linearForce, out Vector3 angularForce)
        {
            Vector3 lThrusters = Vector3.Zero;
            Vector3 hThrusters = Vector3.Zero;
            Vector3 oldInertia = Vector3.Zero;
            Vector3 newInertia = Vector3.Zero;
            bool updateInertia;

            angularForce = Vector3.Zero;

            lock(m_Lock)
            {
                updateInertia = m_AppliedInertiaUpdate;
                if (updateInertia)
                {
                    oldInertia = m_CurrentInertia;
                    newInertia = m_AppliedInertia;
                    m_CurrentInertia = newInertia;
                }
            }

            /* Jold/Jnew*omega_old=omega_new */
            if (updateInertia)
            {
                Vector3 newAngularVelocity = oldInertia.ElementDivide(newInertia).ElementMultiply(currentState.AngularVelocity);
                /* we have to divide by dt to produce dt / dt later to have immediate effect */
                angularForce += (newAngularVelocity - currentState.AngularVelocity) / dt;
            }

            foreach (KeyValuePair<UUID, Vector3> kvp in m_Group.AttachedForces)
            {
                ObjectPart part;
                if (m_Group.TryGetValue(kvp.Key, out part))
                {
                    Vector3 pos = part.LocalPosition;
                    Vector3 force = kvp.Value * part.LocalRotation;
                    if (pos.X < 0)
                    {
                        lThrusters.X += force.X / Math.Abs(pos.X);
                    }
                    else if (pos.X > 0)
                    {
                        hThrusters.X += force.Y / pos.Y;
                    }
                    if (pos.Y < 0)
                    {
                        lThrusters.Y += force.Y / Math.Abs(pos.Y);
                    }
                    else if (pos.Y > 0)
                    {
                        hThrusters.Y += force.Y / pos.Y;
                    }
                    if (pos.Z < 0)
                    {
                        lThrusters.Z += force.Z / Math.Abs(pos.Z);
                    }
                    else if (pos.Z > 0)
                    {
                        hThrusters.Z += force.Z / pos.Z;
                    }
                }
            }
            angularForce += (hThrusters - lThrusters);
            linearForce = hThrusters + lThrusters;
        }

        public void Process(double dt)
        {
            if(IsPhysicsActive)
            {
                VehicleParams vehicleParams = m_Group.VehicleParams;
                if(vehicleParams.VehicleType == VehicleType.None)
                {
                    return;
                }

                double totalMass = Mass;

                Vector3 linearForce = Vector3.Zero;
                Vector3 angularTorque = Vector3.Zero;

                linearForce += BuoyancyMotor(m_Group, dt);
                linearForce += GravityMotor(m_Group, dt);
                linearForce += HoverMotor(m_Group, dt);

                if (m_Group.AttachedForces.Count != 0)
                {
                    Vector3 advLinear;
                    Vector3 advAngular;
                    ProcessAdvancedPhysics(dt, m_StateData, out advLinear, out advAngular);
                    linearForce += advLinear;
                    angularTorque += advAngular;
                }

                m_Vehicle.Process(dt, m_StateData, m_Group.Scene, totalMass);
                linearForce += m_Vehicle.LinearForce;
                angularTorque += m_Vehicle.AngularTorque;

                lock (m_Lock)
                {
                    linearForce += m_AppliedForce;
                    angularTorque += m_AppliedTorque;
                    linearForce += m_LinearImpulse;
                    m_LinearImpulse = Vector3.Zero;
                    angularTorque += m_AngularImpulse;
                    m_AngularImpulse = Vector3.Zero;
                }

                /* process acceleration and velocity */
                m_Group.Acceleration = linearForce / totalMass;
                m_Group.AngularAcceleration = angularTorque.ElementMultiply(AppliedInertia);

                /* we need to scale the accelerations towards timescale */
                DeltaLinearVelocity = m_Group.Acceleration * dt;
                DeltaAngularVelocity = m_Group.AngularAcceleration * dt;
            }
        }

        #endregion
    }
}
