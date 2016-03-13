// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Teleport
{
    [UDPMessage(MessageType.StartLure)]
    [Reliable]
    [NotTrusted]
    public class StartLure : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public byte LureType;
        public string Message;
        public List<UUID> TargetData = new List<UUID>();
        

        public StartLure()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            StartLure m = new StartLure();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.LureType = p.ReadUInt8();
            m.Message = p.ReadStringLen8();
            uint count;
            count = p.ReadUInt8();
            for (uint i = 0; i < count; ++i)
            {
                m.TargetData.Add(p.ReadUUID());
            }

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt8(LureType);
            p.WriteStringLen8(Message);
            p.WriteUInt8((byte)TargetData.Count);
            foreach(UUID id in TargetData)
            {
                p.WriteUUID(id);
            }
        }
    }
}
