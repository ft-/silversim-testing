/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

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

        public static List<RegionInfo> GetNeighbors(GridServiceInterface gridService, RegionInfo ownRegion)
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
