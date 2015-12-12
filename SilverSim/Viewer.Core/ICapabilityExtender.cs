// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpServer;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.Core
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    /* used on methods compatible with FactoryDelegate */
    public sealed class CapabilityHandlerAttribute : Attribute
    {
        public string Name { get; private set; }

        public CapabilityHandlerAttribute(string name)
        {
            Name = name;
        }
        // for documentation
        // public delegate void CapabilityDelegate(ViewerAgent agent, AgentCircuit circuit, HttpRequest req);
    }

    public interface ICapabilityExtender : IProtocolExtender
    {
    }
}
