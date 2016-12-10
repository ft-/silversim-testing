// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.AuthInfo;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.AuthInfo;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Main.Cmd.UserServer
{
    public class UserServerCommands : IPlugin
    {
        readonly string m_UserAccountServiceName;
        readonly string m_InventoryServiceName;
        readonly string m_AuthInfoServiceName;

        UserAccountServiceInterface m_UserAccountService;
        AuthInfoServiceInterface m_AuthInfoService;
        InventoryServiceInterface m_InventoryService;
        List<IUserAccountDeleteServiceInterface> m_AccountDeleteServices;

        public UserServerCommands(IConfig ownSection)
        {
            m_UserAccountServiceName = ownSection.GetString("UserAccountService", "UserAccountService");
            m_InventoryServiceName = ownSection.GetString("InventoryService", "InventoryService");
            m_AuthInfoServiceName = ownSection.GetString("AuthInfoService", "AuthInfoService");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_UserAccountService = loader.GetService<UserAccountServiceInterface>(m_UserAccountServiceName);
            m_InventoryService = loader.GetService<InventoryServiceInterface>(m_InventoryServiceName);
            m_AccountDeleteServices = loader.GetServicesByValue<IUserAccountDeleteServiceInterface>();
            m_AuthInfoService = loader.GetService<AuthInfoServiceInterface>(m_AuthInfoServiceName);
            loader.CommandRegistry.AddCreateCommand("user", CreateUserCommand);
            loader.CommandRegistry.AddDeleteCommand("user", DeleteUserCommand);
        }

        bool IsNameValid(string s)
        {
            foreach(char c in s)
            {
                if(!char.IsLetterOrDigit(c))
                {
                    return false;
                }
            }
            return true;
        }

        [Description("Create user")]
        void CreateUserCommand(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            if(args[0] == "help" || args.Count != 4)
            {
                io.Write("create user <firstname> <lastname>");
            }
            else if (limitedToScene != UUID.Zero)
            {
                io.Write("create user not allowed on limited console");
            }
            else if(!IsNameValid(args[1]) || !IsNameValid(args[2]))
            {
                io.Write("name can only contains letters or digits");
            }
            else if (m_UserAccountService.ContainsKey(UUID.Zero, args[1], args[2]))
            {
                io.Write("user already created");
            }
            else
            {
                UserAccount account = new UserAccount();
                account.IsLocalToGrid = true;
                account.Principal.ID = UUID.Random;
                account.Principal.FirstName = args[2];
                account.Principal.LastName = args[3];
                account.UserLevel = 0;

                UserAuthInfo authInfo = new UserAuthInfo();
                authInfo.ID = account.Principal.ID;
                authInfo.Password = io.GetPass("Password");

                try
                {
                    m_UserAccountService.Add(account);
                }
                catch
                {
                    io.WriteFormatted("Could not add user account");
                }

                try
                {
                    m_AuthInfoService.Store(authInfo);
                }
                catch
                {
                    m_UserAccountService.Remove(account.ScopeID, account.Principal.ID);
                    io.WriteFormatted("Could not add user account");
                }
            }
        }

        [Description("Delete user")]
        void DeleteUserCommand(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UserAccount account;
            if (args[0] == "help" || args.Count < 3 || args.Count > 4)
            {
                io.Write("delete user <uuid>\ndelete user <first> <last>");
                return;
            }
            else if (limitedToScene != UUID.Zero)
            {
                io.Write("delete user not allowed on limited console");
                return;
            }
            else if (args.Count == 3)
            {
                UUID userid;
                if (UUID.TryParse(args[2], out userid) && m_UserAccountService.TryGetValue(UUID.Zero, userid, out account))
                {
                    /* account found */
                }
                else
                {
                    io.Write("Invalid UUID given");
                    return;
                }
            }
            else 
            {
                if(m_UserAccountService.TryGetValue(UUID.Zero, args[2], args[3], out account))
                {
                    /* account found */
                }
                else
                {
                    io.Write("Account not found");
                    return;
                }
            }

            io.WriteFormatted("Deleting user {0}.{1} (ID {2})", account.Principal.FirstName, account.Principal.LastName, account.Principal.ID);

            foreach(IUserAccountDeleteServiceInterface delService in m_AccountDeleteServices)
            {
                delService.Remove(account.ScopeID, account.Principal.ID);
            }
        }
    }

    [PluginName("UserServerCommands")]
    public class UserServerCommandsFactory : IPluginFactory
    {
        public UserServerCommandsFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new UserServerCommands(ownSection);
        }
    }
}
