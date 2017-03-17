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
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.GridUser;
using System.ComponentModel;

namespace SilverSim.Database.Memory.GridUser
{
    #region Service Implementation
    [Description("Memory GridUser Backend")]
    public sealed class MemoryGridUserService : GridUserServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        readonly RwLockedDictionary<UUID, GridUserInfo> m_Data = new RwLockedDictionary<UUID, GridUserInfo>();

        #region Constructor
        public MemoryGridUserService()
        {
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* nothing to do */
        }
        #endregion

        #region GridUserServiceInterface
        public override bool TryGetValue(UUID userID, out GridUserInfo userInfo)
        {
            GridUserInfo internaldata;
            if(m_Data.TryGetValue(userID, out internaldata))
            {
                lock(internaldata)
                {
                    userInfo = new GridUserInfo(internaldata);
                }
                return true;
            }
            userInfo = default(GridUserInfo);
            return false;
        }

        public override GridUserInfo this[UUID userID]
        {
            get 
            {
                GridUserInfo info;
                if(TryGetValue(userID, out info))
                {
                    return info;
                }
                throw new GridUserNotFoundException();
            }
        }


        public override bool TryGetValue(UUI userID, out GridUserInfo gridUserInfo)
        {
            return TryGetValue(userID.ID, out gridUserInfo);
        }

        public override GridUserInfo this[UUI userID]
        {
            get 
            {
                return this[userID.ID];
            }
        }

        public override void LoggedInAdd(UUI userID)
        {
            GridUserInfo info;
            if(m_Data.TryGetValue(userID.ID, out info))
            {
                info.LastLogin = Date.Now;
                info.IsOnline = true;
            }
            else
            {
                info = new GridUserInfo();
                info.User = userID;
                info.LastLogin = Date.Now;
                info.IsOnline = true;
                m_Data[userID.ID] = info;
            }
        }

        public override void LoggedIn(UUI userID)
        {
            GridUserInfo info;
            if (m_Data.TryGetValue(userID.ID, out info))
            {
                lock(info)
                {
                    info.LastLogin = Date.Now;
                    info.IsOnline = true;
                }
            }
        }

        public override void LoggedOut(UUI userID, UUID lastRegionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
            GridUserInfo info;
            if (m_Data.TryGetValue(userID.ID, out info))
            {
                lock(info)
                {
                    info.LastLogout = Date.Now;
                    info.LastRegionID = lastRegionID;
                    info.LastPosition = lastPosition;
                    info.LastLookAt = lastLookAt;
                    info.IsOnline = false;
                }
            }
        }

        public override void SetHome(UUI userID, UUID homeRegionID, Vector3 homePosition, Vector3 homeLookAt)
        {
            GridUserInfo info;
            if (m_Data.TryGetValue(userID.ID, out info))
            {
                lock(info)
                {
                    info.HomeRegionID = homeRegionID;
                    info.HomePosition = homePosition;
                    info.HomeLookAt = homeLookAt;
                }
            }
        }

        public override void SetPosition(UUI userID, UUID lastRegionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
            GridUserInfo info;
            if (m_Data.TryGetValue(userID.ID, out info))
            {
                lock (info)
                {
                    info.LastRegionID = lastRegionID;
                    info.LastPosition = lastPosition;
                    info.LastLookAt = lastLookAt;
                }
            }
        }
        #endregion

        public void Remove(UUID scopeID, UUID userAccount)
        {
            m_Data.Remove(userAccount);
        }
    }
    #endregion

    #region Factory
    [PluginName("GridUser")]
    public class MemoryGridUserServiceFactory : IPluginFactory
    {
        public MemoryGridUserServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MemoryGridUserService();
        }
    }
    #endregion

}
