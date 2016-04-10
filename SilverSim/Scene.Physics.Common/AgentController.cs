// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Types;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Physics.Common
{
    [SuppressMessage("Gendarme.Rules.Concurrency", "DoNotLockOnThisOrTypesRule")]
    public abstract class AgentController : CommonPhysicsController, IAgentPhysicsObject
    {
        readonly IAgent m_Agent;
        readonly PhysicsStateData m_StateData;
        readonly object m_Lock = new object();

        protected AgentController(IAgent agent, UUID sceneID)
        {
            m_Agent = agent;
            m_StateData = new PhysicsStateData(agent, sceneID);
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

        public abstract void SetDeltaLinearVelocity(Vector3 value);
        public abstract void SetDeltaAngularVelocity(Vector3 value);
        public abstract bool IsPhysicsActive { get; set; } /* disables updates of object */
        public bool IsPhantom 
        {
            get
            {
                return false;
            }
            set
            {

            }
        }


        public double Mass
        {
            get
            {
                return 2;
            }
        }

        public abstract bool IsVolumeDetect { get; set; }
        public abstract bool IsAgentCollisionActive { get; set; }

        Vector3 m_ControlTargetVelocity = Vector3.Zero;
        public void SetControlTargetVelocity(Vector3 value)
        {
            lock (m_Lock)
            {
                m_ControlTargetVelocity = value;
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        Vector3 ControlTargetVelocityInput
        {
            get
            {
                lock(m_Lock)
                {
                    return m_ControlTargetVelocity;
                }
            }
        }

        public bool ContributesToCollisionSurfaceAsChild 
        {
            get
            {
                return false;
            }
            set
            {
            }
        }

        public VehicleType VehicleType 
        {
            get
            {
                return VehicleType.None;
            }
            set
            {
            }
        }

        public VehicleFlags VehicleFlags 
        {
            get
            {
                return VehicleFlags.None;
            }
            set
            {

            }
        }

        public void SetVehicleFlags(VehicleFlags value)
        {
            /* intentionally left empty */
        }

        public void ClearVehicleFlags(VehicleFlags value)
        {
            /* intentionally left empty */
        }

        public Quaternion this[VehicleRotationParamId id]
        {
            get
            {
                return Quaternion.Identity;
            }
            set
            {

            }
        }

        public Vector3 this[VehicleVectorParamId id] 
        {
            get
            {
                return Vector3.Zero;
            }
            set
            {
            }
        }

        public double this[VehicleFloatParamId id] 
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }

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
            lock (m_Lock)
            {
                m_LinearImpulse = value;
            }
        }

        Vector3 m_AngularImpulse = Vector3.Zero;
        public void SetAngularImpulse(Vector3 value)
        {
            lock (m_Lock)
            {
                m_AngularImpulse = value;
            }
        }

        public void Process(double dt)
        {
            if (IsPhysicsActive)
            {
                Vector3 linearForce = Vector3.Zero;
                Vector3 angularTorque = Vector3.Zero;

                linearForce += BuoyancyMotor(m_Agent, dt);
                linearForce += GravityMotor(m_Agent, dt);
                linearForce += HoverMotor(m_Agent, dt);
                linearForce += TargetVelocityMotor(m_Agent, ControlTargetVelocityInput, 1f, dt);

                angularTorque += TargetRotationMotor(m_Agent, m_Agent.BodyRotation, 1f, dt);

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
                m_Agent.Acceleration = linearForce / Mass;
#warning implement inertia applied mass correctly
                m_Agent.AngularAcceleration = angularTorque / Mass;

                /* we need to scale the accelerations towards timescale */
                SetDeltaLinearVelocity(m_Agent.Acceleration * dt);
                SetDeltaAngularVelocity(m_Agent.AngularAcceleration * dt);
            }
        }
    }
}
