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
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SilverSim.Database.MySQL.Grid
{
    #region Service Implementation
    [Description("Memory Grid Backend")]
    [ServerParam("DeleteOnUnregister")]
    [ServerParam("AllowDuplicateRegionNames")]
    public sealed class MemoryGridService : GridServiceInterface, IPlugin, IServerParamListener
    {
        private bool IsDeleteOnUnregister;
        private bool AllowDuplicateRegionNames;
        readonly RwLockedDictionary<UUID, RegionInfo> m_Data = new RwLockedDictionary<UUID, RegionInfo>();
        readonly bool m_UseRegionDefaultServices;
        List<RegionDefaultFlagsServiceInterface> m_RegionDefaultServices;

        public void TriggerParameterUpdated(UUID regionid, string parameter, string value)
        {
            if(regionid == UUID.Zero)
            {
                switch(parameter)
                {
                    case "DeleteOnUnregister":
                        IsDeleteOnUnregister = bool.Parse(value);
                        break;

                    case "AllowDuplicateRegionNames":
                        AllowDuplicateRegionNames = bool.Parse(value);
                        break;

                    default:
                        break;
                }
            }
            
        }

        #region Constructor
        public MemoryGridService(IConfig ownSection)
        {
            m_UseRegionDefaultServices = ownSection.GetBoolean("UseRegionDefaultServices", false);
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_RegionDefaultServices = loader.GetServicesByValue<RegionDefaultFlagsServiceInterface>();
        }
        #endregion

        #region Accessors
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override RegionInfo this[UUID scopeID, UUID regionID]
        {
            get
            {
                RegionInfo rInfo;
                if(!TryGetValue(scopeID, regionID, out rInfo))
                {
                    throw new KeyNotFoundException();
                }
                return rInfo;
            }
        }

        public override bool TryGetValue(UUID scopeID, UUID regionID, out RegionInfo rInfo)
        {
            if(m_Data.TryGetValue(regionID, out rInfo) && rInfo.ScopeID == scopeID)
            {
                rInfo = new RegionInfo(rInfo);
                return true;
            }
            return false;
        }

        public override bool ContainsKey(UUID scopeID, UUID regionID)
        {
            RegionInfo rInfo;
            return m_Data.TryGetValue(regionID, out rInfo) && (scopeID == UUID.Zero || scopeID == rInfo.ScopeID);
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override RegionInfo this[UUID scopeID, uint gridX, uint gridY]
        {
            get
            {
                RegionInfo rInfo;
                if(!TryGetValue(scopeID, gridX, gridY, out rInfo))
                {
                    throw new KeyNotFoundException();
                }
                return rInfo;
            }
        }

        public override bool TryGetValue(UUID scopeID, uint gridX, uint gridY, out RegionInfo rInfo)
        {
            var res = from region in m_Data.Values
                                          where region.Location.X <= gridX && region.Location.Y <= gridY &&
             region.Location.X + region.Size.X > gridX && region.Location.Y + region.Size.Y > gridY &&
             (scopeID == UUID.Zero || scopeID == region.ScopeID)
                                          select region;
            foreach(RegionInfo regionInfo in res)
            {
                rInfo = new RegionInfo(regionInfo);
                return true;
            }

            rInfo = default(RegionInfo);
            return false;
        }

        public override bool ContainsKey(UUID scopeID, uint gridX, uint gridY)
        {
            var res = from region in m_Data.Values
                                          where region.Location.X <= gridX && region.Location.Y <= gridY &&
             region.Location.X + region.Size.X > gridX && region.Location.Y + region.Size.Y > gridY &&
             (scopeID == UUID.Zero || scopeID == region.ScopeID)
                                          select true;
            foreach (bool f in res)
            {
                return true;
            }

            return false;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override RegionInfo this[UUID scopeID, string regionName]
        {
            get
            {
                RegionInfo rInfo;
                if(!TryGetValue(scopeID, regionName, out rInfo))
                {
                    throw new KeyNotFoundException();
                }
                return rInfo;
            }
        }

        public override bool TryGetValue(UUID scopeID, string regionName, out RegionInfo rInfo)
        {
            var res = from region in m_Data.Values
                                          where region.Name.ToLower().Equals(regionName.ToLower()) &&
             (scopeID == UUID.Zero || scopeID == region.ScopeID)
                                          select region;
            foreach (RegionInfo regionInfo in res)
            {
                rInfo = new RegionInfo(regionInfo);
                return true;
            }

            rInfo = default(RegionInfo);
            return false;
        }

        public override bool ContainsKey(UUID scopeID, string regionName)
        {
            var res = from region in m_Data.Values
                                          where region.Name.ToLower().Equals(regionName.ToLower()) &&
             (scopeID == UUID.Zero || scopeID == region.ScopeID)
                                          select true;
            foreach (bool f in res)
            {
                return true;
            }

            return false;
        }

        public override RegionInfo this[UUID regionID]
        {
            get
            {
                RegionInfo rInfo;
                if(!TryGetValue(regionID, out rInfo))
                {
                    throw new KeyNotFoundException();
                }
                return rInfo;
            }
        }

        public override bool TryGetValue(UUID regionID, out RegionInfo rInfo) => m_Data.TryGetValue(regionID, out rInfo);

        public override bool ContainsKey(UUID regionID) => m_Data.ContainsKey(regionID);
        #endregion

        #region Region Registration
        public override void AddRegionFlags(UUID regionID, RegionFlags setflags)
        {
            RegionInfo rInfo;
            if(m_Data.TryGetValue(regionID, out rInfo))
            {
                rInfo.Flags |= setflags;
            }
        }

        public override void RemoveRegionFlags(UUID regionID, RegionFlags removeflags)
        {
            RegionInfo rInfo;
            if (m_Data.TryGetValue(regionID, out rInfo))
            {
                rInfo.Flags &= ~removeflags;
            }
        }

        public override void RegisterRegion(RegionInfo regionInfo)
        {
            foreach (var service in m_RegionDefaultServices)
            {
                regionInfo.Flags |= service.GetRegionDefaultFlags(regionInfo.ID);
            }

            RegionInfo oldRegion;
            if (!AllowDuplicateRegionNames && TryGetValue(regionInfo.ScopeID, regionInfo.Name, out oldRegion) && oldRegion.ID != regionInfo.ID)
            {
                throw new GridRegionUpdateFailedException("Duplicate region name");
            }

            /* we have to give checks for all intersection variants */
            var res = from region in m_Data.Values
                                    where ((region.Location.X >= regionInfo.Location.X && region.Location.Y >= regionInfo.Location.Y &&
                                            region.Location.X < regionInfo.Location.X + regionInfo.Size.X &&
                                            region.Location.Y < regionInfo.Location.Y + regionInfo.Size.Y) ||
                                            (region.Location.X + region.Size.X > regionInfo.Location.X &&
                                           region.Location.Y + region.Size.Y > regionInfo.Location.Y &&
                                           region.Location.X + region.Size.X < regionInfo.Location.X + regionInfo.Size.X &&
                                           region.Location.Y + region.Size.Y < regionInfo.Location.Y + regionInfo.Size.Y)) &&
                                           region.ID != regionInfo.ID && region.ScopeID == regionInfo.ScopeID
                                    select true;

            foreach(bool f in res)
            {
                throw new GridRegionUpdateFailedException("Overlapping regions");
            }

            m_Data[regionInfo.ID] = regionInfo;
        }

        public override void UnregisterRegion(UUID scopeID, UUID regionID)
        {
            if(IsDeleteOnUnregister)
            {
                /* we handoff most stuff to mysql here */
                /* first line deletes only when region is not persistent */
                m_Data.RemoveIf(regionID, delegate (RegionInfo regInfo) { return (scopeID == UUID.Zero || regInfo.ScopeID == scopeID) && (regInfo.Flags & RegionFlags.Persistent) == 0; });

                /* second step is to set it offline when it is persistent */
            }

            RegionInfo rInfo;
            if(m_Data.TryGetValue(regionID, out rInfo) && (scopeID == UUID.Zero || rInfo.ScopeID == scopeID))
            {
                rInfo.Flags &= ~RegionFlags.RegionOnline;
            }
        }

        public override void DeleteRegion(UUID scopeID, UUID regionID)
        {
            m_Data.RemoveIf(regionID, delegate (RegionInfo rInfo) { return scopeID == UUID.Zero || rInfo.ScopeID == scopeID; });
        }

        #endregion

        #region List accessors
        List<RegionInfo> GetRegionsByFlag(UUID scopeID, RegionFlags flags)
        {
            var res = from region in m_Data.Values where (scopeID == UUID.Zero || region.ScopeID == scopeID) && (region.Flags & flags) != 0 select new RegionInfo(region);
            return new List<RegionInfo>(res);
        }

        public override List<RegionInfo> GetHyperlinks(UUID scopeID) => GetRegionsByFlag(scopeID, RegionFlags.Hyperlink);

        public override List<RegionInfo> GetDefaultRegions(UUID scopeID) => GetRegionsByFlag(scopeID, RegionFlags.DefaultRegion);

        public override List<RegionInfo> GetOnlineRegions(UUID scopeID) => GetRegionsByFlag(scopeID, RegionFlags.RegionOnline);

        public override List<RegionInfo> GetOnlineRegions() => GetRegionsByFlag(UUID.Zero, RegionFlags.RegionOnline);

        public override List<RegionInfo> GetFallbackRegions(UUID scopeID) => GetRegionsByFlag(scopeID, RegionFlags.FallbackRegion);

        public override List<RegionInfo> GetDefaultHypergridRegions(UUID scopeID) => GetRegionsByFlag(scopeID, RegionFlags.DefaultHGRegion);

        public override List<RegionInfo> GetRegionsByRange(UUID scopeID, GridVector min, GridVector max)
        {
            var res = from region in m_Data.Values
                                          where 
                                          ((region.Location.X >= min.X && region.Location.Y >= min.Y && region.Location.X <= max.X && region.Location.Y <= max.Y) ||
                                          (region.Location.X + region.Size.X >= min.X && region.Location.Y + region.Size.Y >= min.Y && region.Location.X + region.Size.X <= max.X && region.Location.Y + region.Size.Y <= max.Y) ||
                                          (region.Location.X >= min.X && region.Location.Y >= min.Y && region.Location.X + region.Size.X > min.Y && region.Location.Y + region.Size.Y > min.Y) ||
                                          (region.Location.X >= max.X && region.Location.Y >= max.Y && region.Location.X + region.Size.X > max.X && region.Location.Y + region.Size.Y > max.Y)
                                          )
                                          &&
             (scopeID == UUID.Zero || scopeID == region.ScopeID)
                                          select new RegionInfo(region);


            return new List<RegionInfo>(res);
        }

        public override List<RegionInfo> GetNeighbours(UUID scopeID, UUID regionID)
        {
            RegionInfo ri = this[scopeID, regionID];
            var res = from region in m_Data.Values
                                          where
                                          (
                                          ((region.Location.X == ri.Location.X + ri.Size.X || region.Location.X + region.Size.X == ri.Location.X) &&
                                          (region.Location.Y <= ri.Location.Y + ri.Size.Y && region.Location.Y + region.Size.Y >= ri.Location.Y)) ||
                                          ((region.Location.Y == ri.Location.Y + ri.Size.Y || region.Location.Y + region.Size.Y == ri.Location.Y) &&
                                          (region.Location.X <= ri.Location.X + ri.Size.X && region.Location.X + region.Size.X >= ri.Location.X)))
                                          &&
             (scopeID == UUID.Zero || scopeID == region.ScopeID)
                                          select new RegionInfo(region);

            return new List<RegionInfo>(res);
        }

        public override List<RegionInfo> GetAllRegions(UUID scopeID) =>
            new List<RegionInfo>(from region in m_Data.Values where scopeID == UUID.Zero || region.ScopeID == scopeID select new RegionInfo(region));

        public override List<RegionInfo> SearchRegionsByName(UUID scopeID, string searchString) =>
            new List<RegionInfo>(from region in m_Data.Values where (scopeID == UUID.Zero || region.ScopeID == scopeID) && region.Name.ToLower().StartsWith(searchString.ToLower()) select new RegionInfo(region));

        public override Dictionary<string, string> GetGridExtraFeatures() =>
            new Dictionary<string, string>();

        #endregion

    }
    #endregion

    #region Factory
    [PluginName("Grid")]
    public class MemoryGridServiceFactory : IPluginFactory
    {
        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection) =>
            new MemoryGridService(ownSection);
    }
    #endregion

}
