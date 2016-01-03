// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Physics.Common.Vehicle;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Types;
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

        protected ObjectController(ObjectGroup part, UUID sceneID)
        {
            m_StateData = new PhysicsStateData(part, sceneID);
            m_Group = part;
            m_Phantom = true;
            m_Vehicle = m_Group.VehicleParams.GetMotor();
        }

        public void TransferState(IPhysicsObject target, Vector3 positionOffset)
        {
            lock (this)
            {
                IsPhysicsActive = false;
                target.ReceiveState(m_StateData, positionOffset);
                IsPhysicsActive = true;
            }
        }

        public void ReceiveState(PhysicsStateData data, Vector3 positionOffset)
        {
            lock (this)
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


        public double Mass
        {
            get
            {
#warning Implement Mass
                return 1;
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
                lock (this)
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
                lock (this)
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
                lock(this)
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
                lock(this)
                {
                    m_AngularImpulse = value;
                }
            }
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

                Vector3 linearForce = Vector3.Zero;
                Vector3 angularTorque = Vector3.Zero;

                linearForce += BuoyancyMotor(m_Group, dt);
                linearForce += GravityMotor(m_Group, dt);
                linearForce += HoverMotor(m_Group, dt);

                m_Vehicle.Process(dt, m_StateData);
                linearForce += m_Vehicle.LinearForce;
                angularTorque += m_Vehicle.AngularTorque;

                lock(this)
                {
                    linearForce += m_AppliedForce;
                    angularTorque += m_AppliedTorque;
                    linearForce += m_LinearImpulse;
                    m_LinearImpulse = Vector3.Zero;
                    angularTorque += m_AngularImpulse;
                    m_AngularImpulse = Vector3.Zero;
                }

                /* process acceleration and velocity */
                m_Group.Acceleration = linearForce / Mass;
#warning implement inertia applied mass correctly
                m_Group.AngularAcceleration = angularTorque / Mass;

                /* we need to scale the accelerations towards timescale */
                DeltaLinearVelocity = m_Group.Acceleration * dt;
                DeltaAngularVelocity = m_Group.AngularAcceleration * dt;
            }
        }

        #endregion
    }
}
