// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Presence;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SilverSim.Database.Memory.Presence
{
    #region Service Implementation
    [Description("Memory Presence Backend")]
    public sealed class MemoryPresenceService : PresenceServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        readonly RwLockedDictionary<UUID, PresenceInfo> m_Data = new RwLockedDictionary<UUID, PresenceInfo>();

        #region Constructor
        public MemoryPresenceService()
        {
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* nothing to do */
        }
        #endregion

        #region PresenceServiceInterface
        public override List<PresenceInfo> GetPresencesInRegion(UUID regionId)
        {
            return new List<PresenceInfo>(from presence in m_Data.Values where presence.RegionID == regionId select presence);
        }

        public override List<PresenceInfo> this[UUID userID]
        {
            get
            {
                return new List<PresenceInfo>(from presence in m_Data.Values where presence.UserID.ID == userID select presence);
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override PresenceInfo this[UUID sessionID, UUID userID]
        {
            get
            {
                throw new NotSupportedException();
            }
            set /* setting null means logout, != null not allowed */
            {
                if(value != null)
                {
                    throw new ArgumentException("setting value != null is not allowed without reportType");
                }
                this[sessionID, userID, SetType.Report] = null;
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public override PresenceInfo this[UUID sessionID, UUID userID, PresenceServiceInterface.SetType reportType]
        { 
            /* setting null means logout, != null login message */
            set 
            {
                if (value == null)
                {
                    m_Data.Remove(sessionID);
                }
                else if (reportType == SetType.Login)
                {
                    PresenceInfo pInfo = new PresenceInfo(value);
                    pInfo.RegionID = UUID.Zero;
                    m_Data[sessionID] = pInfo;
                }
                else if (reportType == SetType.Report)
                {
                    PresenceInfo pInfo;
                    if(m_Data.TryGetValue(sessionID, out pInfo))
                    {
                        pInfo.RegionID = value.RegionID;
                    }
                }
                else
                {
                    throw new ArgumentException("Invalid reportType specified");
                }
            }
        }

        public override void LogoutRegion(UUID regionID)
        {
            List<UUID> sessionids = new List<UUID>(from presence in m_Data where presence.Value.RegionID == regionID select presence.Key);

            foreach (UUID sessionid in sessionids)
            {
                m_Data.Remove(sessionid);
            }
        }
        #endregion

        public override void Remove(UUID scopeID, UUID userAccount)
        {
            List<UUID> sessionids = new List<UUID>(from presence in m_Data where presence.Value.UserID.ID == userAccount select presence.Key);

            foreach(UUID sessionid in sessionids)
            {
                m_Data.Remove(sessionid);
            }
        }
    }
    #endregion

    #region Factory
    [PluginName("Presence")]
    public class MemoryPresenceServiceFactory : IPluginFactory
    {
        public MemoryPresenceServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MemoryPresenceService();
        }
    }
    #endregion

}
