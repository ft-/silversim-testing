﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.LL.Messages;
using SilverSim.LL.Messages.IM;
using SilverSim.Types.IM;

namespace SilverSim.LL.Core
{
    public partial class LLAgent
    {
        [IMMessageHandler(GridInstantMessageDialog.MessageFromAgent)]
        [IMMessageHandler(GridInstantMessageDialog.StartTyping)]
        [IMMessageHandler(GridInstantMessageDialog.StopTyping)]
        [IMMessageHandler(GridInstantMessageDialog.BusyAutoResponse)]
        public void HandleIM(LLAgent nop, Circuit circuit, Message m)
        {
            GridInstantMessage im = (GridInstantMessage)(ImprovedInstantMessage)m;
            im.IsFromGroup = false;
            im.FromAgent.ID = m_AgentID;

            im.OnResult = circuit.OnIMResult;

            LLUDPServer server = circuit.Server;
            if (server != null)
            {
                server.RouteIM(im);
            }
        }
    }
}
