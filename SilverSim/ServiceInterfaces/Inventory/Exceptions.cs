// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

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
        public UUID ID { get; }

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
    public class InventoryItemNotCopiableException : Exception
    {
        public UUID ID { get; }

        public InventoryItemNotCopiableException(UUID key)
            : base(string.Format("InventoryItem {0} not copiable", key))
        {
            ID = key;
        }

        public InventoryItemNotCopiableException()
        {
        }

        public InventoryItemNotCopiableException(string message)
            : base(message)
        {
        }

        protected InventoryItemNotCopiableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public InventoryItemNotCopiableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Serializable]
    public class InventoryItemNotStoredException : Exception
    {
        public UUID ID { get; }

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
        public UUID ID { get; }

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
        public AssetType Type { get; }

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
        public UUID ID { get; }

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

    [Serializable]
    public class InvalidParentFolderIdException : Exception
    {
        public InvalidParentFolderIdException()
        {
        }

        public InvalidParentFolderIdException(string message)
            : base(message)
        {
        }

        protected InvalidParentFolderIdException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public InvalidParentFolderIdException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
