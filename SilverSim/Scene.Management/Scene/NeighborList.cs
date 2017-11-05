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

        private uint LowerPos(uint a, uint drawdist) => (a < drawdist) ? 0 : a - drawdist;

        private bool IsPointInBox(GridVector pos, GridVector a, GridVector b, uint drawdist) =>
            pos.X >= LowerPos(a.X, drawdist) && pos.X < b.X + drawdist && pos.Y >= LowerPos(a.Y, drawdist) && pos.Y < b.Y + drawdist;

        public List<RegionInfo> GetNeighbors(RegionInfo thisRegion, uint drawdistance)
        {
            var neighbors = new List<RegionInfo>();
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
                        if ((thisRegion.Location.Y >= otherRegion.Location.Y && thisRegion.Location.Y < otherExtentPos.Y) ||
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
                        GridVector aa;
                        GridVector bb;

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
            OnNeighborAddOrUpdate?.Invoke(region);
        }

        public void Remove(RegionInfo region)
        {
            Remove(region.ID);
            OnNeighborRemove?.Invoke(region);
        }
    }
}
