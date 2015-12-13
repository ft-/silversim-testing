// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.Parcel;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Parcel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        [PacketHandler(MessageType.ParcelDwellRequest)]
        public void HandleParcelDwellRequest(Message m)
        {
            ParcelDwellRequest req = (ParcelDwellRequest)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            ParcelInfo pInfo;
            if (Scene.Parcels.TryGetValue(req.LocalID, out pInfo))
            {
                ParcelDwellReply reply = new ParcelDwellReply();
                reply.AgentID = req.AgentID;
                reply.LocalID = req.LocalID;
                reply.ParcelID = pInfo.ID;
                reply.Dwell = 0;
                SendMessage(reply);
            }
        }

        [PacketHandler(MessageType.ParcelAccessListRequest)]
        public void HandleParcelAccessListRequest(Message m)
        {
            ParcelAccessListRequest req = (ParcelAccessListRequest)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            ParcelInfo pInfo;
            if (Scene.Parcels.TryGetValue(req.LocalID, out pInfo))
            {
                ParcelAccessListReply reply = new ParcelAccessListReply();
                reply.AgentID = req.AgentID;
                reply.SequenceID = req.SequenceID;
                reply.Flags = req.Flags;
                reply.LocalID = req.LocalID;

#warning add parcel access list
                SendMessage(reply);
            }
        }
    }
}
