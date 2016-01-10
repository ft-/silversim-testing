// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.MuteList
{
    [UDPMessage(MessageType.UpdateMuteListEntry)]
    [Reliable]
    [NotTrusted]
    public class UpdateMuteListEntry : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID MuteID;
        public string MuteName;
        public Int32 MuteType;
        public UInt32 MuteFlags;

        public UpdateMuteListEntry()
        {

        }

        public static UpdateMuteListEntry Decode(UDPPacket p)
        {
            UpdateMuteListEntry m = new UpdateMuteListEntry();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.MuteID = p.ReadUUID();
            m.MuteName = p.ReadStringLen8();
            m.MuteType = p.ReadInt32();
            m.MuteFlags = p.ReadUInt32();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(MuteID);
            p.WriteStringLen8(MuteName);
            p.WriteInt32(MuteType);
            p.WriteUInt32(MuteFlags);
        }
    }
}
