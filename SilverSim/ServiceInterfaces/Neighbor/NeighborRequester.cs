// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Neighbor
{
    public static class NeighborRequester
    {
        const int MAXIMUM_VIEW_RANGE = 1024;
        const int MAXIMUM_REGION_SIZE = 8192;

        public static bool IsNeighbor(this RegionInfo ownRegion, RegionInfo neighborRegion)
        {
            GridVector southWestCorner = ownRegion.Location;
            GridVector northEastCorner = ownRegion.Location + ownRegion.Size;

            GridVector southWestViewCorner = ownRegion.Location;
            GridVector northEastViewCorner = ownRegion.Location + ownRegion.Size;

            southWestViewCorner.X -= MAXIMUM_VIEW_RANGE;
            southWestViewCorner.Y -= MAXIMUM_VIEW_RANGE;
            northEastViewCorner.X += MAXIMUM_VIEW_RANGE;
            northEastViewCorner.Y += MAXIMUM_VIEW_RANGE;

            southWestCorner.X -= MAXIMUM_REGION_SIZE;
            southWestCorner.Y -= MAXIMUM_REGION_SIZE;
            northEastCorner.X += MAXIMUM_VIEW_RANGE;
            northEastCorner.Y += MAXIMUM_VIEW_RANGE;

            GridVector northEastNeighborCorner = neighborRegion.Location + neighborRegion.Size;

            if (ownRegion.ID == neighborRegion.ID)
            {
                /* skip we are not our own neighbor */
            }
            // The r.RegionFlags == null check only needs to be made for simulators before 2015-01-14 (pre 0.8.1).
            else if ((neighborRegion.Flags & RegionFlags.RegionOnline) != 0)
            {
                /* skip offline regions */
            }
            else if (northEastNeighborCorner.X < southWestViewCorner.X ||
                northEastNeighborCorner.Y < southWestViewCorner.Y ||
                neighborRegion.Location.X > northEastViewCorner.X ||
                neighborRegion.Location.Y > northEastViewCorner.Y)
            {
                /* not a neighbour at all */
            }
            else
            {
                return true;
            }
            return false;
        }

        public static List<RegionInfo> GetNeighbors(this GridServiceInterface gridService, RegionInfo ownRegion)
        {
            GridVector southWestCorner = ownRegion.Location;
            GridVector northEastCorner = ownRegion.Location + ownRegion.Size;

            GridVector southWestViewCorner = ownRegion.Location;
            GridVector northEastViewCorner = ownRegion.Location + ownRegion.Size;

            southWestViewCorner.X -= MAXIMUM_VIEW_RANGE;
            southWestViewCorner.Y -= MAXIMUM_VIEW_RANGE;
            northEastViewCorner.X += MAXIMUM_VIEW_RANGE;
            northEastViewCorner.Y += MAXIMUM_VIEW_RANGE;

            southWestCorner.X -= MAXIMUM_REGION_SIZE;
            southWestCorner.Y -= MAXIMUM_REGION_SIZE;
            northEastCorner.X += MAXIMUM_VIEW_RANGE;
            northEastCorner.Y += MAXIMUM_VIEW_RANGE;

            List<RegionInfo> regions = gridService.GetRegionsByRange(ownRegion.ScopeID, southWestCorner, northEastCorner);
            List<RegionInfo> actualNeighbors = new List<RegionInfo>();

            foreach(RegionInfo ri in regions)
            {
                GridVector northEastNeighborCorner = ri.Location + ri.Size;
                if (ownRegion.ID == ri.ID)
                {
                    /* skip we are not our own neighbor */
                }
                // The r.RegionFlags == null check only needs to be made for simulators before 2015-01-14 (pre 0.8.1).
                else if ((ri.Flags & RegionFlags.RegionOnline) != 0)
                {
                    /* skip offline regions */
                }
                else if (northEastNeighborCorner.X < southWestViewCorner.X ||
                    northEastNeighborCorner.Y < southWestViewCorner.Y ||
                    ri.Location.X > northEastViewCorner.X ||
                    ri.Location.Y > northEastViewCorner.Y)
                {
                    /* not a neighbour at all */
                }
                else
                {
                    actualNeighbors.Add(ri);
                }
            }

            return actualNeighbors;
        }
    }
}
