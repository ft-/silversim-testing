// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Friend
{
    [UDPMessage(MessageType.OfflineNotification)]
    [Reliable]
    [Trusted]
    public class OfflineNotification : Message
    {
        public List<UUID> AgentIDs = new List<UUID>();

        public OfflineNotification()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt8((byte)AgentIDs.Count);
            foreach(UUID id in AgentIDs)
            {
                p.WriteUUID(id);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            OfflineNotification m = new OfflineNotification();
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                m.AgentIDs.Add(p.ReadUUID());
            }
            return m;
        }
    }
}
