using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nini.Config;
using Mono.Addins;

namespace ArribaSim.Main.Common
{
    /* Factory class for object creation */
    public abstract class PluginFactory : ExtensionNode
    {
        public abstract void Initialize(ConfigurationLoader loader, IConfig ownSection);

        public PluginFactory()
        {

        }
    }
}
