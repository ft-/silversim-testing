// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

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

        public InventoryItemNotFoundException()
        {

        }

        public InventoryItemNotFoundException(string message)
            : base(message)
        {

        }

        protected InventoryItemNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public InventoryItemNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {

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

        public InventoryItemNotStoredException()
        {

        }

        public InventoryItemNotStoredException(string message)
            : base(message)
        {

        }

        protected InventoryItemNotStoredException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public InventoryItemNotStoredException(string message, Exception innerException)
            : base(message, innerException)
        {

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

        public InventoryFolderNotFoundException()
        {

        }

        public InventoryFolderNotFoundException(string message)
            : base(message)
        {

        }

        protected InventoryFolderNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public InventoryFolderNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {

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

        public InventoryFolderTypeNotFoundException()
        {

        }

        public InventoryFolderTypeNotFoundException(string message)
            : base(message)
        {

        }

        protected InventoryFolderTypeNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public InventoryFolderTypeNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {

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

        public InventoryFolderNotStoredException()
        {

        }

        public InventoryFolderNotStoredException(string message)
            : base(message)
        {

        }

        protected InventoryFolderNotStoredException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public InventoryFolderNotStoredException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    [Serializable]
    public class InventoryInaccessibleException : Exception
    {
        public InventoryInaccessibleException()
        {

        }

        public InventoryInaccessibleException(string message)
            : base(message)
        {

        }

        protected InventoryInaccessibleException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public InventoryInaccessibleException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
