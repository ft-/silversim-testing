// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Inventory
{
    [Serializable]
    public class InventoryItemNotFoundException : KeyNotFoundException
    {
        public UUID ID { get; private set; }

        public InventoryItemNotFoundException(UUID key)
            : base(string.Format("InventoryItem {0} not found", key))
        {
            ID = key;
        }
    }

    [Serializable]
    public class InventoryItemNotStoredException : Exception
    {
        public UUID ID { get; private set; }

        public InventoryItemNotStoredException(UUID key)
            : base(string.Format("InventoryItem {0} not stored", key))
        {
            ID = key;
        }
    }

    [Serializable]
    public class InventoryFolderNotFoundException : KeyNotFoundException
    {
        public UUID ID { get; private set; }

        public InventoryFolderNotFoundException(UUID key)
            : base(string.Format("InventoryFolder {0} not found", key))
        {
            ID = key;
        }
    }

    [Serializable]
    public class InventoryFolderTypeNotFoundException : KeyNotFoundException
    {
        public AssetType Type { get; private set; }

        public InventoryFolderTypeNotFoundException(AssetType type)
            : base(string.Format("InventoryFolder for type {0} not found", type))
        {
            Type = type;
        }
    }

    [Serializable]
    public class InventoryFolderNotStoredException : Exception
    {
        public UUID ID { get; private set; }

        public InventoryFolderNotStoredException(UUID key)
            : base(string.Format("InventoryFolder {0} not stored", key))
        {
            ID = key;
        }
    }

    [Serializable]
    public class InventoryInaccessibleException : Exception
    {
        public InventoryInaccessibleException()
        {

        }
    }
}
