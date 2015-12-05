// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;

namespace SilverSim.ServiceInterfaces.Asset
{
    public abstract class AssetMetadataServiceInterface
    {
        #region Metadata accessors
        public abstract AssetMetadata this[UUID key]
        {
            get;
        }

        public abstract bool TryGetValue(UUID key, out AssetMetadata metadata);
        #endregion

        #region Constructor
        public AssetMetadataServiceInterface()
        {

        }
        #endregion
    }
}
