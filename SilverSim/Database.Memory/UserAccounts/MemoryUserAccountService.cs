// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Account;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SilverSim.Database.Memory.UserAccounts
{
    #region Service Implementation
    [Description("Memory UserAccount Backend")]
    public sealed class MemoryUserAccountService : UserAccountServiceInterface, IPlugin
    {
        readonly RwLockedDictionary<UUID, UserAccount> m_Data = new RwLockedDictionary<UUID, UserAccount>();

        #region Constructor
        public MemoryUserAccountService()
        {
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* nothing to do */
        }
        #endregion

        public override bool ContainsKey(UUID scopeID, UUID accountID)
        {
            UserAccount acc;
            return m_Data.TryGetValue(accountID, out acc) && (scopeID == UUID.Zero || acc.ScopeID == scopeID);
        }

        public override bool TryGetValue(UUID scopeID, UUID accountID, out UserAccount account)
        {
            if(m_Data.TryGetValue(accountID, out account) && (scopeID == UUID.Zero || account.ScopeID == scopeID))
            {
                account = new UserAccount(account);
                account.IsLocalToGrid = true;
                return true;
            }
            return false;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override UserAccount this[UUID scopeID, UUID accountID]
        {
            get
            {
                UserAccount account;
                if (!TryGetValue(scopeID, accountID, out account))
                {
                    throw new UserAccountNotFoundException();
                }
                account = new UserAccount(account);
                account.IsLocalToGrid = true;
                return account;
            }
        }

        public override bool ContainsKey(UUID scopeID, string email)
        {
            IEnumerable<bool> result = from account in m_Data.Values
                                              where account.Email.ToLower().Equals(email.ToLower()) &&
                       (scopeID == UUID.Zero || account.ScopeID == scopeID)
                                              select true;
            foreach(bool acc in result)
            {
                return true;
            }

            return false;
        }

        public override bool TryGetValue(UUID scopeID, string email, out UserAccount account)
        {
            IEnumerable<UserAccount> result = from accountdata in m_Data.Values
                                       where accountdata.Email.ToLower().Equals(email.ToLower()) &&
                (scopeID == UUID.Zero || accountdata.ScopeID == scopeID)
                                       select accountdata;
            foreach(UserAccount acc in result)
            {
                account = new UserAccount(acc);
                account.IsLocalToGrid = true;
                return true;
            }
            account = default(UserAccount);
            return false;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override UserAccount this[UUID scopeID, string email]
        {
            get 
            {
                UserAccount account;
                if(!TryGetValue(scopeID, email, out account))
                {
                    throw new UserAccountNotFoundException();
                }
                return account;
            }
        }

        public override bool ContainsKey(UUID scopeID, string firstName, string lastName)
        {
            IEnumerable<bool> result = from account in m_Data.Values
                                       where account.Principal.FirstName.ToLower().Equals(firstName.ToLower()) &&
                                       account.Principal.LastName.ToLower().Equals(lastName.ToLower()) &&
                (scopeID == UUID.Zero || account.ScopeID == scopeID)
                                       select true;
            foreach (bool acc in result)
            {
                return true;
            }

            return false;
        }

        public override bool TryGetValue(UUID scopeID, string firstName, string lastName, out UserAccount account)
        {
            IEnumerable<UserAccount> result = from accountdata in m_Data.Values
                                       where accountdata.Principal.FirstName.ToLower().Equals(firstName.ToLower()) &&
                                       accountdata.Principal.LastName.ToLower().Equals(lastName.ToLower()) &&
                (scopeID == UUID.Zero || accountdata.ScopeID == scopeID)
                                       select accountdata;
            foreach (UserAccount acc in result)
            {
                account = new UserAccount(acc);
                account.IsLocalToGrid = true;
                return true;
            }

            account = default(UserAccount);
            return false;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override UserAccount this[UUID scopeID, string firstName, string lastName]
        {
            get 
            {
                UserAccount account;
                if(!TryGetValue(scopeID, firstName, lastName, out account))
                {
                    throw new UserAccountNotFoundException();
                }
                return account;
            }
        }

        public override List<UserAccount> GetAccounts(UUID scopeID, string query)
        {
            string[] words = query.Split(new char[] {' '}, 2);
            List<UserAccount> accounts = new List<UserAccount>();
            if(words.Length == 0)
            {
                return accounts;
            }

            IEnumerable<UserAccount> res;
            res = (words.Length == 1) ?
                from data in m_Data.Values where data.Principal.FirstName.ToLower().Equals(words[0].ToLower()) || data.Principal.LastName.ToLower().Equals(words[0].ToLower()) select new UserAccount(data) :
                from data in m_Data.Values where data.Principal.FirstName.ToLower().Equals(words[0].ToLower()) && data.Principal.LastName.ToLower().Equals(words[1].ToLower()) select new UserAccount(data);

            foreach(UserAccount acc in res)
            {
                accounts.Add(acc);
            }
            return accounts;
        }

        public override void Add(UserAccount userAccount)
        {
            UserAccount uac = new UserAccount(userAccount);
            uac.IsLocalToGrid = true;
            m_Data.Add(userAccount.Principal.ID, uac);
        }

        public override void Update(UserAccount userAccount)
        {
            UserAccount uac = new UserAccount(userAccount);
            uac.IsLocalToGrid = true;
            m_Data[userAccount.Principal.ID] = uac;
        }

        public override void Remove(UUID scopeID, UUID accountID)
        {
            m_Data.RemoveIf(accountID, delegate (UserAccount acc) { return acc.ScopeID == scopeID || scopeID == UUID.Zero; });
        }

        public override void SetEverLoggedIn(UUID scopeID, UUID accountID)
        {
            UserAccount ua;
            if(m_Data.TryGetValue(accountID, out ua))
            {
                ua.IsEverLoggedIn = true;
            }
        }
    }
    #endregion

    #region Factory
    [PluginName("UserAccounts")]
    public class MemoryUserAccountServiceFactory : IPluginFactory
    {
        public MemoryUserAccountServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MemoryUserAccountService();
        }
    }
    #endregion

}
