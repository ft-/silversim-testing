/*

SilverSim is distributed under the terms of the
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

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Inventory
{
    [UDPMessage(MessageType.FetchInventory)]
    [Reliable]
    public class FetchInventory : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public struct InventoryDataEntry
        {
            public UUID OwnerID;
            public UUID ItemID;
        }
        public List<InventoryDataEntry> InventoryData = new List<InventoryDataEntry>();

        public FetchInventory()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            FetchInventory m = new FetchInventory();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            uint i;
            uint c = p.ReadUInt8();
            for (i = 0; i < c; ++i)
            {
                InventoryDataEntry d = new InventoryDataEntry();
                d.OwnerID = p.ReadUUID();
                d.ItemID = p.ReadUUID();
                m.InventoryData.Add(d);
            }

            return m;
        }
    }
}
