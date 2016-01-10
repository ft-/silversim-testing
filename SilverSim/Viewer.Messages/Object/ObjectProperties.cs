// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectProperties)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class ObjectProperties : Message
    {
        public List<byte[]> ObjectData = new List<byte[]>();

        public ObjectProperties()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt8((byte)ObjectData.Count);
            foreach(byte[] d in ObjectData)
            {
                p.WriteBytes(d);
            }
        }
    }
}
