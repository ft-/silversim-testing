﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Types;
using System.Diagnostics.CodeAnalysis;
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
        readonly object m_Lock = new object();

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
            get
            {
                return false;
            }

            set
            {
                /* intentionally left empty */
            }
        }

        public void TransferState(IPhysicsObject target, Vector3 positionOffset)
        {
            lock(m_Lock)
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

        void UfoTimerFunction(object sender, ElapsedEventArgs e)
        {
            Vector3 controlTarget;
            if (IsPhysicsActive)
            {
                lock (m_Lock)
                {
                    controlTarget = m_ControlTargetVelocity;
                }
                m_StateData.Position += controlTarget / 10f;
                m_StateData.Velocity = controlTarget;
                IAgent agent = m_Agent;
                if (agent != null)
                {
                    m_StateData.Rotation = agent.BodyRotation;
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

        public void SetControlDirectionalInput(Vector3 value)
        {
            lock (m_Lock)
            {
                m_ControlTargetVelocity = value;
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
                /* intentionally left empty */
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
                /* intentionally left empty */
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
                /* intentionally left empty */
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
                /* intentionally left empty */
            }
        }

        public bool IsRotateXEnabled
        {
            get
            {
                return false;
            }

            set
            {
                /* intentionally left empty */
            }
        }

        public bool IsRotateYEnabled
        {
            get
            {
                return false;
            }

            set
            {
                /* intentionally left empty */
            }
        }

        public bool IsRotateZEnabled
        {
            get
            {
                return true;
            }

            set
            {
                /* intentionally left empty */
            }
        }
    }
}
