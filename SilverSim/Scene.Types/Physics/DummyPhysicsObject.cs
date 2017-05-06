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

using SilverSim.Types;

namespace SilverSim.Scene.Types.Physics
{
    public class DummyPhysicsObject : IPhysicsObject
    {
        public DummyPhysicsObject()
        {

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

        public Vector3 Torque
        {
            get
            {
                return Vector3.Zero;
            }
        }

        public Vector3 Force
        {
            get
            {
                return Vector3.Zero;
            }
        }

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
                /* intentionally left empty */
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
                return false;
            }

            set
            {
                /* intentionally left empty */
            }
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

        public static readonly DummyPhysicsObject SharedInstance = new DummyPhysicsObject();
    }
}
