// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ForceObjectSelect)]
    [Reliable]
    [NotTrusted]
    public class ForceObjectSelect : Message
    {
        public bool ResetList;
        public List<uint> LocalIDs = new List<uint>();

        public ForceObjectSelect()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteBoolean(ResetList);
            p.WriteUInt8((byte)LocalIDs.Count);
            foreach (uint d in LocalIDs)
            {
                p.WriteUInt32(d);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            ForceObjectSelect m = new ForceObjectSelect();
            m.ResetList = p.ReadBoolean();
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                m.LocalIDs.Add(p.ReadUInt32());
            }
            return m;
        }
    }
}
