// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Names
{
    [UDPMessage(MessageType.UUIDGroupNameRequest)]
    [Reliable]
    [NotTrusted]
    public class UUIDGroupNameRequest : Message
    {
        public List<UUID> UUIDNameBlock = new List<UUID>();

        public UUIDGroupNameRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            UUIDGroupNameRequest m = new UUIDGroupNameRequest();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.UUIDNameBlock.Add(p.ReadUUID());
            }

            return m;
        }
    }
}
