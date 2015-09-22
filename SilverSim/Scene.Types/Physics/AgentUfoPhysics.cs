// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace SilverSim.Scene.Types.Physics
{
    public class AgentUfoPhysics : IPhysicsObject, IDisposable
    {
        Timer m_UfoTimer;
        IAgent m_Agent;
        Vector3 m_ControlTargetVelocity = Vector3.Zero;

        public AgentUfoPhysics(IAgent agent)
        {
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

        public void Dispose()
        {
            m_UfoTimer.Stop();
            m_UfoTimer.Elapsed -= UfoTimerFunction;
        }

        void UfoTimerFunction(object sender, ElapsedEventArgs e)
        {
            Vector3 controlTarget;
            lock (this)
            {
                controlTarget = m_ControlTargetVelocity;
            }
            m_Agent.Position += controlTarget / 10f;
        }

        public Vector3 DeltaLinearVelocity
        {
            set 
            {
            }
        }

        public Vector3 DeltaAngularVelocity
        {
            set 
            {
            }
        }

        public Vector3 AppliedForce
        {
            set 
            {
            }
        }

        public Vector3 AppliedTorque
        {
            set 
            {
            }
        }

        public Vector3 LinearImpulse
        {
            set 
            {
            }
        }

        public Vector3 AngularImpulse
        {
            set 
            {
            }
        }

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
            get
            {
                return false;
            }
            set
            {
            }
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

        public Vehicle.VehicleType VehicleType
        {
            get
            {
                return Vehicle.VehicleType.None;
            }
            set
            {
            }
        }

        public Vehicle.VehicleFlags VehicleFlags
        {
            get
            {
                return Vehicle.VehicleFlags.None;
            }
            set
            {
            }
        }

        public Vehicle.VehicleFlags SetVehicleFlags
        {
            set 
            { 
            }
        }

        public Vehicle.VehicleFlags ClearVehicleFlags
        {
            set 
            {
            }
        }

        public Quaternion this[Vehicle.VehicleRotationParamId id]
        {
            get
            {
                return Quaternion.Identity;
            }
            set
            {
            }
        }

        public Vector3 this[Vehicle.VehicleVectorParamId id]
        {
            get
            {
                return Vector3.Zero;
            }
            set
            {
            }
        }

        public double this[Vehicle.VehicleFloatParamId id]
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
