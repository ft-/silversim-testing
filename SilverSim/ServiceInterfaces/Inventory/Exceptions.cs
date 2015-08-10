// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;

namespace SilverSim.ServiceInterfaces.Inventory
{
    public class InventoryItemNotFound : Exception
    {
        public UUID ID { get; private set; }

        public InventoryItemNotFound(UUID key)
            : base(string.Format("InventoryItem {0} not found", key))
        {
            ID = key;
        }
    }

    public class InventoryItemNotStored : Exception
    {
        public UUID ID { get; private set; }

        public InventoryItemNotStored(UUID key)
            : base(string.Format("InventoryItem {0} not stored", key))
        {
            ID = key;
        }
    }

    public class InventoryFolderNotFound : Exception
    {
        public UUID ID { get; private set; }

        public InventoryFolderNotFound(UUID key)
            : base(string.Format("InventoryFolder {0} not found", key))
        {
            ID = key;
        }
    }

    public class InventoryFolderTypeNotFound : Exception
    {
        public AssetType Type { get; private set; }

        public InventoryFolderTypeNotFound(AssetType type)
            : base(string.Format("InventoryFolder for type {0} not found", type))
        {
            Type = type;
        }
    }

    public class InventoryFolderNotStored : Exception
    {
        public UUID ID { get; private set; }

        public InventoryFolderNotStored(UUID key)
            : base(string.Format("InventoryFolder {0} not stored", key))
        {
            ID = key;
        }
    }

    public class InventoryInaccessible : Exception
    {
        public InventoryInaccessible()
        {

        }
    }
}
