// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Profile
{
    [UDPMessage(MessageType.AvatarPicksReply)]
    [Reliable]
    [Trusted]
    public class AvatarPicksReply : Message
    {
        public UUID AgentID;
        public UUID TargetID;

        public struct PickData
        {
            public UUID PickID;
            public string Name;
        }

        public List<PickData> Data = new List<PickData>();

        public AvatarPicksReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(TargetID);
            p.WriteUInt8((byte)Data.Count);
            foreach (PickData d in Data)
            {
                p.WriteUUID(d.PickID);
                p.WriteStringLen8(d.Name);
            }
        }
    }
}
