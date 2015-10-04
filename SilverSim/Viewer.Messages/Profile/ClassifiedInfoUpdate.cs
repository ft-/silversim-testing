// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Profile
{
    [UDPMessage(MessageType.ClassifiedInfoUpdate)]
    [Reliable]
    [NotTrusted]
    public class ClassifiedInfoUpdate : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UUID ClassifiedID;
        public int Category;
        public string Name;
        public string Description;
        public UUID ParcelID;
        public int ParentEstate;
        public UUID SnapshotID;
        public Vector3 PosGlobal;
        public byte ClassifiedFlags;
        public int PriceForListing;

        public ClassifiedInfoUpdate()
        {

        }

        public static ClassifiedInfoUpdate Decode(UDPPacket p)
        {
            ClassifiedInfoUpdate m = new ClassifiedInfoUpdate();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ClassifiedID = p.ReadUUID();
            m.Category = p.ReadInt32();
            m.Name = p.ReadStringLen8();
            m.Description = p.ReadStringLen16();
            m.ParcelID = p.ReadUUID();
            m.ParentEstate = p.ReadInt32();
            m.SnapshotID = p.ReadUUID();
            m.PosGlobal = p.ReadVector3d();
            m.ClassifiedFlags = p.ReadUInt8();
            m.PriceForListing = p.ReadInt32();
            return m;
        }
    }
}
