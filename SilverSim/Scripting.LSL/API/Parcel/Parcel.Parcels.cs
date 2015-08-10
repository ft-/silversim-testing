// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public int llGetParcelFlags(ScriptInstance Instance, Vector3 pos)
        {
#warning Implement llGetParcelFlags(Vector3)
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public int llGetParcelMaxPrims(ScriptInstance Instance, Vector3 pos, int sim_wide)
        {
#warning Implement llGetParcelMaxPrims(Vector3, int)
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public string llGetParcelMusicURL(ScriptInstance Instance)
        {
#warning Implement llGetParcelMusicURL()
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(2)]
        public void llSetParcelMusicURL(ScriptInstance Instance, string url)
        {
#warning Implement llSetParcelMusicURL(string)
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public int llReturnObjectsByID(ScriptInstance Instance, AnArray objects)
        {
#warning Implement llReturnObjectsByID(AnArray)
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public int llReturnObjectsByOwner(ScriptInstance Instance, LSLKey owner, int scope)
        {
#warning Implement llReturnObjectsByOwner(UUID, int)
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public LSLKey llGetLandOwnerAt(ScriptInstance Instance, Vector3 pos)
        {
#warning Implement llGetLandOwnerAt(Vector3)
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public int llGetParcelPrimCount(ScriptInstance Instance, Vector3 pos, int category, int sim_wide)
        {
#warning Implement llGetParcelPrimCount(Vector3, int, int)
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(2)]
        public AnArray llGetParcelPrimOwners(ScriptInstance Instance, Vector3 pos)
        {
#warning Implement llGetParcelPrimOwners(Vector3)
            throw new NotImplementedException();
        }
    }
}
