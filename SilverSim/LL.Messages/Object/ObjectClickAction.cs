// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using SilverSim.Types.Primitive;

namespace SilverSim.LL.Messages.Object
{
    [UDPMessage(MessageType.ObjectClickAction)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ObjectClickAction : Message
    {
        public struct Data
        {
            public UInt32 ObjectLocalID;
            public ClickActionType ClickAction;
        }

        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public List<Data> ObjectData = new List<Data>();

        public ObjectClickAction()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectClickAction m = new ObjectClickAction();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                Data d = new Data();
                d.ObjectLocalID = p.ReadUInt32();
                d.ClickAction = (ClickActionType)p.ReadUInt8();
                m.ObjectData.Add(d);
            }

            return m;
        }
    }
}
