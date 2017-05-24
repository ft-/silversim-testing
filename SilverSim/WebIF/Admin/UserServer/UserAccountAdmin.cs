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

#pragma warning disable IDE0018
#pragma warning disable RCS1029

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.AuthInfo;
using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.AuthInfo;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.WebIF.Admin.UserServer
{
    [Description("WebIF User Account Admin Support")]
    [PluginName("UserAccountAdmin")]
    public class UserAccountAdmin : IPlugin
    {
        private readonly string m_UserAccountServiceName;
        private readonly string m_AuthInfoServiceName;
        private UserAccountServiceInterface m_UserAccountService;
        private AuthInfoServiceInterface m_AuthInfoService;
        private IAdminWebIF m_WebIF;
        private List<IUserAccountDeleteServiceInterface> m_AccountDeleteServices;

        public UserAccountAdmin(IConfig ownSection)
        {
            m_UserAccountServiceName = ownSection.GetString("UserAccountService", "UserAccountService");
            m_AuthInfoServiceName = ownSection.GetString("AuthInfoService", "AuthInfoService");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_AccountDeleteServices = loader.GetServicesByValue<IUserAccountDeleteServiceInterface>();

            m_UserAccountService = loader.GetService<UserAccountServiceInterface>(m_UserAccountServiceName);
            m_AuthInfoService = loader.GetService<AuthInfoServiceInterface>(m_AuthInfoServiceName);
            var webif = loader.GetAdminWebIF();
            m_WebIF = webif;
            webif.ModuleNames.Add("useraccounts");
            webif.AutoGrantRights["useraccounts.manage"].Add("useraccounts.view");
            webif.AutoGrantRights["useraccounts.delete"].Add("useraccounts.view");
            webif.AutoGrantRights["useraccounts.create"].Add("useraccounts.view");

            webif.JsonMethods.Add("useraccounts.search", HandleUserAccountSearch);
            webif.JsonMethods.Add("useraccount.get", HandleUserAccountGet);
            webif.JsonMethods.Add("useraccount.change", HandleUserAccountChange);
            webif.JsonMethods.Add("useraccount.delete", HandleUserAccountDelete);
            webif.JsonMethods.Add("useraccount.changepassword", HandleUserAccountChangePassword);
            webif.JsonMethods.Add("useraccount.create", HandleUserAccountCreate);
        }

        [AdminWebIfRequiredRight("useraccounts.create")]
        private void HandleUserAccountCreate(HttpRequest req, Map jsondata)
        {
            var account = new UserAccount();
            account.Principal.ID = UUID.Random;
            if (!jsondata.ContainsKey("firstname") ||
                !jsondata.ContainsKey("lastname") ||
                !jsondata.ContainsKey("password"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }
            account.Principal.FirstName = jsondata["firstname"].ToString();
            account.Principal.LastName = jsondata["lastname"].ToString();
            if (!jsondata.TryGetValue("scopeid", out account.ScopeID))
            {
                account.ScopeID = UUID.Zero;
            }
            if (jsondata.ContainsKey("userlevel"))
            {
                account.UserLevel = jsondata["userlevel"].AsInt;
                if (account.UserLevel > 255)
                {
                    m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                    return;
                }
            }
            if (jsondata.ContainsKey("usertitle"))
            {
                account.UserTitle = jsondata["usertitle"].ToString();
            }
            if (jsondata.ContainsKey("userflags"))
            {
                account.UserFlags = jsondata["userflags"].AsUInt;
            }
            if (jsondata.ContainsKey("email"))
            {
                account.Email = jsondata["email"].ToString();
            }
            try
            {
                m_UserAccountService.Add(account);
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.AlreadyExists);
                return;
            }
            var uai = new UserAuthInfo()
            {
                ID = account.Principal.ID,
                Password = jsondata["password"].ToString()
            };
            try
            {
                m_AuthInfoService.Store(uai);
            }
            catch
            {
                m_UserAccountService.Remove(account.ScopeID, account.Principal.ID);
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                return;
            }
            var res = new Map
            {
                { "id", account.Principal.ID },
                { "firstname", account.Principal.FirstName },
                { "lastname", account.Principal.LastName }
            };
            var resdata = new Map
            {
                ["account"] = res
            };
            m_WebIF.SuccessResponse(req, resdata);
        }

        [AdminWebIfRequiredRight("useraccounts.manage")]
        private void HandleUserAccountChange(HttpRequest req, Map jsondata)
        {
            UserAccount account;
            if (!jsondata.ContainsKey("id"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }
            if(!m_UserAccountService.TryGetValue(UUID.Zero, jsondata["id"].AsUUID, out account))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
                return;
            }

            if (jsondata.ContainsKey("userlevel"))
            {
                account.UserLevel = jsondata["userlevel"].AsInt;
                if(account.UserLevel > 255)
                {
                    m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                    return;
                }
            }
            if (jsondata.ContainsKey("usertitle"))
            {
                account.UserTitle = jsondata["usertitle"].ToString();
            }
            if (jsondata.ContainsKey("userflags"))
            {
                account.UserFlags = jsondata["userflags"].AsUInt;
            }
            if (jsondata.ContainsKey("email"))
            {
                account.Email = jsondata["email"].ToString();
            }
            try
            {
                m_UserAccountService.Update(account);
            }
            catch
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.AlreadyExists);
                return;
            }
            m_WebIF.SuccessResponse(req, new Map());
        }

        [AdminWebIfRequiredRight("useraccounts.delete")]
        private void HandleUserAccountDelete(HttpRequest req, Map jsondata)
        {
            IValue id;
            IValue scopeid;
            if (!jsondata.TryGetValue("id", out id))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }
            if (!jsondata.TryGetValue("scopeid", out scopeid))
            {
                scopeid = UUID.Zero;
            }

            if (!m_UserAccountService.ContainsKey(scopeid.AsUUID, id.AsUUID))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                foreach (var delService in m_AccountDeleteServices)
                {
                    try
                    {
                        delService.Remove(scopeid.AsUUID, id.AsUUID);
                    }
                    catch
                    {
                        /* intentionally ignored */
                    }
                }
                m_WebIF.SuccessResponse(req, new Map());
            }
        }

        [AdminWebIfRequiredRight("useraccounts.changepassword")]
        private void HandleUserAccountChangePassword(HttpRequest req, Map jsondata)
        {
            UUID id;
            UserAuthInfo uai;
            if (!jsondata.TryGetValue("id", out id) ||
                !jsondata.ContainsKey("password"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }
            else if (!m_UserAccountService.ContainsKey(UUID.Zero, id))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
            else
            {
                try
                {
                    uai = m_AuthInfoService[id];
                }
                catch
                {
                    m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidParameter);
                    return;
                }
                uai.Password = jsondata["password"].ToString();
                try
                {
                    m_AuthInfoService.Store(uai);
                }
                catch
                {
                    m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotPossible);
                    return;
                }
            }
            m_WebIF.SuccessResponse(req, new Map());
        }

        [AdminWebIfRequiredRight("useraccount.get")]
        private void HandleUserAccountGet(HttpRequest req, Map jsondata)
        {
            IValue id;
            if (!jsondata.TryGetValue("id", out id))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }
            UUID scopeid;
            IValue scopeiv;
            if (!jsondata.TryGetValue("scopeid", out scopeiv))
            {
                scopeid = UUID.Zero;
            }
            else
            {
                scopeid = scopeiv.AsUUID;
            }

            UserAccount acc;
            if (m_UserAccountService.TryGetValue(scopeid, id.AsUUID, out acc))
            {
                var result = new Map
                {
                    { "id", acc.Principal.ID },
                    { "firstname", acc.Principal.FirstName },
                    { "lastname", acc.Principal.LastName },
                    { "email", acc.Email },
                    { "userlevel", acc.UserLevel },
                    { "userflags", acc.UserFlags.ToString() },
                    { "usertitle", acc.UserTitle }
                };
                var resdata = new Map
                {
                    ["account"] = result
                };
                m_WebIF.SuccessResponse(req, resdata);
            }
            else
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
        }

        [AdminWebIfRequiredRight("useraccounts.view")]
        private void HandleUserAccountSearch(HttpRequest req, Map jsondata)
        {
            var res = new Map();
            var accountsRes = new AnArray();
            UUID scopeid;
            int start = 0;
            int count = 1000;
            if (!jsondata.TryGetValue("scopeid", out scopeid))
            {
                scopeid = UUID.Zero;
            }
            if (!jsondata.ContainsKey("query"))
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.InvalidRequest);
                return;
            }
            if (jsondata.ContainsKey("start"))
            {
                start = jsondata["start"].AsInt;
            }
            if (jsondata.ContainsKey("count"))
            {
                count = jsondata["count"].AsInt;
            }
            if (count > 1000 || count < 0)
            {
                count = 1000;
            }
            string query = jsondata["query"].ToString();
            foreach (var acc in m_UserAccountService.GetAccounts(scopeid, query))
            {
                if (start > 0)
                {
                    --start;
                }
                else
                {
                    if (count-- == 0)
                    {
                        break;
                    }
                    var accountData = new Map
                    {
                        { "scopeid", acc.ScopeID },
                        { "id", acc.Principal.ID },
                        { "firstname", acc.Principal.FirstName },
                        { "lastname", acc.Principal.LastName }
                    };
                    accountsRes.Add(accountData);
                }
            }
            res.Add("accounts", accountsRes);
            m_WebIF.SuccessResponse(req, res);
        }
    }
}
