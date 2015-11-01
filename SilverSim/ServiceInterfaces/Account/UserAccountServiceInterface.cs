﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Account;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace SilverSim.ServiceInterfaces.Account
{
    [Serializable]
    public class UserAccountNotFoundException : KeyNotFoundException
    {
        public UserAccountNotFoundException()
        {

        }

        public UserAccountNotFoundException(string message)
            : base(message)
        {

        }

        protected UserAccountNotFoundException(SerializationInfo info, StreamingContext context):
            base(info, context)
        {

        }

        public UserAccountNotFoundException(string message, Exception innerException)
            : base(message, innerException)
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

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract UserAccount this[UUID scopeID, UUID accountID]
        {
            get;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract UserAccount this[UUID scopeID, string email]
        {
            get;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
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
