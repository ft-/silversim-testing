// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using System.IO;

namespace SilverSim.ServiceInterfaces.Asset
{
    public interface IAssetDataServiceInterface
    {
        #region Data accessors
        Stream this[UUID key]
        {
            get;
        }

        bool TryGetValue(UUID key, out Stream s);
        #endregion
    }
}
