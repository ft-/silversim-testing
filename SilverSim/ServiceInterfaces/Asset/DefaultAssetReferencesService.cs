// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;
using ThreadedClasses;

namespace SilverSim.ServiceInterfaces.Asset
{
    public class DefaultAssetReferencesService : AssetReferencesServiceInterface, IDisposable
    {
        #region Fields
        private AssetServiceInterface m_Service;
        private readonly RwLockedDictionary<UUID, List<UUID>> m_ReferencesCache = new RwLockedDictionary<UUID, List<UUID>>();
        #endregion

        #region Constructor
        public DefaultAssetReferencesService(AssetServiceInterface service)
        {
            m_Service = service;
        }

        public void Dispose()
        {
            m_Service = null;
        }
        #endregion

        #region Accessor
        public override List<UUID> this[UUID asset]
        {
            get
            {
                List<UUID> result;
                if(m_ReferencesCache.TryGetValue(asset, out result))
                {
                    /* this action is cheaper than re-requesting the asset over and over again */
                    return new List<UUID>(result);
                }

                AssetData data = m_Service[asset];
                try
                {
                    return new List<UUID>(m_ReferencesCache[asset] = data.References);
                }
                catch
                {
                    return new List<UUID>();
                }
            }
        }
        #endregion
    }
}
