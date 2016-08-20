// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Teleport;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        [PacketHandler(MessageType.TeleportCancel)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void HandleTeleportCancel(Message m)
        {
            TeleportCancel req = (TeleportCancel)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            m_Log.Warn("Implement TeleportCancel");
        }

        [PacketHandler(MessageType.TeleportLandmarkRequest)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void HandleTeleportLandmarkRequest(Message m)
        {
            TeleportLandmarkRequest req = (TeleportLandmarkRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            m_Log.Warn("Implement TeleportLandmarkRequest");
        }

        [PacketHandler(MessageType.TeleportLocationRequest)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void HandleTeleportLocationRequest(Message m)
        {
            TeleportLocationRequest req = (TeleportLocationRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            AgentCircuit circuit;
            if(Circuits.TryGetValue(req.CircuitSceneID, out circuit))
            {
                /* TODO: we need the specific local list for HG destinations */
                if(!TeleportTo(circuit.Scene, req.GridPosition, req.Position, req.LookAt, Types.Grid.TeleportFlags.ViaLocation))
                {
                    TeleportFailed failedmsg = new TeleportFailed();
                    failedmsg.AgentID = m_AgentID;
                    failedmsg.Reason = this.GetLanguageString(CurrentCulture, "TeleportNotPossibleToRegion", "Teleport to region not possible");
                    SendMessageAlways(failedmsg, req.CircuitSceneID);
                }
            }
        }

        [PacketHandler(MessageType.TeleportRequest)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void HandleTeleportRequest(Message m)
        {
            TeleportRequest req = (TeleportRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            AgentCircuit circuit;
            if (Circuits.TryGetValue(req.CircuitSceneID, out circuit))
            {
                /* TODO: we need the specific local list for HG destinations */
                if (!TeleportTo(circuit.Scene, req.RegionID, req.Position, req.LookAt, Types.Grid.TeleportFlags.ViaLocation))
                {
                    TeleportFailed failedmsg = new TeleportFailed();
                    failedmsg.AgentID = m_AgentID;
                    failedmsg.Reason = this.GetLanguageString(CurrentCulture, "TeleportNotPossibleToRegion", "Teleport to region not possible");
                    SendMessageAlways(failedmsg, req.CircuitSceneID);
                }
            }
        }
    }
}
