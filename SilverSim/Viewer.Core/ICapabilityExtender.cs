// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpServer;
using System;

namespace SilverSim.Viewer.Core
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    /* used on methods compatible with FactoryDelegate */
    public class CapabilityHandler : Attribute
    {
        public readonly string Name;

        public CapabilityHandler(string name)
        {
            Name = name;
        }
        public delegate void CapabilityDelegate(ViewerAgent agent, AgentCircuit circuit, HttpRequest req);
    }

    public interface ICapabilityExtender : IProtocolExtender
    {
    }
}
