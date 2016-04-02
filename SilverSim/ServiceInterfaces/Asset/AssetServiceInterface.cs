﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Asset
{
    public abstract class AssetServiceInterface
    {
        #region Exists methods
        public abstract bool Exists(UUID key);
        public abstract Dictionary<UUID, bool> Exists(List<UUID> assets);
        #endregion

        #region Accessors
        public abstract AssetData this[UUID key]
        {
            get;
        }

        public abstract bool TryGetValue(UUID key, out AssetData assetData);

        #endregion

        #region Metadata interface
        public abstract IAssetMetadataServiceInterface Metadata
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
        public abstract IAssetDataServiceInterface Data
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

        public virtual bool IsSameServer(AssetServiceInterface other)
        {
            return false;
        }

        #region Constructors
        public AssetServiceInterface()
        {

        }
        #endregion
    }
}
