// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Types.Parcel
{
    public class ParcelAccessEntry
    {
        public UUID RegionID;
        public UUID ParcelID;
        public UUI Accessor;
        public Date ExpiresAt;

        public ParcelAccessEntry()
        {

        }

        public ParcelAccessEntry(ParcelAccessEntry src)
        {
            RegionID = src.RegionID;
            ParcelID = src.ParcelID;
            Accessor = new UUI(src.Accessor);
            ExpiresAt = new Date(src.ExpiresAt);
        }
    }
}
