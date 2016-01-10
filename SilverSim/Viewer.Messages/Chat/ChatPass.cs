// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Grid;
using System;

namespace SilverSim.Viewer.Messages.Chat
{
    [UDPMessage(MessageType.ChatPass)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class ChatPass : Message
    {
        public Int32 Channel;
        public Vector3 Position;
        public UUID ID;
        public UUID OwnerID;
        public string Name;
        public ChatSourceType SourceType;
        public ChatType ChatType;
        public double Radius;
        public RegionAccess SimAccess;
        public string Message;


        public ChatPass()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteInt32(Channel);
            p.WriteVector3f(Position);
            p.WriteUUID(ID);
            p.WriteUUID(OwnerID);
            p.WriteStringLen8(Name);
            p.WriteUInt8((byte)SourceType);
            p.WriteUInt8((byte)ChatType);
            p.WriteFloat((float)Radius);
            p.WriteUInt8((byte)SimAccess);
            p.WriteStringLen16(Message);
        }

        public static ChatPass Decode(UDPPacket p)
        {
            ChatPass m = new ChatPass();
            m.Channel = p.ReadInt32();
            m.Position = p.ReadVector3f();
            m.ID = p.ReadUUID();
            m.OwnerID = p.ReadUUID();
            m.Name = p.ReadStringLen8();
            m.SourceType = (ChatSourceType)p.ReadUInt8();
            m.ChatType = (ChatType)p.ReadUInt8();
            m.Radius = p.ReadFloat();
            m.SimAccess = (RegionAccess)p.ReadUInt8();
            m.Message = p.ReadStringLen16();

            return m;
        }
    }
}
