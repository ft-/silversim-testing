﻿/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Types;
using SilverSim.Types.Grid;
using System;

namespace SilverSim.LL.Messages.Chat
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
            p.WriteMessageType(Number);
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
