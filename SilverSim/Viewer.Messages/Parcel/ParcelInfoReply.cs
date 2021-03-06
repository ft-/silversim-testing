﻿// SilverSim is distributed under the terms of the
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

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelInfoReply)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class ParcelInfoReply : Message
    {
        public UUID AgentID;

        public ParcelID ParcelID;
        public UUID OwnerID;
        public string Name;
        public string Description;
        public int ActualArea;
        public int BillableArea;
        public byte Flags;
        public Vector3 GlobalPosition = new Vector3();
        public string SimName;
        public UUID SnapshotID;
        public double Dwell;
        public int SalePrice;
        public uint AuctionID;

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteBytes(ParcelID.GetBytes());
            p.WriteUUID(OwnerID);
            p.WriteStringLen8(Name);
            p.WriteStringLen8(Description);
            p.WriteInt32(ActualArea);
            p.WriteInt32(BillableArea);
            p.WriteUInt8(Flags);
            p.WriteVector3f(GlobalPosition);
            p.WriteStringLen8(SimName);
            p.WriteUUID(SnapshotID);
            p.WriteFloat((float)Dwell);
            p.WriteInt32(SalePrice);
            p.WriteUInt32(AuctionID);
        }

        public static Message Decode(UDPPacket p) => new ParcelInfoReply
        {
            AgentID = p.ReadUUID(),
            ParcelID = new ParcelID(p.ReadBytes(16), 0),
            OwnerID = p.ReadUUID(),
            Name = p.ReadStringLen8(),
            Description = p.ReadStringLen8(),
            ActualArea = p.ReadInt32(),
            BillableArea = p.ReadInt32(),
            Flags = p.ReadUInt8(),
            GlobalPosition = p.ReadVector3f(),
            SimName = p.ReadStringLen8(),
            SnapshotID = p.ReadUUID(),
            Dwell = p.ReadFloat(),
            SalePrice = p.ReadInt32(),
            AuctionID = p.ReadUInt32()
        };
    }
}
