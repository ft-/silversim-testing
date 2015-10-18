// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Profile
{
    [UDPMessage(MessageType.ClassifiedInfoReply)]
    [Reliable]
    [Trusted]
    public class ClassifiedInfoReply : Message
    {
        public UUID AgentID = UUID.Zero;

        public UUID ClassifiedID = UUID.Zero;
        public UUID CreatorID = UUID.Zero;
        public Date CreationDate = new Date();
        public Date ExpirationDate = new Date();
        public int Category;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public UUID ParcelID = UUID.Zero;
        public int ParentEstate;
        public UUID SnapshotID = UUID.Zero;
        public string SimName = string.Empty;
        public Vector3 PosGlobal = Vector3.Zero;
        public string ParcelName = string.Empty;
        public byte ClassifiedFlags;
        public int PriceForListing;

        public ClassifiedInfoReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(ClassifiedID);
            p.WriteUUID(CreatorID);
            p.WriteUInt32((uint)CreationDate.DateTimeToUnixTime());
            p.WriteUInt32((uint)ExpirationDate.DateTimeToUnixTime());
            p.WriteInt32(Category);
            p.WriteStringLen8(Name);
            p.WriteStringLen16(Description);
            p.WriteUUID(ParcelID);
            p.WriteInt32(ParentEstate);
            p.WriteUUID(SnapshotID);
            p.WriteStringLen8(SimName);
            p.WriteVector3d(PosGlobal);
            p.WriteStringLen8(ParcelName);
            p.WriteUInt8(ClassifiedFlags);
            p.WriteInt32(PriceForListing);
        }
    }
}
