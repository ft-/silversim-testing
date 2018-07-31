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

using log4net;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Agent;
using System.Collections.Generic;
using AnimationState = SilverSim.Scene.Types.Agent.AgentAnimationController.AnimationState;

namespace SilverSim.Scene.Physics.Common
{
    public abstract class AgentController : CommonPhysicsController, IAgentPhysicsObject
    {
#if DEBUG
        private static readonly ILog m_Log = LogManager.GetLogger("AGENT CONTROLLER");
#endif

        public IAgent Agent { get; }
        protected readonly PhysicsStateData m_StateData;
        private readonly object m_Lock = new object();

        protected override SceneInterface.LocationInfoProvider LocationInfoProvider { get; }

        public abstract Vector3 Torque { get; }
        public abstract Vector3 Force { get; }

        protected AgentController(IAgent agent, UUID sceneID, SceneInterface.LocationInfoProvider locInfoProvider)
        {
            WalkRunSpeedSwitchThreshold = 4;
            ControlLinearInputFactor = 10.6;
            SpeedFactor = 1;
            RestitutionInputFactor = 3.2;
            FlySlowFastSpeedSwitchThreshold = 4;
            StandstillSpeedThreshold = 0.2;
            Agent = agent;
            m_StateData = new PhysicsStateData(agent, sceneID);
            LocationInfoProvider = locInfoProvider;
        }

        public void ReceiveState(PhysicsStateData data, Vector3 positionOffset)
        {
            lock (m_Lock)
            {
                m_StateData.Position = data.Position + positionOffset;
                m_StateData.Rotation = data.Rotation;
                m_StateData.Velocity = data.Velocity;
                m_StateData.AngularVelocity = data.AngularVelocity;
                m_StateData.Acceleration = data.Acceleration;
                m_StateData.AngularAcceleration = data.AngularAcceleration;
            }
        }

        public double Mass => 2;

        public abstract bool IsAgentCollisionActive { get; set; }

        private ControlFlags m_ControlFlags;
        public void SetControlFlags(ControlFlags flags)
        {
            if (Agent.SittingOnObject == null)
            {
                lock (m_Lock)
                {
                    m_ControlFlags = flags;
                }
            }
            else
            {
                lock(m_Lock)
                {
                    m_ControlFlags = ControlFlags.None;
                }
            }
        }

        private Vector3 m_ControlDirectionalInput = Vector3.Zero;
        public void SetControlDirectionalInput(Vector3 value)
        {
            lock (m_Lock)
            {
                m_ControlDirectionalInput = value;
            }
        }

        private Vector3 ControlLinearInput
        {
            get
            {
                lock(m_Lock)
                {
                    return m_ControlDirectionalInput;
                }
            }
        }

        public VehicleType VehicleType
        {
            get { return VehicleType.None; }
            set
            {
                /* nothing to do for agents */
            }
        }

        public VehicleFlags VehicleFlags
        {
            get { return VehicleFlags.None; }

            set
            {
                /* nothing to do for agents */
            }
        }

        public void SetVehicleFlags(VehicleFlags value)
        {
            /* nothing to do for agents */
        }

        public void ClearVehicleFlags(VehicleFlags value)
        {
            /* nothing to do for agents */
        }

        public Quaternion this[VehicleRotationParamId id]
        {
            get { return Quaternion.Identity; }

            set
            {
                /* nothing to do for agents */
            }
        }

        public Vector3 this[VehicleVectorParamId id]
        {
            get { return Vector3.Zero; }

            set
            {
                /* nothing to do for agents */
            }
        }

        public double this[VehicleFloatParamId id]
        {
            get { return 0; }

            set
            {
                /* nothing to do for agents */
            }
        }

        private Vector3 m_AppliedForce = Vector3.Zero;

        public void SetAppliedForce(Vector3 value)
        {
            lock (m_Lock)
            {
                m_AppliedForce = value;
            }
        }

        public void SetAppliedTorque(Vector3 value)
        {
            /* nothing to do here */
        }

        private Vector3 m_LinearImpulse = Vector3.Zero;
        public void SetLinearImpulse(Vector3 value)
        {
            lock (m_Lock)
            {
                m_LinearImpulse = value;
            }
        }

        public void SetAngularImpulse(Vector3 value)
        {
            /* nothing to do here */
        }

        protected double ControlLinearInputFactor { get; set; }
        protected double RestitutionInputFactor { get; set; }
        public double SpeedFactor { get; set; }
        protected double WalkRunSpeedSwitchThreshold { get; set; }
        protected double FlySlowFastSpeedSwitchThreshold { get; set; }
        protected double StandstillSpeedThreshold { get; set; }
        private Quaternion m_LastKnownBodyRotation = Quaternion.Identity;

        protected List<PositionalForce> CalculateForces(double dt, out Vector3 agentTorque)
        {
            var forces = new List<PositionalForce>();
            agentTorque = Vector3.Zero;
            Vector3 linearVelocity = Agent.Velocity;
            double horizontalVelocity = linearVelocity.HorizontalLength;
            if (Agent.SittingOnObject != null || Agent.SceneID != m_StateData.SceneID)
            {
                /* No animation update on disabled physics */
                return forces;
            }
            else if(Agent.IsFlying)
            {
                if (horizontalVelocity >= FlySlowFastSpeedSwitchThreshold * SpeedFactor)
                {
                    Agent.SetDefaultAnimation(Agent.IsUnderwater ? AnimationState.Swimming : AnimationState.Flying);
                }
                else if (horizontalVelocity > 0.2)
                {
                    Agent.SetDefaultAnimation(Agent.IsUnderwater ? AnimationState.SwimmingSlow : AnimationState.FlyingSlow);
                }
                else if(Agent.Velocity.Z > 0.2)
                {
                    Agent.SetDefaultAnimation(Agent.IsUnderwater ? AnimationState.SwimmingUp : AnimationState.HoveringUp);
                }
                else if(Agent.Velocity.Z < -0.2)
                {
                    Agent.SetDefaultAnimation(Agent.IsUnderwater ? AnimationState.SwimmingDown : AnimationState.HoveringDown);
                }
                else
                {
                    Agent.SetDefaultAnimation(Agent.IsUnderwater ? AnimationState.Floating : AnimationState.Hovering);
                }
                /* TODO: implement taking off */
            }
            else
            {
                Vector3 angularVelocity = Agent.AngularVelocity;
                double groundHeightDiff = Agent.GlobalPositionOnGround.Z - LocationInfoProvider.At(Agent.GlobalPosition).GroundHeight;
                bool isfalling = Agent.Velocity.Z > 0.1;
                bool standing_still = horizontalVelocity < StandstillSpeedThreshold;
                bool iscrouching = m_ControlFlags.HasDown();
                AnimationState oldState = Agent.GetDefaultAnimation();

                Vector3 bodyRotDiff = Agent.BodyRotation.GetEulerAngles() - m_LastKnownBodyRotation.GetEulerAngles();

                if (isfalling && 
                    oldState != AnimationState.Running && 
                    oldState != AnimationState.Walking && 
                    oldState != AnimationState.Crouching && 
                    oldState != AnimationState.CrouchWalking)
                {
                    Agent.SetDefaultAnimation(AnimationState.FallingDown);
                }
                else if (iscrouching)
                {
                    Agent.SetDefaultAnimation(standing_still ? AnimationState.Crouching : AnimationState.CrouchWalking);
                }
                else if (!standing_still)
                {
                    Agent.SetDefaultAnimation(horizontalVelocity >= WalkRunSpeedSwitchThreshold * SpeedFactor ? AnimationState.Running : AnimationState.Walking);
                }
                else if (m_ControlFlags.HasLeft())
                {
                    Agent.SetDefaultAnimation(AnimationState.TurningLeft);
                }
                else if (m_ControlFlags.HasRight())
                {
                    Agent.SetDefaultAnimation(AnimationState.TurningRight);
                }
                else
                {
                    Agent.SetDefaultAnimation(AnimationState.Standing);
                }
                /* TODO: implement striding, prejumping, jumping, soft landing */
            }

            m_LastKnownBodyRotation = Agent.BodyRotation;

            forces.Add(BuoyancyMotor(this, Agent, Vector3.Zero));
            Vector3 currentPosition = Agent.GlobalPosition;
            Vector3 size = Agent.Size;
            if (!Agent.IsFlying && currentPosition.Z - size.Z / 2 > LocationInfoProvider.At(currentPosition).GroundHeight)
            {
                forces.Add(GravityMotor(this, Agent, Vector3.Zero));
            }
            forces.Add(HoverMotor(this, Agent, Vector3.Zero));
            forces.Add(new PositionalForce("ControlInput", ControlLinearInput * ControlLinearInputFactor * SpeedFactor, Vector3.Zero));
            Vector3 restitutionForce = -Agent.Velocity * RestitutionInputFactor;
            if(!Agent.IsFlying)
            {
                restitutionForce.Z = 0;
            }
            forces.Add(new PositionalForce("LinearRestitutionMotor", restitutionForce, Vector3.Zero));

            /* let us allow advanced physics force input to be used on agents */
            foreach (ObjectGroup grp in Agent.Attachments.All)
            {
                foreach (KeyValuePair<UUID, Vector3> kvp in grp.AttachedForces)
                {
                    ObjectPart part;
                    if (grp.TryGetValue(kvp.Key, out part))
                    {
                        forces.Add(new PositionalForce("AdvPhysics", kvp.Value, part.LocalPosition + grp.Position));
                    }
                }
            }

            lock (m_Lock)
            {
                forces.Add(new PositionalForce("LinearImpulse", m_LinearImpulse, Vector3.Zero));
                m_LinearImpulse = Vector3.Zero;
                forces.Add(new PositionalForce("AppliedForce", m_AppliedForce, Vector3.Zero));
            }

            agentTorque += LookAtMotor(Agent);

            return forces;
        }

        public bool ActivateTargetList(Vector3[] targetList) => false;

        public void DeactivateTargetList()
        {
        }

        public abstract void Process(double dt);
    }
}
