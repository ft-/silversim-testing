// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Asset
{
    public abstract class AssetServiceInterface
    {
        #region Exists methods
        public abstract bool exists(UUID key);
        public abstract Dictionary<UUID, bool> exists(List<UUID> assets);
        #endregion

        #region Accessors
        public abstract AssetData this[UUID key]
        {
            get;
        }

        #endregion

        #region Metadata interface
        public abstract AssetMetadataServiceInterface Metadata
        {
            get;
        }
        #endregion

        #region References interface
        public abstract AssetReferencesServiceInterface References
        {
            get;
        }
        #endregion

        #region Data interface
        public abstract AssetDataServiceInterface Data
        {
            get;
        }
        #endregion

        #region Store asset method
        public abstract void Store(AssetData asset);
        #endregion

        #region Delete asset method
        public abstract void Delete(UUID id);
        #endregion

        #region Constructors
        public AssetServiceInterface()
        {

        }
        #endregion
    }
}
