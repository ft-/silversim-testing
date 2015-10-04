// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Names
{
    [UDPMessage(MessageType.UUIDNameRequest)]
    [Reliable]
    [NotTrusted]
    public class UUIDNameRequest : Message
    {

        public List<UUID> UUIDNameBlock = new List<UUID>();

        public UUIDNameRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            UUIDNameRequest m = new UUIDNameRequest();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.UUIDNameBlock.Add(p.ReadUUID());
            }

            return m;
        }
    }
}
