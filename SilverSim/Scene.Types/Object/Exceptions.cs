// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Scene.Types.Object
{
    [Serializable]
    public class InvalidObjectXmlException : Exception
    {
        public InvalidObjectXmlException()
        {

        }
    }

    [Serializable]
    public class ObjectDeserializationFailedDueKey : Exception
    {
        public ObjectDeserializationFailedDueKey()
        {

        }
    }
}
