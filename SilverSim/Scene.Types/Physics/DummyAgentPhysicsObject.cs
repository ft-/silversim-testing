// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Types;
using System.Diagnostics.CodeAnalysis;

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

            }
        }


        public void TransferState(IPhysicsObject target, Vector3 positionOffset)
        {
        }

        public void ReceiveState(PhysicsStateData data, Vector3 positionOffset)
        {
        }

        #region Injecting parameters properties
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
        public Vector3 AppliedForce { set { } }
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public Vector3 AppliedTorque { set { } }
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public Vector3 LinearImpulse { set { } }
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public Vector3 AngularImpulse { set { } }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public Vector3 ControlTargetVelocity { set { } }
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
                return false;
            }
            set
            {
            }
        }

        public static readonly DummyAgentPhysicsObject SharedInstance = new DummyAgentPhysicsObject();
    }
}
