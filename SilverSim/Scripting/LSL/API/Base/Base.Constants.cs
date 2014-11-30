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

using SilverSim.Types;

namespace SilverSim.Scripting.LSL.API.Base
{
    public partial class Base_API
    {
        #region LSL Constants

        [APILevel(APIFlags.LSL)]
        public const int TRUE = 1;
        [APILevel(APIFlags.LSL)]
        public const int FALSE = 0;
        [APILevel(APIFlags.LSL)]
        public const string NULL_KEY = "00000000-0000-0000-0000-000000000000";

        [APILevel(APIFlags.LSL)]
        public static readonly Vector3 ZERO_VECTOR = Vector3.Zero;
        [APILevel(APIFlags.LSL)]
        public static readonly Quaternion ZERO_ROTATION = Quaternion.Identity;
        #endregion
    }
}
