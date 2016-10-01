// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Runtime.Serialization;

namespace SilverSim.Types.Asset.Format.Mesh
{
    public class NoSuchMeshDataException : Exception
    {
        public NoSuchMeshDataException()
        {

        }

        public NoSuchMeshDataException(string message)
            : base(message)
        {

        }

        protected NoSuchMeshDataException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public NoSuchMeshDataException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
