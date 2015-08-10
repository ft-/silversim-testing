// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.BackendConnectors.IWC.Common;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using System.IO;

namespace SilverSim.BackendConnectors.IWC.Asset
{
    public class IWCAssetDataConnector : AssetDataServiceInterface
    {
        public int TimeoutMs = 20000;
        private string m_AssetURI;

        #region Constructor
        public IWCAssetDataConnector(string uri)
        {
            m_AssetURI = uri;
        }
        #endregion

        #region Metadata accessors
        public override Stream this[UUID key]
        {
            get
            {
                try
                {
                    Map param = new Map
                    {
                        {"id", key.AsString}
                    };

                    Map m = IWCGrid.PostToService(m_AssetURI, "GetData", param, TimeoutMs);
                    if (m.ContainsKey("Value"))
                    {
                        return new MemoryStream(m["Value"] as BinaryData);
                    }
                }
                catch
                {
                }
                throw new AssetNotFound(key);
            }
        }
        #endregion
    }
}
