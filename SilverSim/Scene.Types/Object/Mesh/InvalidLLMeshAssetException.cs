// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Runtime.Serialization;

namespace SilverSim.Scene.Types.Object.Mesh
{
    [Serializable]
    public class InvalidLLMeshAssetException : Exception
    {
        public InvalidLLMeshAssetException()
        {

        }

        public InvalidLLMeshAssetException(string message)
            : base(message)
        {

        }

        protected InvalidLLMeshAssetException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public InvalidLLMeshAssetException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
