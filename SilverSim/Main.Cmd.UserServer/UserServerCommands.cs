﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.AuthInfo;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.AuthInfo;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SilverSim.Main.Cmd.UserServer
{
    [Description("User Server Console Commands")]
    public class UserServerCommands : IPlugin
    {
        readonly string m_UserAccountServiceName;
        readonly string m_InventoryServiceName;
        readonly string m_AuthInfoServiceName;

        UserAccountServiceInterface m_UserAccountService;
        AuthInfoServiceInterface m_AuthInfoService;
        InventoryServiceInterface m_InventoryService;
        List<IUserAccountDeleteServiceInterface> m_AccountDeleteServices;
        List<IServiceURLsGetInterface> m_ServiceURLsGetters;

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
            m_ServiceURLsGetters = loader.GetServicesByValue<IServiceURLsGetInterface>();
            loader.CommandRegistry.AddCreateCommand("user", CreateUserCommand);
            loader.CommandRegistry.AddDeleteCommand("user", DeleteUserCommand);
            loader.CommandRegistry.AddChangeCommand("user", ChangeUserCommand);
            loader.CommandRegistry.AddShowCommand("user", ShowUserCommand);
            loader.CommandRegistry.AddShowCommand("serviceurls", ShowServiceUrlsCommand);
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

        [Description("Show service URLs")]
        void ShowServiceUrlsCommand(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            if (args[0] == "help")
            {
                io.Write("show serviceurls");
            }
            else
            {
                Dictionary<string, string> serviceurls = new Dictionary<string, string>();
                foreach(IServiceURLsGetInterface getter in m_ServiceURLsGetters)
                {
                    getter.GetServiceURLs(serviceurls);
                }

                StringBuilder sb = new StringBuilder("Service URLs:\n----------------------------------------\n");
                foreach(KeyValuePair<string, string> kvp in serviceurls)
                {
                    sb.AppendFormat("{0}={1}\n", kvp.Key, kvp.Value);
                }
                io.Write(sb.ToString());
            }
        }

        [Description("Show User")]
        void ShowUserCommand(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UserAccount account;
            if(args[0] == "help" || args.Count != 4)
            {
                io.Write("show user <firstname> <lastname>");
            }
            else if (limitedToScene != UUID.Zero)
            {
                io.Write("show user not allowed on limited console");
            }
            else if(m_UserAccountService.TryGetValue(UUID.Zero, args[2], args[3], out account))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("ID: {0}\n", account.Principal.ID);
                sb.AppendFormat("First Name: {0}\n", account.Principal.FirstName);
                sb.AppendFormat("Last Name: {0}\n", account.Principal.LastName);
                sb.AppendFormat("Level: {0}\n", account.UserLevel);
                sb.AppendFormat("Title: {0}\n", account.UserTitle);
                sb.AppendFormat("Created: {0}\n", account.Created.ToString());
                sb.AppendFormat("Email: {0}\n", account.Email);
                io.Write(sb.ToString());
            }
            else
            {
                io.WriteFormatted("Account {0} {1} does not exist", args[2], args[3]);
            }
        }

        [Description("Change user")]
        void ChangeUserCommand(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UserAccount account;
            if(args[0] == "help" || args.Count < 4 || args.Count % 2 != 0)
            {
                io.Write("change user <firstname> <lastname> (<token> <parameter>)*\n" +
                        "Token parameters:\n" +
                        "userlevel <level>\n" + 
                        "email <email>\n" +
                        "usertitle <usertitle>\n");
            }
            else if (limitedToScene != UUID.Zero)
            {
                io.Write("change user not allowed on limited console");
            }
            else if(m_UserAccountService.TryGetValue(UUID.Zero, args[2], args[3], out account))
            {
                for(int argi = 4; argi < args.Count; argi += 2)
                {
                    switch(args[argi])
                    {
                        case "userlevel":
                            account.UserLevel = int.Parse(args[argi + 1]);
                            if(account.UserLevel < -1 || account.UserLevel > 255)
                            {
                                io.WriteFormatted("User level parameter {0} is not valid", account.UserLevel);
                                return;
                            }
                            break;

                        case "email":
                            account.Email = args[argi + 1];
                            break;

                        case "usertitle":
                            account.UserTitle = args[argi + 1];
                            break;

                        default:
                            io.WriteFormatted("Unsupported token parameter {0}", args[argi]);
                            return;
                    }
                }

                m_UserAccountService.Update(account);
            }
            else
            {
                io.WriteFormatted("User \"{0}\" \"{1}\" not found", args[2], args[3]);
            }
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

            int count = m_AccountDeleteServices.Count;
            io.WriteFormatted("Processing {0} services", count);
            int index = 1;
            foreach(IUserAccountDeleteServiceInterface delService in m_AccountDeleteServices)
            {
                try
                {
                    delService.Remove(account.ScopeID, account.Principal.ID);
                }
                catch
                {
                    /* intentionally ignored */
                }
                io.WriteFormatted("{0}/{1} processed", index++, count);
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
