// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.TaskInventory
{
    [UDPMessage(MessageType.MoveTaskInventory)]
    [Reliable]
    [NotTrusted]
    public class MoveTaskInventory : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID FolderID;
        public UInt32 LocalID;
        public UUID ItemID;

        public MoveTaskInventory()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            MoveTaskInventory m = new MoveTaskInventory();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.FolderID = p.ReadUUID();
            m.LocalID = p.ReadUInt32();
            m.ItemID = p.ReadUUID();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(FolderID);
            p.WriteUInt32(LocalID);
            p.WriteUUID(ItemID);
        }
    }
}
