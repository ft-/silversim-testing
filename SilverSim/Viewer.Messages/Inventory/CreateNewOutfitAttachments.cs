// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.CreateNewOutfitAttachments)]
    [Reliable]
    [NotTrusted]
    public class CreateNewOutfitAttachments : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UUID NewFolderID;

        public struct ObjectDataEntry
        {
            public UUID OldItemID;
            public UUID OldFolderID;
        }

        public List<ObjectDataEntry> ObjectData = new List<ObjectDataEntry>();

        public CreateNewOutfitAttachments()
        {

        }

        public static CreateNewOutfitAttachments Decode(UDPPacket p)
        {
            CreateNewOutfitAttachments m = new CreateNewOutfitAttachments();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.NewFolderID = p.ReadUUID();
            uint cnt = p.ReadUInt8();
            for(uint i = 0; i < cnt; ++i)
            {
                ObjectDataEntry e = new ObjectDataEntry();
                e.OldItemID = p.ReadUUID();
                e.OldFolderID = p.ReadUUID();
                m.ObjectData.Add(e);
            }
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(NewFolderID);
            p.WriteUInt8((byte)ObjectData.Count);
            foreach(ObjectDataEntry e in ObjectData)
            {
                p.WriteUUID(e.OldItemID);
                p.WriteUUID(e.OldFolderID);
            }
        }
    }
}
