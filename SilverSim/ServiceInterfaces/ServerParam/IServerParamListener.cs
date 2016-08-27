// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.ServiceInterfaces.ServerParam
{
    public interface IServerParamListener
    {
    }

    public interface IServerParamAnyListener : IServerParamListener
    {
        void TriggerParameterUpdated(UUID regionID, string parametername, string value);
    }
}
