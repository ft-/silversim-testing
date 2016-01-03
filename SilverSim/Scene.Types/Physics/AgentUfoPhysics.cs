// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Timers;

namespace SilverSim.Scene.Types.Physics
{
    [SuppressMessage("Gendarme.Rules.Concurrency", "DoNotLockOnThisOrTypesRule")]
    [SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule")]
    public class AgentUfoPhysics : IAgentPhysicsObject
    {
        readonly Timer m_UfoTimer;
        readonly IAgent m_Agent;
        Vector3 m_ControlTargetVelocity = Vector3.Zero;
        readonly PhysicsStateData m_StateData;

        public AgentUfoPhysics(IAgent agent, UUID sceneID)
        {
            m_StateData = new PhysicsStateData(agent, sceneID);

            m_UfoTimer = new Timer(0.1);
            m_UfoTimer.Elapsed += UfoTimerFunction;
            IsPhysicsActive = true;
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
            get
            {
                return false;
            }

            set
            {

            }
        }

        public void TransferState(IPhysicsObject target, Vector3 positionOffset)
        {
            lock(this)
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

        void UfoTimerFunction(object sender, ElapsedEventArgs e)
        {
            Vector3 controlTarget;
            if (IsPhysicsActive)
            {
                lock (this)
                {
                    controlTarget = m_ControlTargetVelocity;
                }
                m_StateData.Position += controlTarget / 10f;
                m_StateData.Velocity = controlTarget;
                IAgent agent = m_Agent;
                if (agent != null)
                {
                    m_StateData.Rotation = agent.BodyRotation;
                    agent.PhysicsUpdate = m_StateData;
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public Vector3 DeltaLinearVelocity
        {
            set 
            {
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public Vector3 DeltaAngularVelocity
        {
            set 
            {
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public Vector3 AppliedForce
        {
            set 
            {
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public Vector3 AppliedTorque
        {
            set 
            {
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public Vector3 LinearImpulse
        {
            set 
            {
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public Vector3 AngularImpulse
        {
            set 
            {
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
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

        public bool IsPhysicsActive
        {
            get;
            set;
        }

        public bool IsPhantom
        {
            get
            {
                return true;
            }
            set
            {
            }
        }

        public bool IsVolumeDetect
        {
            get
            {
                return false;
            }
            set
            {
            }
        }

        public bool ContributesToCollisionSurfaceAsChild
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
                return 2; 
            }
        }

        public double Buoyancy
        {
            get
            {
                return 0f;
            }
            set
            {
            }
        }
    }
}
