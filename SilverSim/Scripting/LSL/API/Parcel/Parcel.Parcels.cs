﻿/*

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
using SilverSim.Scene.Types.Script;

namespace SilverSim.Scripting.LSL.API.Parcel
{
    public partial class Parcel_API
    {
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_COUNT_TOTAL = 0;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_COUNT_OWNER = 1;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_COUNT_GROUP = 2;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_COUNT_OTHER = 3;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_COUNT_SELECTED = 4;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_COUNT_TEMP = 5;

        [APILevel(APIFlags.LSL)]
        public const int PARCEL_DETAILS_NAME = 0;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_DETAILS_DESC = 1;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_DETAILS_OWNER = 2;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_DETAILS_GROUP = 3;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_DETAILS_AREA = 4;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_DETAILS_ID = 5;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_DETAILS_SEE_AVATARS = 6; // not implemented

        //osSetParcelDetails
        public const int PARCEL_DETAILS_CLAIMDATE = 10;

        [APILevel(APIFlags.LSL)]
        public AnArray llGetParcelDetails(ScriptInstance Instance, Vector3 pos, AnArray param)
        {
#warning Implement llGetParcelDetails(Vector3, AnArray)
            return new AnArray();
        }

        [APILevel(APIFlags.LSL)]
        public int llGetParcelFlags(ScriptInstance Instance, Vector3 pos)
        {
#warning Implement llGetParcelFlags(Vector3)
            return 0;
        }

        [APILevel(APIFlags.LSL)]
        public int llGetParcelMaxPrims(ScriptInstance Instance, Vector3 pos, int sim_wide)
        {
#warning Implement llGetParcelMaxPrims(Vector3, int)
            return 0;
        }

        [APILevel(APIFlags.LSL)]
        public string llGetParcelMusicURL(ScriptInstance Instance)
        {
#warning Implement llGetParcelMusicURL()
            return string.Empty;
        }

        [APILevel(APIFlags.LSL)]
        public void llSetParcelMusicURL(ScriptInstance Instance, string url)
        {
#warning Implement llSetParcelMusicURL(string)
        }

        [APILevel(APIFlags.LSL)]
        public int llReturnObjectsByID(ScriptInstance Instance, AnArray objects)
        {
#warning Implement llReturnObjectsByID(AnArray)
            return 0;
        }

        [APILevel(APIFlags.LSL)]
        public int llReturnObjectsByOwner(ScriptInstance Instance, UUID owner, int scope)
        {
#warning Implement llReturnObjectsByOwner(UUID, int)
            return 0;
        }

        [APILevel(APIFlags.LSL)]
        public UUID llGetLandOwnerAt(ScriptInstance Instance, Vector3 pos)
        {
#warning Implement llGetLandOwnerAt(Vector3)
            return UUID.Zero;
        }

        [APILevel(APIFlags.LSL)]
        public int llGetParcelPrimCount(ScriptInstance Instance, Vector3 pos, int category, int sim_wide)
        {
#warning Implement llGetParcelPrimCount(Vector3, int, int)
            return 0;
        }

        [APILevel(APIFlags.LSL)]
        public AnArray llGetParcelPrimOwners(ScriptInstance Instance, Vector3 pos)
        {
#warning Implement llGetParcelPrimOwners(Vector3)
            return new AnArray();
        }
    }
}
