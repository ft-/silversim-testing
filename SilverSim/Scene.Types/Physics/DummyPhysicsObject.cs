/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

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
        public Vector3 LinearVelocity 
        { 
            set 
            { 
            }
        }

        public Vector3 AngularVelocity 
        {
            set 
            { 
            } 
        }
        #endregion

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
