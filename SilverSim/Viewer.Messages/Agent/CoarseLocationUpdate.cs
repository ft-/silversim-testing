// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Agent
{
    [UDPMessage(MessageType.CoarseLocationUpdate)]
    [Trusted]
    public class CoarseLocationUpdate : Message
    {
        public Int16 You;
        public Int16 Prey;

        public struct AgentDataEntry
        {
            public byte X;
            public byte Y;
            public byte Z;
            public UUID AgentID;
        }
        public List<AgentDataEntry> AgentData = new List<AgentDataEntry>();

        public CoarseLocationUpdate()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUInt8((byte)AgentData.Count);
            foreach (AgentDataEntry d in AgentData)
            {
                p.WriteUInt8(d.X);
                p.WriteUInt8(d.Y);
                p.WriteUInt8(d.Z);
            }
            p.WriteInt16(You);
            p.WriteInt16(Prey);
            p.WriteUInt8((byte)AgentData.Count);
            foreach (AgentDataEntry d in AgentData)
            {
                p.WriteUUID(d.AgentID);
            }
        }
    }
}
