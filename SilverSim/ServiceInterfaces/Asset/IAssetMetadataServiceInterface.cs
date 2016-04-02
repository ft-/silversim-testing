// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;

namespace SilverSim.ServiceInterfaces.Asset
{
    public interface IAssetMetadataServiceInterface
    {
        #region Metadata accessors
        AssetMetadata this[UUID key]
        {
            get;
        }

        bool TryGetValue(UUID key, out AssetMetadata metadata);
        #endregion
    }
}
