// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelPropertiesRequest)]
    [Reliable]
    [NotTrusted]
    public class ParcelPropertiesRequest : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public Int32 SequenceID = 0;
        public double West = 0;
        public double South = 0;
        public double East = 0;
        public double North = 0;
        public bool SnapSelection = false;

        public ParcelPropertiesRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ParcelPropertiesRequest m = new ParcelPropertiesRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.SequenceID = p.ReadInt32();
            m.West = p.ReadFloat();
            m.South = p.ReadFloat();
            m.East = p.ReadFloat();
            m.North = p.ReadFloat();
            m.SnapSelection = p.ReadBoolean();
            return m;
        }
    }
}
