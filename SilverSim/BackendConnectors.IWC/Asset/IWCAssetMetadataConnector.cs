// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.BackendConnectors.IWC.Common;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;

namespace SilverSim.BackendConnectors.IWC.Asset
{
    public class IWCAssetMetadataConnector : AssetMetadataServiceInterface
    {
        public int TimeoutMs = 20000;
        private string m_AssetURI;

        #region Constructor
        public IWCAssetMetadataConnector(string uri)
        {
            m_AssetURI = uri;
        }
        #endregion

        #region Metadata accessors
        public override AssetMetadata this[UUID key]
        {
            get
            {
                Map param = new Map
                {
                    {"id", key.AsString}
                };

                Map m = IWCGrid.PostToService(m_AssetURI, "GetMeta", param, TimeoutMs);
                if(m.ContainsKey("Value"))
                {
                    return ((Map)m).IWCtoAssetMetadata();
                }

                throw new AssetNotFound(key);
            }
        }
        #endregion
    }
}
