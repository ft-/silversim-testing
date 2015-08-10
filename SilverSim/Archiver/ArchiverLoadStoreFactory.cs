// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Archiver
{
    [PluginName("OpenSimArchiveSupport")]
    public class ArchiverLoadStoreFactory : IPluginFactory
    {
        public ArchiverLoadStoreFactory()
        {

        }
        public IPlugin Initialize(ConfigurationLoader loader, Nini.Config.IConfig ownSection)
        {
            return new ArchiverLoadStore();
        }
    }
}
