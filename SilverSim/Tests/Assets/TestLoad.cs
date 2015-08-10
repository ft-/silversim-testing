// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Tests.Extensions;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SilverSim.Tests.Assets
{
    public class TestLoad : ITest
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public List<UUID> AssetIDs = new List<UUID>();

        AssetServiceInterface m_AssetService;

        public void Startup(ConfigurationLoader loader)
        {
            IConfig config = loader.Config.Configs[GetType().FullName];
            m_AssetService = loader.GetService<AssetServiceInterface>(config.GetString("AssetService"));
            foreach(string key in config.GetKeys())
            {
                if(key.StartsWith("AssetID"))
                {
                    AssetIDs.Add(config.GetString(key));
                }
            }
        }

        public bool Run()
        {
            bool success = true;
            foreach(UUID assetID in AssetIDs)
            {
                m_Log.InfoFormat("Testing load of AssetID {0}", assetID);
                AssetData test;
                try
                {
                    test = m_AssetService[assetID];
                }
                catch
                {
                    success = false;
                }
            }

            return success;
        }
    }
}
