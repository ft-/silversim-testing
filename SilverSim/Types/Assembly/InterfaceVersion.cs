// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Runtime.InteropServices;

namespace SilverSim.Types.Assembly
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    [ComVisible(true)]
    public sealed class InterfaceVersion : Attribute
    {
        public InterfaceVersion(string version)
        {
            Version = version;
        }

        public string Version { get; private set; }
    }
}
