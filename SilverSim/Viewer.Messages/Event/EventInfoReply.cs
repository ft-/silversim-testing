// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Event
{
    [UDPMessage(MessageType.EventInfoReply)]
    [Reliable]
    [Trusted]
    public class EventInfoReply : Message
    {
        public UUID AgentID;
        public UInt32 EventID;
        public string Creator;
        public string Name;
        public string Category;
        public string Desc;
        public string Date;
        public UInt32 DateUTC;
        public UInt32 Duration;
        public UInt32 Cover;
        public UInt32 Amount;
        public string SimName;
        public Vector3 GlobalPos;
        public UInt32 EventFlags;

        public EventInfoReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUInt32(EventID);
            p.WriteStringLen8(Creator);
            p.WriteStringLen8(Name);
            p.WriteStringLen8(Category);
            p.WriteStringLen16(Desc);
            p.WriteStringLen8(Date);
            p.WriteUInt32(DateUTC);
            p.WriteUInt32(Duration);
            p.WriteUInt32(Cover);
            p.WriteUInt32(Amount);
            p.WriteStringLen8(SimName);
            p.WriteVector3d(GlobalPos);
            p.WriteUInt32(EventFlags);
        }

        public static Message Decode(UDPPacket p)
        {
            EventInfoReply m = new EventInfoReply();
            m.AgentID = p.ReadUUID();
            m.EventID = p.ReadUInt32();
            m.Creator = p.ReadStringLen8();
            m.Name = p.ReadStringLen8();
            m.Category = p.ReadStringLen8();
            m.Desc = p.ReadStringLen16();
            m.Date = p.ReadStringLen8();
            m.DateUTC = p.ReadUInt32();
            m.Duration = p.ReadUInt32();
            m.Cover = p.ReadUInt32();
            m.Amount = p.ReadUInt32();
            m.SimName = p.ReadStringLen8();
            m.GlobalPos = p.ReadVector3d();
            m.EventFlags = p.ReadUInt32();
            return m;
        }
    }
}
