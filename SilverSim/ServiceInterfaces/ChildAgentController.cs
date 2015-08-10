// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.Neighbor;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.ServiceInterfaces
{
    public static class ChildAgentController
    {
        public enum ChildConnectionState
        {
            Keep,
            Close,
            Establish
        }
        public class ChildConnection
        {
            public ChildConnectionState State = ChildConnectionState.Keep;
            public string ServerURI = string.Empty;
            public RegionInfo RegionInfo = null; /* only valid when Establish is set */

            public ChildConnection()
            {

            }
        }

        public static void LookupChildConnections(Dictionary<UInt64, ChildConnection> childList, GridServiceInterface gridService, RegionInfo ownRegion)
        {
            List<RegionInfo> ourNeighbors = NeighborRequester.GetNeighbors(gridService, ownRegion);
            Dictionary<UInt64, RegionInfo> ourNeighborsDict = new Dictionary<ulong,RegionInfo>();

            /* find the regions that require childs */
            foreach(RegionInfo ri in ourNeighbors)
            {
                ourNeighborsDict.Add(ri.Location.RegionHandle, ri);

                if(!childList.ContainsKey(ri.Location.RegionHandle))
                {
                    ChildConnection childConn = new ChildConnection();
                    childConn.State = ChildConnectionState.Establish;
                    childConn.RegionInfo = ri;
                    childConn.ServerURI = ri.ServerURI;
                    childList.Add(ri.Location.RegionHandle, childConn);
                }
            }

            /* find the childs to be removed */
            foreach(KeyValuePair<UInt64, ChildConnection> kvp in childList)
            {
                if (!ourNeighborsDict.ContainsKey(kvp.Key))
                {
                    kvp.Value.State = ChildConnectionState.Close;
                }
            }
        }
    }
}
