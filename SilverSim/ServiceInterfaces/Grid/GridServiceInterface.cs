// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Grid
{
    [Serializable]
    public class GridRegionUpdateFailedException : Exception
    {
        public GridRegionUpdateFailedException()
        {

        }
        public GridRegionUpdateFailedException(string message)
            : base(message)
        {

        }
    }

    [Serializable]
    public class GridRegionNotFoundException : Exception
    {
        public GridRegionNotFoundException()
        {

        }
        public GridRegionNotFoundException(string message)
            : base(message)
        {

        }
    }

    [Serializable]
    public class GridServiceInaccessibleException : Exception
    {
        public GridServiceInaccessibleException()
        {

        }
    }

    public abstract class GridServiceInterface
    {
        #region Constructor
        public GridServiceInterface()
        {

        }
        #endregion

        #region Accessors
        public abstract RegionInfo this[UUID scopeID, UUID regionID]
        {
            get;
        }

        public RegionInfo this[UUID scopeID, GridVector position]
        {
            get
            {
                return this[scopeID, position.X, position.Y];
            }
        }

        public abstract RegionInfo this[UUID scopeID, uint gridX, uint gridY]
        {
            get;
        }

        public abstract RegionInfo this[UUID scopeID, string regionName]
        {
            get;
        }

        public abstract RegionInfo this[UUID regionID]
        {
            get;
        }

        #endregion

        #region Region Registration
        public abstract void RegisterRegion(RegionInfo regionInfo);
        public abstract void UnregisterRegion(UUID scopeID, UUID regionID);
        public abstract void DeleteRegion(UUID scopeID, UUID regionID);
        #endregion

        #region List accessors
        public abstract List<RegionInfo> GetDefaultRegions(UUID scopeID);
        public abstract List<RegionInfo> GetFallbackRegions(UUID scopeID);
        public abstract List<RegionInfo> GetDefaultHypergridRegions(UUID scopeID);
        public abstract List<RegionInfo> GetRegionsByRange(UUID scopeID, GridVector min, GridVector max);
        public abstract List<RegionInfo> GetNeighbours(UUID scopeID, UUID regionID);
        public abstract List<RegionInfo> GetAllRegions(UUID scopeID);
        public abstract List<RegionInfo> GetOnlineRegions(UUID scopeID);
        public abstract List<RegionInfo> GetOnlineRegions();
        public abstract Dictionary<string, string> GetGridExtraFeatures();

        public abstract List<RegionInfo> SearchRegionsByName(UUID scopeID, string searchString);
        #endregion
    }
}
