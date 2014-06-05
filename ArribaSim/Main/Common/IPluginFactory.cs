using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nini.Config;

namespace ArribaSim.Main.Common
{
    /* Factory class for object creation */
    public interface IPluginFactory
    {
        IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection);
    }
}
