// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.IM;
using System;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.IM
{
    [Serializable]
    public class IMOfflineStoreFailedException : Exception
    {
        public IMOfflineStoreFailedException()
        {

        }

        public IMOfflineStoreFailedException(string message)
            : base(message)
        {

        }
    }

    [Serializable]
    public class IMOfflineRetrieveFailedException : Exception
    {
        public IMOfflineRetrieveFailedException()
        {

        }

        public IMOfflineRetrieveFailedException(string message)
            : base(message)
        {

        }
    }

    public abstract class OfflineIMServiceInterface
    {
        #region Constructor
        public OfflineIMServiceInterface()
        {

        }
        #endregion

        #region Methods
        public abstract void StoreOfflineIM(GridInstantMessage im);
        public abstract List<GridInstantMessage> GetOfflineIMs(UUID principalID);
        public abstract void DeleteOfflineIM(ulong offlineImID);
        #endregion
    }
}
