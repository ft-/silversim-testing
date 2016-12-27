// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
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
    public class UserAccountAdmin : IPlugin
    {
        private static readonly ILog m_Log = LogManager.GetLogger("ADMIN WEB IF - USER ACCOUNTS");

        readonly string m_UserAccountServiceName;
        readonly string m_AuthInfoServiceName;
        UserAccountServiceInterface m_UserAccountService;
        AuthInfoServiceInterface m_AuthInfoService;
        IAdminWebIF m_WebIF;
        List<IUserAccountDeleteServiceInterface> m_AccountDeleteServices;

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
            IAdminWebIF webif = loader.GetAdminWebIF();
            m_WebIF = webif;
            webif.ModuleNames.Add("useraccounts");
            webif.AutoGrantRights["useraccounts.manage"].Add("useraccounts.view");
            webif.AutoGrantRights["useraccounts.delete"].Add("useraccounts.view");
            webif.AutoGrantRights["useraccounts.create"].Add("useraccounts.view");

            webif.JsonMethods.Add("useraccounts.search", HandleUserAccountSearch);
            webif.JsonMethods.Add("useraccount.get", HandleUserAccountGet);
            webif.JsonMethods.Add("useraccount.delete", HandleUserAccountDelete);
            webif.JsonMethods.Add("useraccount.changepassword", HandleUserAccountChangePassword);
            webif.JsonMethods.Add("useraccount.create", HandleUserAccountCreate);
        }

        [AdminWebIfRequiredRight("useraccounts.create")]
        void HandleUserAccountCreate(HttpRequest req, Map jsondata)
        {
            UserAccount account = new UserAccount();
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
            UserAuthInfo uai = new UserAuthInfo();
            uai.ID = account.Principal.ID;
            uai.Password = jsondata["password"].ToString();
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
            Map res = new Map();
            res.Add("id", account.Principal.ID);
            res.Add("firstname", account.Principal.FirstName);
            res.Add("lastname", account.Principal.LastName);
            Map resdata = new Map();
            resdata.Add("account", res);
            m_WebIF.SuccessResponse(req, resdata);
        }

        [AdminWebIfRequiredRight("useraccounts.delete")]
        void HandleUserAccountDelete(HttpRequest req, Map jsondata)
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
                foreach (IUserAccountDeleteServiceInterface delService in m_AccountDeleteServices)
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
        void HandleUserAccountChangePassword(HttpRequest req, Map jsondata)
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
        void HandleUserAccountGet(HttpRequest req, Map jsondata)
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
                Map result = new Map();
                result.Add("id", acc.Principal.ID);
                result.Add("firstname", acc.Principal.FirstName);
                result.Add("lastname", acc.Principal.LastName);
                result.Add("email", acc.Email);
                result.Add("userlevel", acc.UserLevel);
                result.Add("userflags", acc.UserFlags.ToString());
                result.Add("usertitle", acc.UserTitle);
                Map resdata = new Map();
                resdata.Add("account", result);
                m_WebIF.SuccessResponse(req, resdata);
            }
            else
            {
                m_WebIF.ErrorResponse(req, AdminWebIfErrorResult.NotFound);
            }
        }

        [AdminWebIfRequiredRight("useraccounts.view")]
        void HandleUserAccountSearch(HttpRequest req, Map jsondata)
        {
            Map res = new Map();
            AnArray accountsRes = new AnArray();
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
            IEnumerable<UserAccount> accounts = m_UserAccountService.GetAccounts(scopeid, query);
            foreach (UserAccount acc in accounts)
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
                    Map accountData = new Map();
                    accountData.Add("scopeid", acc.ScopeID);
                    accountData.Add("id", acc.Principal.ID);
                    accountData.Add("firstname", acc.Principal.FirstName);
                    accountData.Add("lastname", acc.Principal.LastName);
                    accountsRes.Add(accountData);
                }
            }
            res.Add("accounts", accountsRes);
            m_WebIF.SuccessResponse(req, res);
        }
    }

    [PluginName("UserAccountAdmin")]
    public class UserAccountAdminFactory : IPluginFactory
    {
        public UserAccountAdminFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new UserAccountAdmin(ownSection);
        }
    }
}
