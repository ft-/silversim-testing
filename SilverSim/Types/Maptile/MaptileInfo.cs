// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Types.Maptile
{
    public class MaptileInfo
    {
        public GridVector Location = GridVector.Zero;
        public Date LastUpdate = Date.Now;
        public int ZoomLevel = 1;
        public UUID ScopeID = UUID.Zero;

        public MaptileInfo()
        {

        }

        public MaptileInfo(MaptileInfo info)
        {
            Location = info.Location;
            LastUpdate = new Date(info.LastUpdate);
            ZoomLevel = info.ZoomLevel;
            ScopeID = info.ScopeID;
        }
    }
}
