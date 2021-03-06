﻿// SilverSim is distributed under the terms of the
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

using SilverSim.ServiceInterfaces.Parcel;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;
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
        #region Accessors
        public RegionInfo this[UUID regionID]
        {
            get
            {
                RegionInfo ri;
                if(!TryGetValue(regionID, out ri))
                {
                    throw new GridRegionNotFoundException();
                }
                return ri;
            }
        }

        public abstract bool TryGetValue(UUID regionID, out RegionInfo rInfo);
        public abstract bool ContainsKey(UUID regionID);

        public RegionInfo this[GridVector position] => this[position.X, position.Y];

        public bool TryGetValue(GridVector position, out RegionInfo rInfo) => TryGetValue(position.X, position.Y, out rInfo);

        public bool ContainsKey(GridVector position) => ContainsKey(position.X, position.Y);

        public RegionInfo this[uint gridX, uint gridY]
        {
            get
            {
                RegionInfo ri;
                if(!TryGetValue(gridX, gridY, out ri))
                {
                    throw new GridRegionNotFoundException();
                }
                return ri;
            }
        }

        public abstract bool TryGetValue(uint gridX, uint gridY, out RegionInfo rInfo);
        public abstract bool ContainsKey(uint gridX, uint gridY);

        public RegionInfo this[string regionName]
        {
            get
            {
                RegionInfo ri;
                if(!TryGetValue(regionName, out ri))
                {
                    throw new GridRegionNotFoundException();
                }
                return ri;
            }
        }

        public abstract bool TryGetValue(string regionName, out RegionInfo rInfo);
        public abstract bool ContainsKey(string regionName);

        #endregion

        #region Region Registration
        public abstract void RegisterRegion(RegionInfo regionInfo);
        public virtual void RegisterRegion(RegionInfo regionInfo, bool keepOnlineUnmodified)
        {
            if(keepOnlineUnmodified) /* if set, means that a re-register will keep the flag */
            {
                throw new NotSupportedException(nameof(keepOnlineUnmodified));
            }
            RegisterRegion(regionInfo);
        }
        public abstract void UnregisterRegion(UUID regionID);
        public abstract void DeleteRegion(UUID regionID);
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
        public abstract List<RegionInfo> GetHyperlinks();
        public abstract List<RegionInfo> GetDefaultRegions();
        public abstract List<RegionInfo> GetFallbackRegions();
        public abstract List<RegionInfo> GetDefaultIntergridRegions();
        public abstract List<RegionInfo> GetRegionsByRange(GridVector min, GridVector max);
        public abstract List<RegionInfo> GetNeighbours(UUID regionID);
        public abstract List<RegionInfo> GetAllRegions();
        public abstract List<RegionInfo> GetOnlineRegions();
        public abstract Dictionary<string, string> GetGridExtraFeatures();

        public abstract List<RegionInfo> SearchRegionsByName(string searchString);
        #endregion

        public virtual IRemoteParcelServiceInterface RemoteParcelService => new NoRemoteParcelService();
    }
}
