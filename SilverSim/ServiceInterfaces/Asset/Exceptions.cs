// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Asset
{
    [Serializable]
    public class HGAccessNotSupportedException : Exception
    {
        public HGAccessNotSupportedException()
        {

        }
    }

    [Serializable]
    public class AssetNotFoundException : KeyNotFoundException
    {
        public UUID ID { get; private set; }

        public AssetNotFoundException(UUID key)
            : base(string.Format("Asset {0} not found", key))
        {
            ID = key;
        }
    }

    [Serializable]
    public class AssetNotDeletedException : Exception
    {
        public UUID ID { get; private set; }

        public AssetNotDeletedException(UUID key)
            : base(string.Format("Asset {0} not deleted", key))
        {
            ID = key;
        }
    }

    [Serializable]
    public class AssetStoreFailedException : Exception
    {
        public UUID ID { get; private set; }

        public AssetStoreFailedException(UUID key)
            : base(string.Format("Asset {0} not stored", key))
        {
            ID = key;
        }
    }

}
