// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Parcel;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelInfoReply)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class ParcelInfoReply : Message
    {
        public UUID AgentID;

        public UUID ParcelID;
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

        public ParcelInfoReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(ParcelID);
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

        public static Message Decode(UDPPacket p)
        {
            ParcelInfoReply m = new ParcelInfoReply();
            m.AgentID = p.ReadUUID();
            m.ParcelID = p.ReadUUID();
            m.OwnerID = p.ReadUUID();
            m.Name = p.ReadStringLen8();
            m.Description = p.ReadStringLen8();
            m.ActualArea = p.ReadInt32();
            m.BillableArea = p.ReadInt32();
            m.Flags = p.ReadUInt8();
            m.GlobalPosition = p.ReadVector3f();
            m.SimName = p.ReadStringLen8();
            m.SnapshotID = p.ReadUUID();
            m.Dwell = p.ReadFloat();
            m.SalePrice = p.ReadInt32();
            m.AuctionID = p.ReadUInt32();
            return m;
        }
    }
}
