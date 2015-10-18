// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Types.Grid;
using SilverSim.Viewer.Messages;
using SilverSim.Scene.Types.Agent;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        [PacketHandler(MessageType.RegionHandleRequest)]
        internal void HandleRegionHandleRequest(Message m)
        {
            SilverSim.Viewer.Messages.Region.RegionHandleRequest req = (SilverSim.Viewer.Messages.Region.RegionHandleRequest)m;
            SilverSim.Viewer.Messages.Region.RegionIDAndHandleReply res;

            try
            {
                RegionInfo ri = GridService[RegionData.ScopeID, req.RegionID];
                res = new Viewer.Messages.Region.RegionIDAndHandleReply();
                res.RegionPosition = ri.Location;
                res.RegionID = req.RegionID;
                UDPServer.SendMessageToAgent(req.CircuitAgentID, res);
            }
            catch
            {

            }
        }
    }
}
