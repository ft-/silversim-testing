// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace SilverSim.Types.Assembly
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    [ComVisible(true)]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public sealed class InterfaceVersionAttribute : Attribute
    {
        public InterfaceVersionAttribute(string version)
        {
            Version = version;
        }

        public string Version { get; private set; }
    }
}
