// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.ServerParam
{
    public enum ServerParamType
    {
        GlobalOnly,
        GlobalAndRegion
    }

    public interface IServerParamListener
    {
    }

    public interface IServerParamAnyListener : IServerParamListener
    {
        void TriggerParameterUpdated(UUID regionID, string parametername, string value);
        IDictionary<string, ServerParamType> ServerParams { get; }
    }
}
