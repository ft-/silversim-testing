// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Runtime.Serialization;

namespace SilverSim.Types.Asset.Format
{
    [Serializable]
    public class NotAMeshFormatException : Exception
    {
        public NotAMeshFormatException()
        {

        }

        public NotAMeshFormatException(string message)
            : base(message)
        {

        }

        protected NotAMeshFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            
        }

        public NotAMeshFormatException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    [Serializable]
    public class NotAMaterialFormatException : Exception
    {
        public NotAMaterialFormatException()
        {

        }

        public NotAMaterialFormatException(string message)
            : base(message)
        {

        }

        protected NotAMaterialFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public NotAMaterialFormatException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    [Serializable]
    public class NotANotecardFormatException : Exception
    {
        public NotANotecardFormatException()
        {

        }

        public NotANotecardFormatException(string message)
            : base(message)
        {

        }

        protected NotANotecardFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public NotANotecardFormatException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    [Serializable]
    public class NotALandmarkFormatException : Exception
    {
        public NotALandmarkFormatException()
        {

        }
 
        public NotALandmarkFormatException(string message)
            : base(message)
        {

        }

        protected NotALandmarkFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public NotALandmarkFormatException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
   }

    [Serializable]
    public class NotAWearableFormatException : Exception
    {
        public NotAWearableFormatException()
        {

        }

        public NotAWearableFormatException(string message)
            : base(message)
        {

        }

        protected NotAWearableFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public NotAWearableFormatException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    [Serializable]
    public class NotAGestureFormatException : Exception
    {
        public NotAGestureFormatException()
        {

        }

        public NotAGestureFormatException(string message)
            : base(message)
        {

        }

        protected NotAGestureFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public NotAGestureFormatException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

}
