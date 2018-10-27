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
    [PluginName("UserServerCommands")]
    public class UserServerCommands : IPlugin
    {
        private readonly string m_UserAccountServiceName;
        private readonly string m_InventoryServiceName;
        private readonly string m_AuthInfoServiceName;

        private UserAccountServiceInterface m_UserAccountService;
        private AuthInfoServiceInterface m_AuthInfoService;
        private InventoryServiceInterface m_InventoryService;
        private List<IUserAccountDeleteServiceInterface> m_AccountDeleteServices;
        private List<IServiceURLsGetInterface> m_ServiceURLsGetters;

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
            loader.CommandRegistry.AddResetCommand("user", ResetUserPasswordCommand);
        }

        private bool IsNameValid(string s)
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
        private void ShowServiceUrlsCommand(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            if (args[0] == "help")
            {
                io.Write("show serviceurls");
            }
            else
            {
                var serviceurls = new Dictionary<string, string>();
                foreach(IServiceURLsGetInterface getter in m_ServiceURLsGetters)
                {
                    getter.GetServiceURLs(serviceurls);
                }

                var sb = new StringBuilder("Service URLs:\n----------------------------------------\n");
                foreach(KeyValuePair<string, string> kvp in serviceurls)
                {
                    sb.AppendFormat("{0}={1}\n", kvp.Key, kvp.Value);
                }
                io.Write(sb.ToString());
            }
        }

        [Description("Show User")]
        private void ShowUserCommand(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
            else if(m_UserAccountService.TryGetValue(args[2], args[3], out account))
            {
                var sb = new StringBuilder();
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

        [Description("Reset user password")]
        private void ResetUserPasswordCommand(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UserAccount account;
            if (args[0] == "help" || args.Count < 5 || args[2] != "password")
            {
                io.Write("reset user password <firstname> <lastname>");
            }
            else if (limitedToScene != UUID.Zero)
            {
                io.Write("reset user password not allowed on limited console");
            }
            else if (m_UserAccountService.TryGetValue(args[3], args[4], out account))
            {
                var authInfo = new UserAuthInfo
                {
                    ID = account.Principal.ID,
                    Password = io.GetPass("New password")
                };
                m_AuthInfoService.Store(authInfo);
            }
            else
            {
                io.WriteFormatted("User \"{0}\" \"{1}\" not found", args[2], args[3]);
            }
        }

        [Description("Change user")]
        private void ChangeUserCommand(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
            else if(m_UserAccountService.TryGetValue(args[2], args[3], out account))
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
                            m_UserAccountService.SetUserLevel(account.Principal.ID, account.UserLevel);
                            break;

                        case "email":
                            account.Email = args[argi + 1];
                            m_UserAccountService.SetEmail(account.Principal.ID, account.Email);
                            break;

                        case "usertitle":
                            account.UserTitle = args[argi + 1];
                            m_UserAccountService.SetUserTitle(account.Principal.ID, account.UserTitle);
                            break;

                        default:
                            io.WriteFormatted("Unsupported token parameter {0}", args[argi]);
                            return;
                    }
                }
            }
            else
            {
                io.WriteFormatted("User \"{0}\" \"{1}\" not found", args[2], args[3]);
            }
        }

        [Description("Create user")]
        private void CreateUserCommand(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
            else if (m_UserAccountService.ContainsKey(args[1], args[2]))
            {
                io.Write("user already created");
            }
            else
            {
                var account = new UserAccount
                {
                    IsLocalToGrid = true
                };
                account.Principal.ID = UUID.Random;
                account.Principal.FirstName = args[2];
                account.Principal.LastName = args[3];
                account.UserLevel = 0;

                var authInfo = new UserAuthInfo
                {
                    ID = account.Principal.ID,
                    Password = io.GetPass("Password")
                };
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
                    m_UserAccountService.Remove(account.Principal.ID);
                    io.WriteFormatted("Could not add user account");
                }
            }
        }

        [Description("Delete user")]
        private void DeleteUserCommand(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
                if (UUID.TryParse(args[2], out userid) && m_UserAccountService.TryGetValue(userid, out account))
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
                if(m_UserAccountService.TryGetValue(args[2], args[3], out account))
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
                    delService.Remove(account.Principal.ID);
                }
                catch
                {
                    /* intentionally ignored */
                }
                io.WriteFormatted("{0}/{1} processed", index++, count);
            }
        }
    }
}
