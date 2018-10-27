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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SilverSim.Database.MySQL.Grid
{
    [Description("Memory Grid Backend")]
    [PluginName("Grid")]
    [ServerParam("DeleteOnUnregister")]
    [ServerParam("AllowDuplicateRegionNames")]
    public sealed class MemoryGridService : GridServiceInterface, IPlugin, IServerParamListener
    {
        private bool IsDeleteOnUnregister;
        private bool AllowDuplicateRegionNames;
        private readonly RwLockedDictionary<UUID, RegionInfo> m_Data = new RwLockedDictionary<UUID, RegionInfo>();
        private readonly bool m_UseRegionDefaultServices;
        private List<RegionDefaultFlagsServiceInterface> m_RegionDefaultServices;

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
        public override bool TryGetValue(UUID regionID, out RegionInfo rInfo)
        {
            if(m_Data.TryGetValue(regionID, out rInfo))
            {
                rInfo = new RegionInfo(rInfo);
                return true;
            }
            return false;
        }

        public override bool ContainsKey(UUID regionID)
        {
            RegionInfo rInfo;
            return m_Data.TryGetValue(regionID, out rInfo);
        }

        public override bool TryGetValue(uint gridX, uint gridY, out RegionInfo rInfo)
        {
            var res = from region in m_Data.Values
                                          where region.Location.X <= gridX && region.Location.Y <= gridY &&
             region.Location.X + region.Size.X > gridX && region.Location.Y + region.Size.Y > gridY
                                          select region;
            foreach(RegionInfo regionInfo in res)
            {
                rInfo = new RegionInfo(regionInfo);
                return true;
            }

            rInfo = default(RegionInfo);
            return false;
        }

        public override bool ContainsKey(uint gridX, uint gridY)
        {
            var res = from region in m_Data.Values
                                          where region.Location.X <= gridX && region.Location.Y <= gridY &&
             region.Location.X + region.Size.X > gridX && region.Location.Y + region.Size.Y > gridY
                                          select true;
            foreach (bool f in res)
            {
                return true;
            }

            return false;
        }

        public override bool TryGetValue(string regionName, out RegionInfo rInfo)
        {
            var res = from region in m_Data.Values
                                          where region.Name.Equals(regionName, StringComparison.OrdinalIgnoreCase)
                                          select region;
            foreach (RegionInfo regionInfo in res)
            {
                rInfo = new RegionInfo(regionInfo);
                return true;
            }

            rInfo = default(RegionInfo);
            return false;
        }

        public override bool ContainsKey(string regionName)
        {
            var res = from region in m_Data.Values
                                          where region.Name.Equals(regionName, StringComparison.OrdinalIgnoreCase)
                                          select true;
            foreach (bool f in res)
            {
                return true;
            }

            return false;
        }
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
            RegisterRegion(regionInfo, false);
        }

        public override void RegisterRegion(RegionInfo regionInfo, bool keepOnlineUnmodified)
        {
            foreach (var service in m_RegionDefaultServices)
            {
                regionInfo.Flags |= service.GetRegionDefaultFlags(regionInfo.ID);
            }

            RegionInfo oldRegion = null;
            if (!AllowDuplicateRegionNames && TryGetValue(regionInfo.Name, out oldRegion) && oldRegion.ID != regionInfo.ID)
            {
                throw new GridRegionUpdateFailedException("Duplicate region name");
            }

            if (oldRegion != null && keepOnlineUnmodified)
            {
                regionInfo.Flags &= (~RegionFlags.RegionOnline);
                regionInfo.Flags |= (oldRegion.Flags & RegionFlags.RegionOnline);
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
                                           region.ID != regionInfo.ID
                                    select true;

            foreach(bool f in res)
            {
                throw new GridRegionUpdateFailedException("Overlapping regions");
            }

            m_Data[regionInfo.ID] = regionInfo;
        }

        public override void UnregisterRegion(UUID regionID)
        {
            if(IsDeleteOnUnregister)
            {
                /* first line deletes only when region is not persistent */
                m_Data.RemoveIf(regionID, (RegionInfo regInfo) => (regInfo.Flags & RegionFlags.Persistent) == 0);
                /* second step is to set it offline when it is persistent */
            }

            RegionInfo rInfo;
            if(m_Data.TryGetValue(regionID, out rInfo))
            {
                rInfo.Flags &= ~RegionFlags.RegionOnline;
            }
        }

        public override void DeleteRegion(UUID regionID)
        {
            m_Data.Remove(regionID);
        }

        #endregion

        #region List accessors
        private List<RegionInfo> GetRegionsByFlag(RegionFlags flags)
        {
            var res = from region in m_Data.Values where (region.Flags & flags) != 0 select new RegionInfo(region);
            return new List<RegionInfo>(res);
        }

        public override List<RegionInfo> GetHyperlinks() => GetRegionsByFlag(RegionFlags.Hyperlink);

        public override List<RegionInfo> GetDefaultRegions() => GetRegionsByFlag(RegionFlags.DefaultRegion);

        public override List<RegionInfo> GetOnlineRegions() => GetRegionsByFlag(RegionFlags.RegionOnline);

        public override List<RegionInfo> GetFallbackRegions() => GetRegionsByFlag(RegionFlags.FallbackRegion);

        public override List<RegionInfo> GetDefaultIntergridRegions() => GetRegionsByFlag(RegionFlags.DefaultIntergridRegion);

        public override List<RegionInfo> GetRegionsByRange(GridVector min, GridVector max)
        {
            var res = from region in m_Data.Values where
                                            region.Location.X + region.Size.X >= min.X && region.Location.X <= max.X && 
                                            region.Location.Y + region.Size.Y > min.Y && region.Location.Y <= max.Y
                                            select new RegionInfo(region);

            return new List<RegionInfo>(res);
        }

        public override List<RegionInfo> GetNeighbours(UUID regionID)
        {
            RegionInfo ri = this[regionID];
            var res = from region in m_Data.Values
                                          where
                                          (
                                          ((region.Location.X == ri.Location.X + ri.Size.X || region.Location.X + region.Size.X == ri.Location.X) &&
                                          (region.Location.Y <= ri.Location.Y + ri.Size.Y && region.Location.Y + region.Size.Y >= ri.Location.Y)) ||
                                          ((region.Location.Y == ri.Location.Y + ri.Size.Y || region.Location.Y + region.Size.Y == ri.Location.Y) &&
                                          (region.Location.X <= ri.Location.X + ri.Size.X && region.Location.X + region.Size.X >= ri.Location.X)))
                                          select new RegionInfo(region);

            return new List<RegionInfo>(res);
        }

        public override List<RegionInfo> GetAllRegions() =>
            new List<RegionInfo>(m_Data.Values);

        public override List<RegionInfo> SearchRegionsByName(string searchString) =>
            new List<RegionInfo>(from region in m_Data.Values where region.Name.StartsWith(searchString, StringComparison.OrdinalIgnoreCase) select new RegionInfo(region));

        public override Dictionary<string, string> GetGridExtraFeatures() =>
            new Dictionary<string, string>();

        #endregion

    }
}
