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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Physics.Common
{
    [SuppressMessage("Gendarme.Rules.Concurrency", "DoNotLockOnThisOrTypesRule")]
    public abstract class AgentController : CommonPhysicsController, IAgentPhysicsObject
    {
#if DEBUG
        private static readonly ILog m_Log = LogManager.GetLogger("AGENT CONTROLLER");
#endif

        protected IAgent m_Agent { get; private set; }
        protected readonly PhysicsStateData m_StateData;
        readonly object m_Lock = new object();
        readonly SceneInterface.LocationInfoProvider m_LocInfoProvider;

        protected override SceneInterface.LocationInfoProvider LocationInfoProvider
        {
            get
            {
                return m_LocInfoProvider;
            }
        }

        protected AgentController(IAgent agent, UUID sceneID, SceneInterface.LocationInfoProvider locInfoProvider)
        {
            ControlLinearInputFactor = 10;
            ControlRotationalInputFactor = 10;
            RestitutionInputFactor = 0.8;
            m_Agent = agent;
            m_StateData = new PhysicsStateData(agent, sceneID);
            m_LocInfoProvider = locInfoProvider;
        }

        public void TransferState(IPhysicsObject target, Vector3 positionOffset)
        {
            lock (m_Lock)
            {
                bool saveState = IsPhysicsActive;
                IsPhysicsActive = false;
                target.ReceiveState(m_StateData, positionOffset);
                IsPhysicsActive = saveState;
            }
        }

        public void ReceiveState(PhysicsStateData data, Vector3 positionOffset)
        {
            lock (m_Lock)
            {
                bool saveState = IsPhysicsActive;
                IsPhysicsActive = false;
                m_StateData.Position = data.Position + positionOffset;
                m_StateData.Rotation = data.Rotation;
                m_StateData.Velocity = data.Velocity;
                m_StateData.AngularVelocity = data.AngularVelocity;
                m_StateData.Acceleration = data.Acceleration;
                m_StateData.AngularAcceleration = data.AngularAcceleration;
                IsPhysicsActive = saveState;
            }
        }

        public abstract bool IsPhysicsActive { get; set; } /* disables updates of object */
        public bool IsPhantom 
        {
            get
            {
                return false;
            }
            set
            {
                /* nothing to do for agents */
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
        public abstract bool IsRotateXEnabled { get; set; }
        public abstract bool IsRotateYEnabled { get; set; }
        public abstract bool IsRotateZEnabled { get; set; }

        Vector3 m_ControlDirectionalInput = Vector3.Zero;
        public void SetControlDirectionalInput(Vector3 value)
        {
            lock (m_Lock)
            {
#if DEBUG
                if(!value.ApproxEquals(m_ControlDirectionalInput, double.Epsilon))
                {
                    m_Log.DebugFormat("Agent control input for {0}: {1}", m_Agent.Owner.FullName, value.ToString());
                }
#endif
                m_ControlDirectionalInput = value;
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        Vector3 ControlLinearInput
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
            get
            {
                return VehicleType.None;
            }
            set
            {
                /* nothing to do for agents */
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
            get
            {
                return Quaternion.Identity;
            }
            set
            {
                /* nothing to do for agents */
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
                /* nothing to do for agents */
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
                /* nothing to do for agents */
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

        protected double ControlLinearInputFactor { get; set; }
        protected double ControlRotationalInputFactor { get; set; }
        protected double RestitutionInputFactor { get; set; }

        protected List<PositionalForce> CalculateForces(double dt, out Vector3 agentTorque)
        {
            List<PositionalForce> forces = new List<PositionalForce>();
            agentTorque = Vector3.Zero;
            if (!IsPhysicsActive)
            {
                return forces;
            }

            forces.Add(BuoyancyMotor(m_Agent, Vector3.Zero));
            forces.Add(GravityMotor(m_Agent, Vector3.Zero));
            forces.Add(HoverMotor(m_Agent, Vector3.Zero));
            forces.Add(new PositionalForce("ControlInput", ControlLinearInput * ControlLinearInputFactor, Vector3.Zero));
            forces.Add(LinearRestitutionMotor(m_Agent, RestitutionInputFactor, Vector3.Zero));

            /* let us allow advanced physics force input to be used on agents */
            foreach (ObjectGroup grp in m_Agent.Attachments.All)
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

            agentTorque = TargetRotationMotor(m_Agent, m_Agent.BodyRotation, ControlRotationalInputFactor);

            lock(m_Lock)
            {
                forces.Add(new PositionalForce("LinearImpulse", m_LinearImpulse, Vector3.Zero));
                m_LinearImpulse = Vector3.Zero;
                agentTorque += m_AngularImpulse;
                m_AngularImpulse = Vector3.Zero;
                forces.Add(new PositionalForce("AppliedForce", m_AppliedForce, Vector3.Zero));
                agentTorque += m_AppliedTorque;
            }

            return forces;
        }

        public abstract void Process(double dt);
    }
}
