// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Runtime.Serialization;

namespace SilverSim.Scene.Types.Object.Mesh
{
    [Serializable]
    public class InvalidSculptMeshAssetException : Exception
    {
        public InvalidSculptMeshAssetException()
        {

        }

        public InvalidSculptMeshAssetException(string message)
            : base(message)
        {

        }

        protected InvalidSculptMeshAssetException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public InvalidSculptMeshAssetException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
