// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

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

        public abstract bool ContainsKey(UUID scopeID, UUID accountID);
        public abstract bool ContainsKey(UUID scopeID, string email);
        public abstract bool ContainsKey(UUID scopeID, string firstName, string lastName);

        public abstract bool TryGetValue(UUID scopeID, UUID accountID, out UserAccount account);
        public abstract bool TryGetValue(UUID scopeID, string email, out UserAccount account);
        public abstract bool TryGetValue(UUID scopeID, string firstName, string lastName, out UserAccount account);

        public abstract List<UserAccount> GetAccounts(UUID scopeID, string query);

        #region Optionally supported services
        public abstract void Add(UserAccount userAccount);
        public abstract void Update(UserAccount userAccount);

        public abstract void Remove(UUID scopeID, UUID accountID);
        public abstract void SetEverLoggedIn(UUID scopeID, UUID accountID);
        #endregion
    }
}
