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
using SilverSim.ServiceInterfaces.Neighbor;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;

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
            public ChildConnectionState State;
            public string ServerURI = string.Empty;
            public RegionInfo RegionInfo; /* only valid when Establish is set */
        }

        public static void LookupChildConnections(Dictionary<UInt64, ChildConnection> childList, GridServiceInterface gridService, RegionInfo ownRegion)
        {
            var ourNeighbors = NeighborRequester.GetNeighbors(gridService, ownRegion);
            var ourNeighborsDict = new Dictionary<ulong,RegionInfo>();

            /* find the regions that require childs */
            foreach(var ri in ourNeighbors)
            {
                ourNeighborsDict.Add(ri.Location.RegionHandle, ri);

                if(!childList.ContainsKey(ri.Location.RegionHandle))
                {
                    var childConn = new ChildConnection()
                    {
                        State = ChildConnectionState.Establish,
                        RegionInfo = ri,
                        ServerURI = ri.ServerURI
                    };
                    childList.Add(ri.Location.RegionHandle, childConn);
                }
            }

            /* find the childs to be removed */
            foreach(var kvp in childList)
            {
                if (!ourNeighborsDict.ContainsKey(kvp.Key))
                {
                    kvp.Value.State = ChildConnectionState.Close;
                }
            }
        }
    }
}
