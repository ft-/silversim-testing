// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Runtime.InteropServices;

namespace SilverSim.Main.Common
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [ComVisible(true)]
    public class PluginName : Attribute
    {
        public PluginName(string pluginName)
        {
            Name = pluginName;
        }

        public string Name { get; private set; }
    }
}
