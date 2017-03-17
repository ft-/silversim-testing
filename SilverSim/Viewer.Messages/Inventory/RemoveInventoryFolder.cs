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
using MapType = SilverSim.Types.Map;

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.RemoveInventoryFolder)]
    [Reliable]
    [NotTrusted]
    [EventQueueGet("RemoveInventoryFolder")]
    public class RemoveInventoryFolder : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public List<UUID> FolderData = new List<UUID>();

        public RemoveInventoryFolder()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            RemoveInventoryFolder m = new RemoveInventoryFolder();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.FolderData.Add(p.ReadUUID());
            }

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt8((byte)FolderData.Count);
            foreach(UUID folderid in FolderData)
            {
                p.WriteUUID(folderid);
            }
        }

        public override IValue SerializeEQG()
        {
            MapType llsd = new MapType();
            AnArray agentDataArray = new AnArray();
            MapType agentData = new MapType();
            agentData.Add("AgentID", AgentID);
            agentData.Add("SessionID", SessionID);
            agentDataArray.Add(agentData);
            llsd.Add("AgentData", agentDataArray);

            AnArray folderDataArray = new AnArray();

            foreach (UUID folder in FolderData)
            {
                MapType folderData = new MapType();
                folderData.Add("FolderID", folder);
                folderData.Add("AgentID", AgentID);
                folderDataArray.Add(folderData);
            }
            llsd.Add("FolderData", folderDataArray);

            return llsd;
        }
    }
}
