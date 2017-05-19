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
        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
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
            foreach(var intern in m_Data.Values)
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

        public override bool ContainsKey(uint estateID) => m_Data.ContainsKey(estateID);

        public override bool ContainsKey(string estateName)
        {
            foreach (var intern in m_Data.Values)
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

        public override List<EstateInfo> All => new List<EstateInfo>(from estate in m_Data.Values where true select new EstateInfo(estate));

        public override List<uint> AllIDs => new List<uint>(m_Data.Keys);

        public override IEstateManagerServiceInterface EstateManager => this;

        public override IEstateOwnerServiceInterface EstateOwner => this;

        public override IEstateAccessServiceInterface EstateAccess => this;

        public override IEstateBanServiceInterface EstateBans => this;

        public override IEstateGroupsServiceInterface EstateGroup => this;

        public override IEstateRegionMapServiceInterface RegionMap => this;
    }
    #endregion

    #region Factory
    [PluginName("Estate")]
    public class MemoryEstateServiceFactory : IPluginFactory
    {
        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection) =>
            new MemoryEstateService();
    }
    #endregion

}
