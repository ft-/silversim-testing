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
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Friends;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Database.Memory.Friends
{
    [Description("Memory Friends Backend")]
    [PluginName("Friends")]
    public class MemoryFriendsService : FriendsServiceInterface, IPlugin
    {
        private class FriendData
        {
            public FriendRightFlags Rights;
            public string Secret;

            public FriendData(FriendRightFlags flags, string secret)
            {
                Rights = flags;
                Secret = secret;
            }
        }

        private readonly object m_TransactionLock = new object();

        private readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, FriendData>> m_Friends = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, FriendData>>(() => new RwLockedDictionary<UUID, FriendData>());

        private readonly string[] m_AvatarNameServiceNames;
        private AggregatingAvatarNameService m_AvatarNameService;

        public MemoryFriendsService(IConfig ownSection)
        {
            m_AvatarNameServiceNames = ownSection.GetString("AvatarNameServices", string.Empty).Split(',');
        }

        public void ResolveUUI(FriendInfo fi)
        {
            UUI uui;
            if (!fi.Friend.IsAuthoritative &&
                m_AvatarNameService.TryGetValue(fi.Friend, out uui))
            {
                fi.Friend = uui;
            }
            if (!fi.User.IsAuthoritative &&
                m_AvatarNameService.TryGetValue(fi.User, out uui))
            {
                fi.User = uui;
            }
        }

        public override List<FriendInfo> this[UUI user]
        {
            get
            {
                var friends = new List<FriendInfo>();

                RwLockedDictionary<UUID, FriendData> friendList;
                if(!m_Friends.TryGetValue(user.ID, out friendList))
                {
                    return friends;
                }

                foreach(var kvp in friendList)
                {
                    var fi = new FriendInfo();
                    RwLockedDictionary<UUID, FriendData> otherFriendList;
                    FriendData otherFriendData;
                    if (m_Friends.TryGetValue(kvp.Key, out otherFriendList) &&
                        otherFriendList.TryGetValue(user.ID, out otherFriendData))
                    {
                        fi.UserGivenFlags = otherFriendData.Rights;
                    }
                    fi.FriendGivenFlags = kvp.Value.Rights;
                    fi.Secret = kvp.Value.Secret;
                    fi.Friend.ID = kvp.Key;
                    fi.User = user;
                    ResolveUUI(fi);
                    friends.Add(fi);
                }
                return friends;
            }
        }

        public override FriendInfo this[UUI user, UUI friend]
        {
            get
            {
                FriendInfo fi;
                if(!TryGetValue(user, friend, out fi))
                {
                    throw new KeyNotFoundException();
                }
                return fi;
            }
        }

        public override void Delete(FriendInfo fi)
        {
            RwLockedDictionary<UUID, FriendData> friendList;
            lock (m_TransactionLock)
            {
                if (m_Friends.TryGetValue(fi.User.ID, out friendList))
                {
                    friendList.Remove(fi.Friend.ID);
                }
                if (m_Friends.TryGetValue(fi.Friend.ID, out friendList))
                {
                    friendList.Remove(fi.User.ID);
                }
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            var avatarNameServices = new RwLockedList<AvatarNameServiceInterface>();
            foreach (string avatarnameservicename in m_AvatarNameServiceNames)
            {
                avatarNameServices.Add(loader.GetService<AvatarNameServiceInterface>(avatarnameservicename.Trim()));
            }
            m_AvatarNameService = new AggregatingAvatarNameService(avatarNameServices);
        }

        public override void Store(FriendInfo fi)
        {
            lock(m_TransactionLock)
            {
                m_Friends[fi.User.ID][fi.Friend.ID] = new FriendData(fi.FriendGivenFlags, fi.Secret);
                m_Friends[fi.Friend.ID][fi.User.ID] = new FriendData(fi.UserGivenFlags, fi.Secret);
            }
        }

        public override void StoreOffer(FriendInfo fi)
        {
            var data = new FriendData(FriendRightFlags.None, fi.Secret);
            RwLockedDictionary<UUID, FriendData> friendList;
            if(m_Friends.TryGetValue(fi.Friend.ID, out friendList) && !m_Friends.ContainsKey(fi.User.ID))
            {
                m_Friends[fi.Friend.ID].Add(fi.User.ID, data);
            }
        }

        public override void StoreRights(FriendInfo fi)
        {
            RwLockedDictionary<UUID, FriendData> friendList;
            FriendData data;
            if(m_Friends.TryGetValue(fi.User.ID, out friendList) && friendList.TryGetValue(fi.Friend.ID, out data))
            {
                data.Rights = fi.FriendGivenFlags;
            }
        }

        public override bool TryGetValue(UUI user, UUI friend, out FriendInfo fInfo)
        {
            RwLockedDictionary<UUID, FriendData> friendList;
            FriendData data;
            fInfo = null;
            if(m_Friends.TryGetValue(user.ID, out friendList) && friendList.TryGetValue(friend.ID, out data))
            {
                fInfo = new FriendInfo()
                {
                    Secret = data.Secret,
                    User = user,
                    Friend = friend,
                    FriendGivenFlags = data.Rights
                };
                if (m_Friends.TryGetValue(friend.ID, out friendList) && friendList.TryGetValue(user.ID, out data))
                {
                    fInfo.UserGivenFlags = data.Rights;
                }
                ResolveUUI(fInfo);
                return true;
            }
            return false;
        }
    }
}
