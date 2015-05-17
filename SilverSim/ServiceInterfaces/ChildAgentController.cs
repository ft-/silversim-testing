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
