// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Management.Scene
{
    public class NeighborList : RwLockedDictionary<UUID, RegionInfo>
    {
        public event Action<RegionInfo> OnNeighborAddOrUpdate;
        public Action<RegionInfo> OnNeighborRemove;

        public NeighborList()
        {

        }

        uint LowerPos(uint a, uint drawdist)
        {
            return (a < drawdist) ? 0 : a - drawdist;
        }

        bool IsPointInBox(GridVector pos, GridVector a, GridVector b, uint drawdist)
        {
            return (pos.X >= LowerPos(a.X, drawdist) && pos.X < b.X + drawdist && pos.Y >= LowerPos(a.Y, drawdist) && pos.Y < b.Y + drawdist);
        }

        public List<RegionInfo> GetNeighbors(RegionInfo thisRegion, uint drawdistance)
        {
            List<RegionInfo> neighbors = new List<RegionInfo>();
            GridVector thisExtentPos = thisRegion.Location + thisRegion.Size;
            foreach(RegionInfo otherRegion in Values)
            {
                if(otherRegion.ID == thisRegion.ID && otherRegion.GridURI == thisRegion.GridURI)
                {
                    GridVector otherExtentPos = otherRegion.Location + otherRegion.Size;

                    /* the first two cases are direct neighbors */
                    if(thisRegion.Location.X == otherExtentPos.X || otherRegion.Location.X == thisExtentPos.X)
                    {
                        if((thisRegion.Location.Y >= otherRegion.Location.Y && thisRegion.Location.Y < otherExtentPos.Y) ||
                            (otherRegion.Location.Y >= thisRegion.Location.Y && otherRegion.Location.Y < thisExtentPos.Y))
                        {
                            neighbors.Add(otherRegion);
                        }
                    }
                    else if(thisRegion.Location.Y == otherExtentPos.Y || otherRegion.Location.Y == thisExtentPos.Y)
                    {
                        if (thisRegion.Location.Y >= otherRegion.Location.Y && thisRegion.Location.Y < otherExtentPos.Y ||
                            (otherRegion.Location.Y >= thisRegion.Location.Y && otherRegion.Location.Y < thisExtentPos.Y))
                        {
                            neighbors.Add(otherRegion);
                        }
                    }
                    else if (IsPointInBox(otherRegion.Location, thisRegion.Location, thisExtentPos, drawdistance) ||
                        IsPointInBox(otherExtentPos, thisRegion.Location, thisExtentPos, drawdistance))
                    {
                        neighbors.Add(otherRegion);
                    }
                    else
                    {
                        GridVector aa, bb;
                        aa = otherRegion.Location;
                        bb = otherExtentPos;
                        aa.Y = otherExtentPos.Y;
                        bb.Y = otherRegion.Location.Y;
                        if(IsPointInBox(aa, thisRegion.Location, thisExtentPos, drawdistance) ||
                            IsPointInBox(bb, thisRegion.Location, thisExtentPos, drawdistance))
                        {
                            neighbors.Add(otherRegion);
                        }
                    }
                }
            }

            return neighbors;
        }

        public void Add(RegionInfo region)
        {
            this[region.ID] = region;
            var ev = OnNeighborAddOrUpdate;
            if(ev != null)
            {
                foreach (Action<RegionInfo> del in ev.GetInvocationList())
                {
                    del(region);
                }
            }
        }

        public void Remove(RegionInfo region)
        {
            Remove(region.ID);
            var ev = OnNeighborRemove;
            if(ev != null)
            {
                foreach (Action<RegionInfo> del in ev.GetInvocationList())
                {
                    del(region);
                }
            }
        }
    }
}
