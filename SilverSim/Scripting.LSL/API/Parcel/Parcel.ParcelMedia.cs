// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.LSL.API.Parcel
{
    public partial class Parcel_API
    {
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_STOP = 0;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_PAUSE = 1;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_PLAY = 2;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_LOOP = 3;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_TEXTURE = 4;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_URL = 5;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_TIME = 6;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_AGENT = 7;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_UNLOAD = 8;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_AUTO_ALIGN = 9;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_TYPE = 10;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_SIZE = 11;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_DESC = 12;
        [APILevel(APIFlags.LSL)]
        public const int PARCEL_MEDIA_COMMAND_LOOP_SET = 13;

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(2)]
        public void llParcelMediaCommandList(ScriptInstance Instance, AnArray commandList)
        {
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(2)]
        public AnArray llParcelMediaQuery(ScriptInstance Instance, AnArray query)
        {
            throw new NotImplementedException();
        }
    }
}
