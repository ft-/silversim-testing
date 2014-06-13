/*

ArribaSim is distributed under the terms of the
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

using ArribaSim.Main.Common;
using ArribaSim.ServiceInterfaces.Asset;
using ArribaSim.Types;
using ArribaSim.Types.Asset;
using log4net;
using Nini.Config;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ArribaSim.Stacked.Asset
{
    #region Service Implementation
    public class MetadataHandler : AssetMetadataServiceInterface
    {
        private List<AssetServiceInterface> m_Services;

        public MetadataHandler(List<AssetServiceInterface> services)
        {
            m_Services = services;
        }

        public override AssetMetadata this[UUID key]
        {
            get
            {
                foreach(AssetServiceInterface service in m_Services)
                {
                    try
                    {
                        return service.Metadata[key];
                    }
                    catch
                    {
                    }
                }
                throw new AssetNotFound(key);
            }
        }
    }

    public class Handler : AssetServiceInterface, IPlugin
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private List<AssetServiceInterface> m_ServiceList = new List<AssetServiceInterface>();
        private List<string> m_ServiceNameList;
        private string m_ServiceName;
        private MetadataHandler m_MetadataService;

        #region Constructor
        public Handler(string serviceName, List<string> serviceNameList)
        {
            m_ServiceNameList = serviceNameList;
            m_ServiceName = serviceName;
        }

        public void Startup(ConfigurationLoader loader)
        {
            foreach(string serviceName in m_ServiceNameList)
            {
                try
                {
                    m_ServiceList.Add(loader.GetService<AssetServiceInterface>(serviceName));
                }
                catch
                {
                    m_Log.ErrorFormat("[{0}]: Invalid service module in section {1}", m_ServiceName, serviceName);
                    throw new ConfigurationLoader.ConfigurationError();
                }
            }
            m_MetadataService = new MetadataHandler(m_ServiceList);
        }
        #endregion

        #region Exists methods
        public override void exists(UUID key)
        {
            foreach(AssetServiceInterface service in m_ServiceList)
            {
                try
                {
                    service.exists(key);
                    break;
                }
                catch
                {
                    continue;
                }
            }
            throw new AssetNotFound(key);
        }

        public override Dictionary<UUID, bool> exists(List<UUID> assets)
        {
            Dictionary<UUID, bool> result = new Dictionary<UUID,bool>();
            foreach(AssetServiceInterface service in m_ServiceList)
            {
                foreach(KeyValuePair<UUID, bool> kvp in service.exists(assets))
                {
                    if(!result.ContainsKey(kvp.Key))
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                    else if(!result[kvp.Key])
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }

                /* abort if all assets have been found */
                if (!result.ContainsValue(false))
                {
                    break;
                }
            }
            return result;
        }
        #endregion

        #region Accessors
        public override AssetData this[UUID key]
        {
            get
            {
                List<AssetServiceInterface> toStoreAt = new List<AssetServiceInterface>();
                foreach(AssetServiceInterface service in m_ServiceList)
                {
                    try
                    {
                        AssetData asset = service[key];
                        foreach(AssetServiceInterface storeAt in toStoreAt)
                        {
                            try
                            {
                                storeAt.Store(asset);
                            }
                            catch
                            {

                            }
                        }
                        return asset;
                    }
                    catch
                    {

                    }
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

        #region Store asset method
        public override void Store(AssetData asset)
        {
            foreach(AssetServiceInterface service in m_ServiceList)
            {
                service.Store(asset);
            }
        }
        #endregion

        #region Delete asset method
        public override void Delete(UUID id)
        {
            bool assetDeleted = false;
            foreach (AssetServiceInterface service in m_ServiceList)
            {
                try
                {
                    service.Delete(id);
                    assetDeleted = true;
                }
                catch
                {

                }
            }
            if(!assetDeleted)
            {
                throw new AssetNotFound(id);
            }
        }
        #endregion
    }
    #endregion

    #region Factory Implementation
    public class HandlerFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public HandlerFactory()
        {
        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            if (!ownSection.Contains("ServiceList"))
            {
                m_Log.FatalFormat("Missing 'ServiceList' in section {0}", ownSection.Name);
                throw new ConfigurationLoader.ConfigurationError();
            }
            string serviceList = ownSection.GetString("ServiceList");
            string[] services = serviceList.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if(services.Length == 0)
            {
                m_Log.FatalFormat("Empty 'ServiceList' in section {0}", ownSection.Name);
                throw new ConfigurationLoader.ConfigurationError();
            }
            return new Handler(ownSection.Name, new List<string>(services));
        }
    }
    #endregion
}
