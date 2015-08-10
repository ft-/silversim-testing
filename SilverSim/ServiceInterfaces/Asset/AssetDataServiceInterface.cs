// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using System.IO;

namespace SilverSim.ServiceInterfaces.Asset
{
    public abstract class AssetDataServiceInterface
    {
        #region Data accessors
        public abstract Stream this[UUID key]
        {
            get;
        }
        #endregion

        #region Constructor
        public AssetDataServiceInterface()
        {

        }
        #endregion
    }
}
