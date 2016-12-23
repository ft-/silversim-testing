// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Database.MySQL._Migration;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.Types;
using SilverSim.Types.Friends;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Database.MySQL.Friends
{
    public static class MySQLFriendsExtensionMethods
    {
        public static FriendInfo ToFriendInfo(this MySqlDataReader reader)
        {
            FriendInfo fi = new FriendInfo();
            fi.User.ID = reader.GetUUID("User");
            fi.Friend.ID = reader.GetUUID("Friend");
            fi.Secret = reader.GetString("Secret");
            fi.FriendGivenFlags = reader.GetEnum<FriendRightFlags>("RightsToFriend");
            fi.UserGivenFlags = reader.GetEnum<FriendRightFlags>("RightsToUser");
            return fi;
        }
    }

    [Description("MySQL Friends Backend")]
    public class MySQLFriendsService : FriendsServiceInterface, IPlugin, IDBServiceInterface, IUserAccountDeleteServiceInterface
    {
        readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL FRIENDS SERVICE");
        readonly List<AvatarNameServiceInterface> m_AvatarNameServices = new List<AvatarNameServiceInterface>();
        readonly string[] m_AvatarNameServiceNames;

        public MySQLFriendsService(string connectionString, string avatarNameServices)
        {
            m_ConnectionString = connectionString;
            m_AvatarNameServiceNames = avatarNameServices.Split(',');
        }

        const string m_InnerJoinSelectFull = "SELECT A.*, B.RightsToFriend AS RightsToUser FROM friends AS A INNER JOIN friends as B ON A.FriendID LIKE B.UserID AND A.UserID LIKE B.FriendID ";

        public void ResolveUUI(FriendInfo fi)
        {
            foreach(AvatarNameServiceInterface service in m_AvatarNameServices)
            {
                UUI uui;
                if(fi.Friend.IsAuthoritative && fi.User.IsAuthoritative)
                {
                    break;
                }

                if(!fi.Friend.IsAuthoritative)
                {
                    if(service.TryGetValue(fi.Friend, out uui))
                    {
                        fi.Friend = uui;
                    }
                }
                if(!fi.User.IsAuthoritative)
                {
                    if(service.TryGetValue(fi.User, out uui))
                    {
                        fi.User = uui;
                    }
                }
            }
        }

        public override List<FriendInfo> this[UUI user]
        {
            get
            {
                List<FriendInfo> fis = new List<FriendInfo>();
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand(m_InnerJoinSelectFull + "WHERE A.UserID LIKE ?id", connection))
                    {
                        cmd.Parameters.AddParameter("?id", user.ID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while(reader.Read())
                            {
                                FriendInfo fi = reader.ToFriendInfo();
                                ResolveUUI(fi);
                                fis.Add(fi);
                            }
                        }
                    }
                }

                return fis;
            }
        }

        public override FriendInfo this[UUI user, UUI friend]
        {
            get
            {
                FriendInfo fi;
                if(TryGetValue(user, friend, out fi))
                {
                    ResolveUUI(fi);
                    return fi;
                }
                throw new KeyNotFoundException();
            }
        }

        public override void Delete(FriendInfo fi)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM friends WHERE (UserID LIKE ?userid AND FriendID LIKE ?friendid) OR (UserID LIKE ?friendid AND FriendID LIKE ?userid)", connection))
                {
                    cmd.Parameters.AddParameter("?userid", fi.User.ID);
                    cmd.Parameters.AddParameter("?friendid", fi.Friend.ID);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void ProcessMigrations()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.MigrateTables(Migrations, m_Log);
            }
        }

        public void Remove(UUID scopeID, UUID accountID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM friends WHERE UserID LIKE ?id OR FriendID LIKE ?id", connection))
                {
                    cmd.Parameters.AddParameter("?id", accountID);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            foreach(string avatarnameservicename in m_AvatarNameServiceNames)
            {
                m_AvatarNameServices.Add(loader.GetService<AvatarNameServiceInterface>(avatarnameservicename));
            }
        }

        public override void Store(FriendInfo fi)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsideTransaction(delegate ()
                {
                    Dictionary<string, object> vals = new Dictionary<string, object>();
                    vals.Add("UserID", fi.User.ID);
                    vals.Add("FriendID", fi.Friend.ID);
                    vals.Add("Secret", fi.Secret);
                    vals.Add("RightsToFriend", fi.FriendGivenFlags);

                    connection.ReplaceInto("friends", vals);

                    vals.Add("UserID", fi.Friend.ID);
                    vals.Add("FriendID", fi.User.ID);
                    vals.Add("Secret", fi.Secret);
                    vals.Add("RightsToFriend", fi.UserGivenFlags);
                    connection.ReplaceInto("friends", vals);
                });
            }
        }

        public override void StoreOffer(FriendInfo fi)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                Dictionary<string, object> vals = new Dictionary<string, object>();
                vals.Add("UserID", fi.Friend.ID);
                vals.Add("FriendID", fi.User.ID);
                vals.Add("Secret", fi.Secret);
                vals.Add("RightsToFriend", FriendRightFlags.None);
                connection.ReplaceInto("friends", vals);
            }
        }

        public override void StoreRights(FriendInfo fi)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("UPDATE friends SET RightsToFriend = ?rights WHERE UserID LIKE ?userid AND FriendID LIKE ?friendid", connection))
                {
                    cmd.Parameters.AddParameter("?rights", fi.FriendGivenFlags);
                    cmd.Parameters.AddParameter("?userid", fi.User.ID);
                    cmd.Parameters.AddParameter("?friendid", fi.Friend.ID);
                    if(cmd.ExecuteNonQuery() < 1)
                    {
                        throw new FriendUpdateFailedException();
                    }
                }
            }
        }

        public override bool TryGetValue(UUI user, UUI friend, out FriendInfo fInfo)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand(m_InnerJoinSelectFull + "WHERE A.UserID LIKE ?userid AND A.FriendID LIKE ?friendid", connection))
                {
                    cmd.Parameters.AddParameter("?userid", user.ID);
                    cmd.Parameters.AddParameter("?friendid", friend.ID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            fInfo = reader.ToFriendInfo();
                            return true;
                        }
                    }
                }
            }
            fInfo = new FriendInfo();
            return false;
        }

        public void VerifyConnection()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("friends"),
            new AddColumn<UUID>("UserID") { IsNullAllowed = false },
            new AddColumn<UUID>("FriendID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("Secret") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<FriendRightFlags>("RightsToFriend") { IsNullAllowed = false },
            new PrimaryKeyInfo("UserID", "FriendID"),
            new NamedKeyInfo("PrincipalIndex", "UserID") { IsUnique = false },
            new NamedKeyInfo("FriendIndex", "FriendID") { IsUnique = false }
        };

    }

    #region Factory
    [PluginName("Friends")]
    public class MySQLFriendsServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL FRIENDS SERVICE");
        public MySQLFriendsServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLFriendsService(MySQLUtilities.BuildConnectionString(ownSection, m_Log),
                ownSection.GetString("AvatarNameServices", string.Empty));
        }
    }
    #endregion

}
