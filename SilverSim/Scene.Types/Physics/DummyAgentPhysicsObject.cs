// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Diagnostics.CodeAnalysis;
using System;

namespace SilverSim.Scene.Types.Physics
{
    public class DummyAgentPhysicsObject : IAgentPhysicsObject
    {
        public DummyAgentPhysicsObject()
        {

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
            /* intentionally left empty */
        }

        public void ReceiveState(PhysicsStateData data, Vector3 positionOffset)
        {
            /* intentionally left empty */
        }

        #region Injecting parameters properties
        public void SetDeltaLinearVelocity(Vector3 value)
        {
            /* intentionally left empty */
        }

        public void SetDeltaAngularVelocity(Vector3 value)
        {
            /* intentionally left empty */
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

        public void SetControlTargetVelocity(Vector3 value)
        {
            /* intentionally left empty */
        }
        #endregion

        public double Mass 
        { 
            get
            {
                return 0;
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

        public bool IsPhysicsActive 
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

        public bool IsPhantom 
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

        public bool IsRotateXEnabled
        {
            get
            {
                return false;
            }

            set
            {
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
            }
        }

        public bool IsRotateZEnabled
        {
            get
            {
                return false;
            }

            set
            {
            }
        }

        public static readonly DummyAgentPhysicsObject SharedInstance = new DummyAgentPhysicsObject();
    }
}
