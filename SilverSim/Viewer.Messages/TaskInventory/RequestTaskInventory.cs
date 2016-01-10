// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.TaskInventory
{
    [UDPMessage(MessageType.RequestTaskInventory)]
    [Reliable]
    [NotTrusted]
    public class RequestTaskInventory : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UInt32 LocalID;

        public RequestTaskInventory()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            RequestTaskInventory m = new RequestTaskInventory();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.LocalID = p.ReadUInt32();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt32(LocalID);
        }
    }
}
