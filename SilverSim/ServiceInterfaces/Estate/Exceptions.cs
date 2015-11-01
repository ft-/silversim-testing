// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace SilverSim.ServiceInterfaces.Estate
{
    [Serializable]
    public class EstateUpdateFailedException : Exception
    {
        public EstateUpdateFailedException()
        {

        }

        public EstateUpdateFailedException(string message)
            : base(message)
        {

        }

        protected EstateUpdateFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public EstateUpdateFailedException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
