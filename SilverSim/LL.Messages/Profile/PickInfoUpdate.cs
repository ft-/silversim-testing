// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Profile
{
    [UDPMessage(MessageType.PickInfoUpdate)]
    [Reliable]
    [NotTrusted]
    public class PickInfoUpdate : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID PickID;
        public UUID CreatorID;
        public bool TopPick;
        public UUID ParcelID;
        public string Name;
        public string Description;
        public UUID SnapshotID;
        public Vector3 PosGlobal;
        public Int32 SortOrder;
        public bool IsEnabled;

        public PickInfoUpdate()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            PickInfoUpdate m = new PickInfoUpdate();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.PickID = p.ReadUUID();
            m.CreatorID = p.ReadUUID();
            m.TopPick = p.ReadBoolean();
            m.ParcelID = p.ReadUUID();
            m.Name = p.ReadStringLen8();
            m.Description = p.ReadStringLen16();
            m.SnapshotID = p.ReadUUID();
            m.PosGlobal = p.ReadVector3d();
            m.SortOrder = p.ReadInt32();
            m.IsEnabled = p.ReadBoolean();

            return m;
        }
    }
}
