using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Main.Common;
using Nini.Config;

namespace ArribaSim.BackendConnectors.Robust.GroupsV2
{
    public class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            //Parameters
            // EnableHG
            // URI
            // HomeURI
            throw new NotImplementedException();
        }
    }
}
