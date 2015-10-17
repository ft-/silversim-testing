// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.IM;
using System;

namespace SilverSim.ServiceInterfaces.IM
{
    [Serializable]
    public class IMSendFailedException : Exception
    {
        public IMSendFailedException()
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
