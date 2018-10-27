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

using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.Agent;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace SilverSim.Database.Memory.UserAccounts
{
    [Description("Memory UserAccount Backend")]
    [PluginName("UserAccounts")]
    public sealed class MemoryUserAccountService : UserAccountServiceInterface, IPlugin, IUserAccountSerialNoInterface
    {
        private readonly RwLockedDictionary<UUID, UserAccount> m_Data = new RwLockedDictionary<UUID, UserAccount>();
        private Uri m_HomeURI;
        private long m_SerialNumber;

        public ulong SerialNumber
        {
            get
            {
                long serno = Interlocked.Read(ref m_SerialNumber);
                if(serno == 0)
                {
                    ++serno;
                }
                return (ulong)serno;
            }
        }

        public List<UGUIWithName> AccountList
        {
            get
            {
                var list = new List<UGUIWithName>();
                foreach(UserAccount acc in m_Data.Values)
                {
                    list.Add(new UGUIWithName(acc.Principal.ID, acc.Principal.FirstName, acc.Principal.LastName));
                }
                return list;
            }
        }

        #region Constructor
        public void Startup(ConfigurationLoader loader)
        {
            m_HomeURI = new Uri(loader.HomeURI);
        }
        #endregion

        public override bool ContainsKey(UUID accountID)
        {
            UserAccount acc;
            return m_Data.TryGetValue(accountID, out acc);
        }

        public override bool TryGetValue(UUID accountID, out UserAccount account)
        {
            if(m_Data.TryGetValue(accountID, out account))
            {
                account = new UserAccount(account)
                {
                    IsLocalToGrid = true
                };
                return true;
            }
            return false;
        }

        public override bool ContainsKey(string email)
        {
            var result = from account in m_Data.Values
                                              where account.Email.Equals(email, StringComparison.OrdinalIgnoreCase)
                                              select true;
            foreach(bool acc in result)
            {
                return true;
            }

            return false;
        }

        public override bool TryGetValue(string email, out UserAccount account)
        {
            var result = from accountdata in m_Data.Values
                                       where accountdata.Email.Equals(email, StringComparison.OrdinalIgnoreCase)
                                       select accountdata;
            foreach(UserAccount acc in result)
            {
                account = new UserAccount(acc)
                {
                    IsLocalToGrid = true
                };
                return true;
            }
            account = default(UserAccount);
            return false;
        }

        public override bool ContainsKey(string firstName, string lastName)
        {
            var result = from account in m_Data.Values
                                       where account.Principal.FirstName.Equals(firstName, StringComparison.OrdinalIgnoreCase) &&
                                       account.Principal.LastName.Equals(lastName, StringComparison.OrdinalIgnoreCase)
                                       select true;
            foreach (bool acc in result)
            {
                return true;
            }

            return false;
        }

        public override bool TryGetValue(string firstName, string lastName, out UserAccount account)
        {
            var result = from accountdata in m_Data.Values
                                       where accountdata.Principal.FirstName.Equals(firstName, StringComparison.OrdinalIgnoreCase) &&
                                       accountdata.Principal.LastName.Equals(lastName, StringComparison.OrdinalIgnoreCase)
                                       select accountdata;
            foreach (UserAccount acc in result)
            {
                account = new UserAccount(acc)
                {
                    IsLocalToGrid = true
                };
                return true;
            }

            account = default(UserAccount);
            return false;
        }

        public override List<UserAccount> GetAccounts(string query)
        {
            string[] words = query.Split(new char[] {' '}, 2);
            var accounts = new List<UserAccount>();
            IEnumerable<UserAccount> res;
            if (query.Trim().Length == 0)
            {
                res = from data in m_Data.Values where true select new UserAccount(data);
            }
            else
            {
                res = (words.Length == 1) ?
                    from data in m_Data.Values where data.Principal.FirstName.Equals(words[0], StringComparison.OrdinalIgnoreCase) || data.Principal.LastName.Equals(words[0], StringComparison.OrdinalIgnoreCase) select new UserAccount(data) :
                    from data in m_Data.Values where data.Principal.FirstName.Equals(words[0], StringComparison.OrdinalIgnoreCase) && data.Principal.LastName.Equals(words[1], StringComparison.OrdinalIgnoreCase) select new UserAccount(data);
            }
            foreach(var acc in res)
            {
                accounts.Add(acc);
            }
            return accounts;
        }

        public override void Add(UserAccount userAccount)
        {
            var uac = new UserAccount(userAccount);
            uac.Principal.HomeURI = m_HomeURI;
            m_Data.Add(userAccount.Principal.ID, uac);
            Interlocked.Increment(ref m_SerialNumber);
        }

        public override void Remove(UUID accountID)
        {
            m_Data.Remove(accountID);
        }

        #region Online Status
        public override void LoggedOut(UUID accountID, UserRegionData regionData)
        {
            UserAccount ua = m_Data[accountID];
            ua.LastLogout = Date.Now;
            if (regionData != null)
            {
                ua.LastRegion = regionData.Clone();
            }
        }

        public override void SetHome(UUID accountID, UserRegionData regionData)
        {
            if (regionData == null)
            {
                throw new ArgumentNullException(nameof(regionData));
            }
            UserAccount ua = m_Data[accountID];
            ua.HomeRegion = regionData.Clone();
        }

        public override void SetPosition(UUID accountID, UserRegionData regionData)
        {
            if (regionData == null)
            {
                throw new ArgumentNullException(nameof(regionData));
            }
            UserAccount ua = m_Data[accountID];
            ua.LastRegion = regionData.Clone();
        }
        #endregion

        #region Optionally supported services
        public override void SetEverLoggedIn(UUID accountID)
        {
            UserAccount ua;
            if(m_Data.TryGetValue(accountID, out ua))
            {
                ua.IsEverLoggedIn = true;
            }
        }

        public override void SetEmail(UUID accountID, string email)
        {
            if (email == null)
            {
                throw new ArgumentNullException(nameof(email));
            }
            UserAccount ua = m_Data[accountID];
            ua.Email = email;
        }

        public override void SetUserLevel(UUID accountID, int userLevel)
        {
            if (userLevel < -1 || userLevel > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(userLevel));
            }
            UserAccount ua = m_Data[accountID];
            ua.UserLevel = userLevel;
        }

        public override void SetUserFlags(UUID accountID, UserFlags userFlags)
        {
            UserAccount ua = m_Data[accountID];
            ua.UserFlags = userFlags;
        }

        public override void SetUserTitle(UUID accountID, string title)
        {
            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }
            UserAccount ua = m_Data[accountID];
            ua.UserTitle = title;
        }
        #endregion

    }
}
