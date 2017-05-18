// SilverSim is distributed under the terms of the
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

        public virtual bool IsSameServer(AssetServiceInterface other) => false;
    }
}
