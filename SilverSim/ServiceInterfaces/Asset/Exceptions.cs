// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.ServiceInterfaces.Asset
{
    public class HGAccessNotSupported : Exception {}

    public class AssetNotFound : Exception
    {
        public UUID ID { get; private set; }

        public AssetNotFound(UUID key)
            : base(string.Format("Asset {0} not found", key))
        {
            ID = key;
        }
    }

    public class AssetNotDeleted : Exception
    {
        public UUID ID { get; private set; }

        public AssetNotDeleted(UUID key)
            : base(string.Format("Asset {0} not deleted", key))
        {
            ID = key;
        }
    }

    public class AssetStoreFailed : Exception
    {
        public UUID ID { get; private set; }

        public AssetStoreFailed(UUID key)
            : base(string.Format("Asset {0} not stored", key))
        {
            ID = key;
        }
    }

}
