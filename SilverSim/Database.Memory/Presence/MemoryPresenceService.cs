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
        public void Startup(ConfigurationLoader loader)
        {
            /* nothing to do */
        }
        #endregion

        #region PresenceServiceInterface
        public override List<PresenceInfo> GetPresencesInRegion(UUID regionId) =>
            new List<PresenceInfo>(from presence in m_Data.Values where presence.RegionID == regionId select presence);

        public override List<PresenceInfo> this[UUID userID] =>
            new List<PresenceInfo>(from presence in m_Data.Values where presence.UserID.ID == userID select presence);

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
                    var pInfo = new PresenceInfo(value);
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
            foreach (UUID sessionid in new List<UUID>(from presence in m_Data where presence.Value.RegionID == regionID select presence.Key))
            {
                m_Data.Remove(sessionid);
            }
        }
        #endregion

        public override void Remove(UUID scopeID, UUID userAccount)
        {
            foreach(UUID sessionid in new List<UUID>(from presence in m_Data where presence.Value.UserID.ID == userAccount select presence.Key))
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
        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection) =>
            new MemoryPresenceService();
    }
    #endregion
}
