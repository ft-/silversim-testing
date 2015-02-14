/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using HttpClasses;
using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.BackendConnectors.IWC.Common;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.StructuredData.AssetXml;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace SilverSim.BackendConnectors.IWC.Asset
{
    #region Service Implementation
    public class IWCAssetConnector : AssetServiceInterface, IPlugin
    {
        public class IWCAssetProtocolError : Exception
        {
            public IWCAssetProtocolError(string msg) : base(msg) {}
        }

        private int m_TimeoutMs = 20000;
        public int TimeoutMs
        {
            get
            {
                return m_TimeoutMs;
            }
            set
            {
                m_MetadataService.TimeoutMs = value;
                m_TimeoutMs = value;
            }
        }
        private string m_AssetURI;
        private IWCAssetMetadataConnector m_MetadataService;
        private DefaultAssetReferencesService m_ReferencesService;
        private IWCAssetDataConnector m_DataService;
        private bool m_EnableCompression = false;
        private bool m_EnableLocalStorage = false;
        private bool m_EnableTempStorage = false;

        #region Constructor
        public IWCAssetConnector(string uri, bool enableCompression = false, bool enableLocalStorage = false, bool enableTempStorage = false)
        {
            m_AssetURI = uri;
            m_DataService = new IWCAssetDataConnector(uri);
            m_MetadataService = new IWCAssetMetadataConnector(uri);
            m_ReferencesService = new DefaultAssetReferencesService(this);
            m_MetadataService.TimeoutMs = m_TimeoutMs;
            m_EnableCompression = enableCompression;
            m_EnableLocalStorage = enableLocalStorage;
            m_EnableTempStorage = enableTempStorage;
        }

        public void Startup(ConfigurationLoader loader)
        {

        }
        #endregion

        #region Exists methods
        public override void exists(UUID key)
        {
            Map param = new Map
                {
                    {"id", key.AsString}
                };

            Map m = IWCGrid.PostToService(m_AssetURI, "GetExists", param, TimeoutMs);
            if (m.ContainsKey("Value"))
            {
                if(m.AsBoolean)
                {
                    return;
                }
            }

            throw new AssetNotFound(key);
        }

        public override Dictionary<UUID, bool> exists(List<UUID> assets)
        {
            Dictionary<UUID, bool> res = new Dictionary<UUID,bool>();

            foreach(UUID assetid in assets)
            {
                try
                {
                    exists(assetid);
                    res[assetid] = true;
                }
                catch(AssetNotFound)
                {
                    res[assetid] = false;
                }
            }

            return res;
        }
        #endregion

        #region Accessors
        public override AssetData this[UUID key]
        {
            get
            {
                Map param = new Map
                {
                    {"id", key.AsString}
                };

                Map m = IWCGrid.PostToService(m_AssetURI, "GetMeta", param, TimeoutMs);
                if (m.ContainsKey("Value"))
                {
                    return ((Map)m["Value"]).IWCtoAssetData();
                }

                throw new AssetNotFound(key);
            }
        }
        #endregion

        #region Metadata interface
        public override AssetMetadataServiceInterface Metadata
        {
            get
            {
                return m_MetadataService;
            }
        }
        #endregion

        #region References interface
        public override AssetReferencesServiceInterface References
        {
            get
            {
                return m_ReferencesService;
            }
        }
        #endregion

        #region Data interface
        public override AssetDataServiceInterface Data
        {
            get
            {
                return m_DataService;
            }
        }
        #endregion

        #region Store asset method
        static Encoding UTF8NoBOM = new UTF8Encoding(false);

        public override void Store(AssetData asset)
        {
            if((asset.Temporary && !m_EnableTempStorage) ||
                (asset.Local && !m_EnableLocalStorage))
            {
                /* Do not store temporary or local assets on specified server unless explicitly wanted */
                return;
            }

            Map param = new Map
                {
                    {"asset", asset.AssetDataToIWC()}
                };

            Map m = IWCGrid.PostToService(m_AssetURI, "Store", param, TimeoutMs);
            if (m.ContainsKey("Value"))
            {
                asset.ID = m["Value"].AsUUID;
                return;
            }
            throw new AssetStoreFailed(asset.ID);
        }
        #endregion

        #region Delete asset method
        public override void Delete(UUID id)
        {
            Map param = new Map
                {
                    {"id", id}
                };

            Map m = IWCGrid.PostToService(m_AssetURI, "Delete", param, TimeoutMs);
            if (m.ContainsKey("Value"))
            {
                return;
            }
            throw new AssetNotFound(id);
        }
        #endregion
    }
    #endregion

    #region Factory
    [PluginName("Assets")]
    public class IWCAssetConnectorFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("IWC ASSET CONNECTOR");
        public IWCAssetConnectorFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            if (!ownSection.Contains("URI"))
            {
                m_Log.FatalFormat("Missing 'URI' in section {0}", ownSection.Name);
                throw new ConfigurationLoader.ConfigurationError();
            }
            return new IWCAssetConnector(
                ownSection.GetString("URI"),
                ownSection.GetBoolean("EnableCompressedStoreRequest", false),
                ownSection.GetBoolean("EnableLocalAssetStorage", false),
                ownSection.GetBoolean("EnableTempAssetStorage", false));
        }
    }
    #endregion
}
