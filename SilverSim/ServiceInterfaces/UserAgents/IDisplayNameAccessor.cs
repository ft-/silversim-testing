// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.ServiceInterfaces.UserAgents
{
    public interface IDisplayNameAccessor
    {
        string this[UUI agent] { get;  set; }
        bool TryGetValue(UUI agent, out string displayname);
        bool ContainsKey(UUI agent);
    }
}
