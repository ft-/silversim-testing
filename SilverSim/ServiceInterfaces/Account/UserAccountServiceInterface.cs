// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Account;
using System;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Account
{
    [Serializable]
    public class UserAccountNotFoundException : KeyNotFoundException
    {
        public UserAccountNotFoundException()
        {

        }
    }

    public abstract class UserAccountServiceInterface : IUserAccountDeleteServiceInterface
    {
        #region Constructor
        public UserAccountServiceInterface()
        {

        }
        #endregion

        public abstract UserAccount this[UUID scopeID, UUID accountID]
        {
            get;
        }

        public abstract UserAccount this[UUID scopeID, string email]
        {
            get;
        }

        public abstract UserAccount this[UUID scopeID, string firstName, string lastName]
        {
            get;
        }

        public abstract List<UserAccount> GetAccounts(UUID scopeID, string query);

        #region Optionally supported services
        public abstract void Add(UserAccount userAccount);
        public abstract void Update(UserAccount userAccount);

        public abstract void Remove(UUID scopeID, UUID accountID);
        #endregion
    }
}
