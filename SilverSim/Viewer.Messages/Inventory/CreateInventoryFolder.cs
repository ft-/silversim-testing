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
using SilverSim.Types.Inventory;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.CreateInventoryFolder)]
    [Reliable]
    [NotTrusted]
    public class CreateInventoryFolder : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID FolderID;
        public UUID ParentFolderID;
        public InventoryType FolderType;
        public string FolderName;

        public CreateInventoryFolder()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            CreateInventoryFolder m = new CreateInventoryFolder();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            m.FolderID = p.ReadUUID();
            m.ParentFolderID = p.ReadUUID();
            m.FolderType = (InventoryType)p.ReadInt8();
            m.FolderName = p.ReadStringLen8();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(FolderID);
            p.WriteUUID(ParentFolderID);
            p.WriteInt8((sbyte)FolderType);
            p.WriteStringLen8(FolderName);
        }
    }
}
