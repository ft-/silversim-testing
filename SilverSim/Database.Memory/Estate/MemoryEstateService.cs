// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Threading;
using SilverSim.Types.Estate;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SilverSim.Database.Memory.Estate
{
    #region Service Implementation
    [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    [Description("Memory Estate Backend")]
    public sealed partial class MemoryEstateService : EstateServiceInterface, IPlugin
    {
        readonly RwLockedDictionary<uint, EstateInfo> m_Data = new RwLockedDictionary<uint, EstateInfo>();

        #region Constructor
        public MemoryEstateService()
        {
        }

        public void Startup(ConfigurationLoader loader)
        {
        }
        #endregion

        public override bool TryGetValue(uint estateID, out EstateInfo estateInfo)
        {
            EstateInfo intern;
            if(m_Data.TryGetValue(estateID, out intern))
            {
                lock(intern)
                {
                    estateInfo = new EstateInfo(intern);
                }
                return true;
            }
            estateInfo = default(EstateInfo);
            return false;
        }

        public override bool TryGetValue(string estateName, out EstateInfo estateInfo)
        {
            foreach(EstateInfo intern in m_Data.Values)
            {
                if(intern.Name.ToLower() == estateName.ToLower())
                {
                    estateInfo = new EstateInfo(intern);
                    return true;
                }
            }
            estateInfo = default(EstateInfo);
            return false;
        }

        public override bool ContainsKey(uint estateID)
        {
            return m_Data.ContainsKey(estateID);
        }

        public override bool ContainsKey(string estateName)
        {
            foreach (EstateInfo intern in m_Data.Values)
            {
                if (intern.Name.ToLower() == estateName.ToLower())
                {
                    return true;
                }
            }
            return false;
        }

        public override void Add(EstateInfo estateInfo)
        {
            m_Data.Add(estateInfo.ID, new EstateInfo(estateInfo));
        }

        public override EstateInfo this[uint estateID]
        {
            get
            {
                EstateInfo info;
                if(TryGetValue(estateID, out info))
                {
                    return info;
                }
                throw new KeyNotFoundException();
            }
            set
            {
                if (value != null)
                {
                    m_Data[estateID] = new EstateInfo(value);
                }
                else
                {
                    m_Data.Remove(estateID);
                }
            }
        }

        public override List<EstateInfo> All
        {
            get 
            {
                return new List<EstateInfo>(from estate in m_Data.Values where true select new EstateInfo(estate));
            }
        }

        public override List<uint> AllIDs
        {
            get 
            {
                return new List<uint>(m_Data.Keys);
            }
        }

        public override IEstateManagerServiceInterface EstateManager
        {
            get
            {
                return this;
            }
        }

        public override IEstateOwnerServiceInterface EstateOwner
        {
            get 
            {
                return this;
            }
        }

        public override IEstateAccessServiceInterface EstateAccess
        {
            get 
            {
                return this;
            }
        }

        public override IEstateBanServiceInterface EstateBans
        {
            get
            {
                return this;
            }
        }

        public override IEstateGroupsServiceInterface EstateGroup
        {
            get 
            {
                return this;
            }
        }

        public override IEstateRegionMapServiceInterface RegionMap
        {
            get 
            {
                return this;
            }
        }
    }
    #endregion

    #region Factory
    [PluginName("Estate")]
    public class MemoryEstateServiceFactory : IPluginFactory
    {
        public MemoryEstateServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MemoryEstateService();
        }
    }
    #endregion

}
