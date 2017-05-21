// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelBuy)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ParcelBuy : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UUID GroupID;
        public bool IsGroupOwned;
        public bool RemoveContribution;
        public Int32 LocalID;
        public bool IsFinal;
        public Int32 Price;
        public Int32 Area;

        public static Message Decode(UDPPacket p) => new ParcelBuy()
        {
            AgentID = p.ReadUUID(),
            SessionID = p.ReadUUID(),

            GroupID = p.ReadUUID(),
            IsGroupOwned = p.ReadBoolean(),
            RemoveContribution = p.ReadBoolean(),
            LocalID = p.ReadInt32(),
            IsFinal = p.ReadBoolean(),
            Price = p.ReadInt32(),
            Area = p.ReadInt32()
        };

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(GroupID);
            p.WriteBoolean(IsGroupOwned);
            p.WriteBoolean(RemoveContribution);
            p.WriteInt32(LocalID);
            p.WriteBoolean(IsFinal);
            p.WriteInt32(Price);
            p.WriteInt32(Area);
        }
    }
}
