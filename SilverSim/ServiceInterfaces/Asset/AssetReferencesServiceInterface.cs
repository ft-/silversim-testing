// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;

namespace SilverSim.ServiceInterfaces.Asset
{
    public abstract class AssetReferencesServiceInterface
    {
        #region References accessors
        public abstract List<UUID> this[UUID key]
        {
            get;
        }
        #endregion

        #region Constructor
        public AssetReferencesServiceInterface()
        {

        }
        #endregion
    }
}
