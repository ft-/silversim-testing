// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.TaskInventory
{
    [UDPMessage(MessageType.RemoveTaskInventory)]
    [Reliable]
    [NotTrusted]
    public class RemoveTaskInventory : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UInt32 LocalID;
        public UUID ItemID;

        public RemoveTaskInventory()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            RemoveTaskInventory m = new RemoveTaskInventory();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.LocalID = p.ReadUInt32();
            m.ItemID = p.ReadUUID();

            return m;
        }
    }
}
