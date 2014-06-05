using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArribaSim.Main.Common
{
    public interface IPlugin
    {
        void Startup(ConfigurationLoader loader);
    }
}
