// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SilverSim.ServiceInterfaces.Asset
{
    [Serializable]
    public class HGAccessNotSupportedException : Exception
    {
        public HGAccessNotSupportedException()
        {

        }

        public HGAccessNotSupportedException(string message)
            : base(message)
        {

        }

        protected HGAccessNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public HGAccessNotSupportedException(string message, Exception innerException)
            : base(message, innerException)
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

        public AssetNotFoundException()
        {

        }

        public AssetNotFoundException(string message)
            : base(message)
        {

        }

        protected AssetNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public AssetNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {

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

        public AssetNotDeletedException()
        {

        }

        public AssetNotDeletedException(string message)
            : base(message)
        {

        }

        protected AssetNotDeletedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public AssetNotDeletedException(string message, Exception innerException)
            : base(message, innerException)
        {

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

        public AssetStoreFailedException()
        {

        }

        public AssetStoreFailedException(string message)
            : base(message)
        {

        }

        protected AssetStoreFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public AssetStoreFailedException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

}
