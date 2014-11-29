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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;

namespace SilverSim.Scripting.LSL.API.Parcel
{
    public partial class Parcel_API
    {
        [APILevel(APIFlags.LSL)]
        public AnArray llGetParcelDetails(Vector3 pos, AnArray param)
        {
#warning Implement llGetParcelDetails(Vector3, AnArray)
            return new AnArray();
        }

        [APILevel(APIFlags.LSL)]
        public int llGetParcelFlags(Vector3 pos)
        {
#warning Implement llGetParcelFlags(Vector3)
            return 0;
        }

        [APILevel(APIFlags.LSL)]
        public int llGetParcelMaxPrims(Vector3 pos, int sim_wide)
        {
#warning Implement llGetParcelMaxPrims(Vector3, int)
            return 0;
        }

        [APILevel(APIFlags.LSL)]
        public string llGetParcelMusicURL()
        {
#warning Implement llGetParcelMusicURL()
            return string.Empty;
        }

        [APILevel(APIFlags.LSL)]
        public void llSetParcelMusicURL(string url)
        {
#warning Implement llSetParcelMusicURL(string)
        }

        [APILevel(APIFlags.LSL)]
        public int llReturnObjectsByID(AnArray objects)
        {
#warning Implement llReturnObjectsByID(AnArray)
            return 0;
        }

        [APILevel(APIFlags.LSL)]
        public int llReturnObjectsByOwner(UUID owner, int scope)
        {
#warning Implement llReturnObjectsByOwner(UUID, int)
            return 0;
        }

        [APILevel(APIFlags.LSL)]
        public UUID llGetLandOwnerAt(Vector3 pos)
        {
#warning Implement llGetLandOwnerAt(Vector3)
            return UUID.Zero;
        }

        [APILevel(APIFlags.LSL)]
        public int llGetParcelPrimCount(Vector3 pos, int category, int sim_wide)
        {
#warning Implement llGetParcelPrimCount(Vector3, int, int)
            return 0;
        }

        [APILevel(APIFlags.LSL)]
        public AnArray llGetParcelPrimOwners(Vector3 pos)
        {
#warning Implement llGetParcelPrimOwners(Vector3)
            return new AnArray();
        }
    }
}
