// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ThreadedClasses;

namespace SilverSim.ServiceInterfaces.Asset
{
    public sealed class DefaultAssetReferencesService : AssetReferencesServiceInterface
    {
        #region Fields
        readonly AssetServiceInterface m_Service;
        readonly RwLockedDictionary<UUID, List<UUID>> m_ReferencesCache = new RwLockedDictionary<UUID, List<UUID>>();
        #endregion

        #region Constructor
        public DefaultAssetReferencesService(AssetServiceInterface service)
        {
            m_Service = service;
        }

        #endregion

        #region Accessor
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public override List<UUID> this[UUID key]
        {
            get
            {
                List<UUID> result;
                if(m_ReferencesCache.TryGetValue(key, out result))
                {
                    /* this action is cheaper than re-requesting the asset over and over again */
                    return new List<UUID>(result);
                }

                AssetData data = m_Service[key];
                try
                {
                    result = data.References;
                    m_ReferencesCache[key] = result;
                    return new List<UUID>(result);
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
