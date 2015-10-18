// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Main.Common
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class PluginName : Attribute
    {
        public PluginName(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }
}
