// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Types.Maptile
{
    public class MaptileData : MaptileInfo
    {
        public byte[] Data = new byte[0];
        public string ContentType = string.Empty;

        public MaptileData()
        {

        }

        public MaptileData(MaptileData data)
            : base(data)
        {
            Data = new byte[data.Data.Length];
            Buffer.BlockCopy(data.Data, 0, Data, 0, Data.Length);
            ContentType = data.ContentType;
        }
    }
}
