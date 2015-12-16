// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

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

        protected GridRegionUpdateFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public GridRegionUpdateFailedException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    [Serializable]
    public class GridRegionNotFoundException : KeyNotFoundException
    {
        public GridRegionNotFoundException()
        {

        }

        public GridRegionNotFoundException(string message)
            : base(message)
        {

        }

        protected GridRegionNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public GridRegionNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    [Serializable]
    public class GridServiceInaccessibleException : Exception
    {
        public GridServiceInaccessibleException()
        {

        }

        public GridServiceInaccessibleException(string message)
            : base(message)
        {

        }

        protected GridServiceInaccessibleException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public GridServiceInaccessibleException(string message, Exception innerException)
            : base(message, innerException)
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
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract RegionInfo this[UUID scopeID, UUID regionID]
        {
            get;
        }

        public abstract bool TryGetValue(UUID scopeID, UUID regionID, out RegionInfo rInfo);

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public RegionInfo this[UUID scopeID, GridVector position]
        {
            get
            {
                return this[scopeID, position.X, position.Y];
            }
        }

        public bool TryGetValue(UUID scopeID, GridVector position, out RegionInfo rInfo)
        {
            return TryGetValue(scopeID, position.X, position.Y, out rInfo);
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract RegionInfo this[UUID scopeID, uint gridX, uint gridY]
        {
            get;
        }

        public abstract bool TryGetValue(UUID scopeID, uint gridX, uint gridY, out RegionInfo rInfo);

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract RegionInfo this[UUID scopeID, string regionName]
        {
            get;
        }

        public abstract bool TryGetValue(UUID scopeID, string regionName, out RegionInfo rInfo);

        public abstract RegionInfo this[UUID regionID]
        {
            get;
        }

        public abstract bool TryGetValue(UUID regionID, out RegionInfo rInfo);

        #endregion

        #region Region Registration
        public abstract void RegisterRegion(RegionInfo regionInfo);
        public abstract void UnregisterRegion(UUID scopeID, UUID regionID);
        public abstract void DeleteRegion(UUID scopeID, UUID regionID);
        public virtual void AddRegionFlags(UUID regionID, RegionFlags setflags)
        {
            throw new NotSupportedException();
        }
        public virtual void RemoveRegionFlags(UUID regionID, RegionFlags removeflags)
        {
            throw new NotSupportedException();
        }
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
