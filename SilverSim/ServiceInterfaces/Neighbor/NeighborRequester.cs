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
            var southWestCorner = ownRegion.Location;
            var northEastCorner = ownRegion.Location + ownRegion.Size;

            var southWestViewCorner = ownRegion.Location;
            var northEastViewCorner = ownRegion.Location + ownRegion.Size;

            southWestViewCorner.X -= MAXIMUM_VIEW_RANGE;
            southWestViewCorner.Y -= MAXIMUM_VIEW_RANGE;
            northEastViewCorner.X += MAXIMUM_VIEW_RANGE;
            northEastViewCorner.Y += MAXIMUM_VIEW_RANGE;

            southWestCorner.X -= MAXIMUM_REGION_SIZE;
            southWestCorner.Y -= MAXIMUM_REGION_SIZE;
            northEastCorner.X += MAXIMUM_VIEW_RANGE;
            northEastCorner.Y += MAXIMUM_VIEW_RANGE;

            var northEastNeighborCorner = neighborRegion.Location + neighborRegion.Size;

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
            var southWestCorner = ownRegion.Location;
            var northEastCorner = ownRegion.Location + ownRegion.Size;

            var southWestViewCorner = ownRegion.Location;
            var northEastViewCorner = ownRegion.Location + ownRegion.Size;

            southWestViewCorner.X -= MAXIMUM_VIEW_RANGE;
            southWestViewCorner.Y -= MAXIMUM_VIEW_RANGE;
            northEastViewCorner.X += MAXIMUM_VIEW_RANGE;
            northEastViewCorner.Y += MAXIMUM_VIEW_RANGE;

            southWestCorner.X -= MAXIMUM_REGION_SIZE;
            southWestCorner.Y -= MAXIMUM_REGION_SIZE;
            northEastCorner.X += MAXIMUM_VIEW_RANGE;
            northEastCorner.Y += MAXIMUM_VIEW_RANGE;

            var regions = gridService.GetRegionsByRange(ownRegion.ScopeID, southWestCorner, northEastCorner);
            var actualNeighbors = new List<RegionInfo>();

            foreach(var ri in regions)
            {
                var northEastNeighborCorner = ri.Location + ri.Size;
                if (ownRegion.ID == ri.ID)
                {
                    /* skip we are not our own neighbor */
                }
                // The r.RegionFlags == null check only needs to be made for simulators before 2015-01-14 (pre 0.8.1).
                else if ((ri.Flags & RegionFlags.RegionOnline) == 0)
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
