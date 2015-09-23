// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Types;

namespace SilverSim.Scene.Physics.Common
{
    public abstract class AgentController : CommonPhysicsController, IAgentPhysicsObject
    {
        IAgent m_Agent;
        PhysicsStateData m_StateData;

        public AgentController(IAgent agent, UUID sceneID)
        {
            m_Agent = agent;
            m_StateData = new PhysicsStateData(agent, sceneID);
        }

        public void Dispose()
        {
            m_Agent = null;
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

        public abstract Vector3 DeltaLinearVelocity { set; }
        public abstract Vector3 DeltaAngularVelocity { set; }
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
        public Vector3 ControlTargetVelocity 
        {
            set
            {
                lock (this)
                {
                    m_ControlTargetVelocity = value;
                }
            }
        }

        Vector3 ControlTargetVelocityInput
        {
            get
            {
                lock(this)
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

        public VehicleFlags SetVehicleFlags
        {
            set
            {

            }
        }

        public VehicleFlags ClearVehicleFlags
        {
            set
            {

            }
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
        public Vector3 LinearImpulse
        {
            set
            {
                lock (this)
                {
                    m_LinearImpulse = value;
                }
            }
        }

        Vector3 m_AngularImpulse = Vector3.Zero;
        public Vector3 AngularImpulse
        {
            set
            {
                lock (this)
                {
                    m_AngularImpulse = value;
                }
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

                lock (this)
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
                DeltaLinearVelocity = m_Agent.Acceleration * dt;
                DeltaAngularVelocity = m_Agent.AngularAcceleration * dt;
            }
        }
    }
}
