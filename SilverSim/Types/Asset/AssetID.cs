// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Types.Asset
{
    public struct AssetID
    {
        public UUID ID;
        public URI URI;

        public static explicit operator UUID(AssetID v)
        {
            return v.ID;
        }
    }
}
