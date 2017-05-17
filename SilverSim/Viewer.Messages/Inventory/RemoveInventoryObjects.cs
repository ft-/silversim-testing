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
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.RemoveInventoryObjects)]
    [Reliable]
    [NotTrusted]
    public class RemoveInventoryObjects : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public List<UUID> FolderIDs = new List<UUID>();
        public List<UUID> ItemIDs = new List<UUID>();

        public static Message Decode(UDPPacket p)
        {
            var m = new RemoveInventoryObjects()
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID()
            };
            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.FolderIDs.Add(p.ReadUUID());
            }

            c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.ItemIDs.Add(p.ReadUUID());
            }

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt8((byte)FolderIDs.Count);
            foreach(var id in FolderIDs)
            {
                p.WriteUUID(id);
            }
            p.WriteUInt8((byte)ItemIDs.Count);
            foreach(var id in ItemIDs)
            {
                p.WriteUUID(id);
            }
        }
    }
}
