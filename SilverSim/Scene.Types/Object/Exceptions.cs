// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Runtime.Serialization;

namespace SilverSim.Scene.Types.Object
{
    [Serializable]
    public class InvalidObjectXmlException : Exception
    {
        public InvalidObjectXmlException()
        {

        }

        public InvalidObjectXmlException(string message)
            : base(message)
        {

        }

        protected InvalidObjectXmlException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public InvalidObjectXmlException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    [Serializable]
    public class ObjectDeserializationFailedDueKeyException : Exception
    {
        public ObjectDeserializationFailedDueKeyException()
        {

        }

        public ObjectDeserializationFailedDueKeyException(string message)
            : base(message)
        {

        }

        protected ObjectDeserializationFailedDueKeyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public ObjectDeserializationFailedDueKeyException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
