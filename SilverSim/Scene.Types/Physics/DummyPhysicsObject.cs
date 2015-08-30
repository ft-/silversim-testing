// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Types;

namespace SilverSim.Scene.Types.Physics
{
    public class DummyPhysicsObject : IPhysicsObject
    {
        public DummyPhysicsObject()
        {

        }

        #region Injecting parameters properties
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

        public Vector3 AppliedForce { set { } }
        public Vector3 AppliedTorque { set { } }
        public Vector3 LinearImpulse { set { } }
        public Vector3 AngularImpulse { set { } }

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

        public static readonly DummyPhysicsObject SharedInstance = new DummyPhysicsObject();
    }
}
