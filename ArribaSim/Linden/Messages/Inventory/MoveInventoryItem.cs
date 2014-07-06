/*

ArribaSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using ArribaSim.Types;
using System.Collections.Generic;

namespace ArribaSim.Linden.Messages.Inventory
{
    public class MoveInventoryItem : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public bool Stamp;
        public struct InventoryDataEntry
        {
            public UUID ItemID;
            public UUID FolderID;
            public string NewName;
        }
        public List<InventoryDataEntry> InventoryData = new List<InventoryDataEntry>();

        public MoveInventoryItem()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.MoveInventoryItem;
            }
        }

        public static Message Decode(UDPPacket p)
        {
            MoveInventoryItem m = new MoveInventoryItem();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.Stamp = p.ReadBoolean();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                InventoryDataEntry d = new InventoryDataEntry();
                d.ItemID = p.ReadUUID();
                d.FolderID = p.ReadUUID();
                d.NewName = p.ReadStringLen8();
                m.InventoryData.Add(d);
            }

            return m;
        }
    }
}
