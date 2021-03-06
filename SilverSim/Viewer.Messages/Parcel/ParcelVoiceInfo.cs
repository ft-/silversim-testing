﻿// SilverSim is distributed under the terms of the
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
using MapType = SilverSim.Types.Map;

namespace SilverSim.Viewer.Messages.Parcel
{
    [EventQueueGet("ParcelVoiceInfo")]
    [Trusted]
    public class ParcelVoiceInfo : Message
    {
        public string RegionName = string.Empty;
        public int ParcelLocalId;

        public string ChannelUri = string.Empty;
        public string ChannelCredentials = string.Empty;

        public override IValue SerializeEQG() => new MapType
        {
            { "region_name", RegionName},
            { "parcel_local_id", ParcelLocalId },
            { "voice_credentials", new MapType
                {
                    { "channel_uri", ChannelUri },
                    { "channel_credentials", ChannelCredentials }
                }
            }
        };

        public static Message DeserializeEQG(IValue value)
        {
            var m = (MapType)value;
            var c = (MapType)m["voice_credentials"];
            return new ParcelVoiceInfo
            {
                RegionName = m["region_name"].ToString(),
                ParcelLocalId = m["parcel_local_id"].AsInt,
                ChannelUri = c["channel_uri"].ToString(),
                ChannelCredentials = c["channel_credentials"].ToString()
            };
        }
    }
}
