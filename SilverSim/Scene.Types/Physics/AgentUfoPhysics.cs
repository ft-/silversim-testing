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

using SilverSim.Scene.Types.Agent;
using SilverSim.Types;
using SilverSim.Types.Agent;
using System.Timers;

namespace SilverSim.Scene.Types.Physics
{
    public class AgentUfoPhysics : IAgentPhysicsObject
    {
        private readonly Timer m_UfoTimer;
        private readonly IAgent m_Agent;
        private Vector3 m_ControlTargetVelocity = Vector3.Zero;
        private Vector3 m_ControlTargetAngularVelocity = Vector3.Zero;
        private readonly PhysicsStateData m_StateData;
        private readonly object m_Lock = new object();

        public AgentUfoPhysics(IAgent agent, UUID sceneID)
        {
            m_StateData = new PhysicsStateData(agent, sceneID);

            m_UfoTimer = new Timer(0.1);
            m_UfoTimer.Elapsed += UfoTimerFunction;
            m_Agent = agent;
            m_UfoTimer.Start();
        }

        ~AgentUfoPhysics()
        {
            m_UfoTimer.Stop();
            m_UfoTimer.Elapsed -= UfoTimerFunction;
        }

        public bool IsAgentCollisionActive
        {
            get { return false; }

            set
            {
                /* intentionally left empty */
            }
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

        private void UfoTimerFunction(object sender, ElapsedEventArgs e)
        {
            Vector3 controlTarget;
            Vector3 controlAngularTarget;
            if (m_Agent.SittingOnObject != null || m_Agent.SceneID == m_StateData.SceneID)
            {
                lock (m_Lock)
                {
                    controlTarget = m_ControlTargetVelocity;
                    controlAngularTarget = m_ControlTargetAngularVelocity;
                }
                m_StateData.Position += controlTarget / 10f;
                m_StateData.Velocity = controlTarget;
                m_StateData.Rotation *= Quaternion.CreateFromEulers(controlAngularTarget / 10f);
                m_StateData.AngularVelocity = controlAngularTarget;
                var agent = m_Agent;
                if (agent != null)
                {
                    agent.PhysicsUpdate(m_StateData);
                }
            }
        }

        public void SetAppliedForce(Vector3 value)
        {
            /* intentionally left empty */
        }

        public void SetAppliedTorque(Vector3 value)
        {
            /* intentionally left empty */
        }

        public void SetLinearImpulse(Vector3 value)
        {
            /* intentionally left empty */
        }

        public void SetAngularImpulse(Vector3 value)
        {
            /* intentionally left empty */
        }

        private ControlFlags m_ControlFlags;

        public void SetControlFlags(ControlFlags flags)
        {
            m_ControlFlags = flags;
        }

        public ControlFlags GetControlFlags()
        {
            return m_ControlFlags;
        }

        public void SetControlDirectionalInput(Vector3 value)
        {
            lock (m_Lock)
            {
                m_ControlTargetVelocity = value;
            }
        }

        public void SetControlAngularInput(Vector3 value)
        {
            lock (m_Lock)
            {
                m_ControlTargetAngularVelocity = value;
            }
        }

        public Vector3 Torque => Vector3.Zero;

        public Vector3 Force => Vector3.Zero;

        public bool ContributesToCollisionSurfaceAsChild
        {
            get { return true; }

            set
            {
                /* intentionally left empty */
            }
        }

        public double Mass => 2;

        public double Buoyancy
        {
            get { return 0f; }

            set
            {
                /* intentionally left empty */
            }
        }
        public double SpeedFactor { get; set; }

        public void GroundRepel(double height, bool water, double tau)
        {
        }

        public void SetHoverHeight(double height, bool water, double tau)
        {
        }

        public void StopHover()
        {
        }

        public void SetLookAt(Quaternion q, double strength, double damping)
        {
        }

        public void StopLookAt()
        {
        }

        public bool ActivateTargetList(Vector3[] targetList) => false;

        public void DeactivateTargetList()
        {
        }
    }
}
