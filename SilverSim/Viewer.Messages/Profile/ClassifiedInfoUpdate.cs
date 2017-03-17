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

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(ClassifiedID);
            p.WriteInt32(Category);
            p.WriteStringLen8(Name);
            p.WriteStringLen16(Description);
            p.WriteUUID(ParcelID);
            p.WriteInt32(ParentEstate);
            p.WriteUUID(SnapshotID);
            p.WriteVector3d(PosGlobal);
            p.WriteUInt8(ClassifiedFlags);
            p.WriteInt32(PriceForListing);
        }
    }
}
