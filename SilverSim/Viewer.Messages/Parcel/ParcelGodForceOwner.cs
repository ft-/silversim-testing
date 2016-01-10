// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelGodForceOwner)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ParcelGodForceOwner : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID OwnerID;
        public Int32 LocalID;

        public ParcelGodForceOwner()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ParcelGodForceOwner m = new ParcelGodForceOwner();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.OwnerID = p.ReadUUID();
            m.LocalID = p.ReadInt32();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(OwnerID);
            p.WriteInt32(LocalID);
        }
    }
}
