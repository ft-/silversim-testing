// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.IM;
using System;
using System.Runtime.Serialization;

namespace SilverSim.ServiceInterfaces.IM
{
    [Serializable]
    public class IMSendFailedException : Exception
    {
        public IMSendFailedException()
        {

        }

        public IMSendFailedException(string message)
            : base(message)
        {

        }

        protected IMSendFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public IMSendFailedException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    public abstract class IMServiceInterface
    {
        #region Constructor
        public IMServiceInterface()
        {

        }
        #endregion

        #region Methods
        public abstract void Send(GridInstantMessage im);
        #endregion
    }
}
