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
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Search
{
    [UDPMessage(MessageType.PlacesReply)]
    [Reliable]
    [Trusted]
    public class PlacesReply : Message
    {
        public UUID AgentID;
        public UUID QueryID;
        public UUID TransactionID;
        public struct DataEntry
        {
            public UUID OwnerID;
            public string Name;
            public string Description;
            public Int32 ActualArea;
            public Int32 BillableArea;
            public byte Flags;
            public Vector3 GlobalPos;
            public string SimName;
            public UUID SnapshotID;
            public double Dwell;
            public Int32 Price;
        }

        public List<DataEntry> QueryData = new List<DataEntry>();

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(QueryID);
            p.WriteUUID(TransactionID);
            p.WriteUInt8((byte)QueryData.Count);
            foreach (var d in QueryData)
            {
                p.WriteUUID(d.OwnerID);
                p.WriteStringLen8(d.Name);
                p.WriteStringLen8(d.Description);
                p.WriteInt32(d.ActualArea);
                p.WriteInt32(d.BillableArea);
                p.WriteUInt8(d.Flags);
                p.WriteVector3f(d.GlobalPos);
                p.WriteStringLen8(d.SimName);
                p.WriteUUID(d.SnapshotID);
                p.WriteInt32(d.Price);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            var m = new PlacesReply()
            {
                AgentID = p.ReadUUID(),
                QueryID = p.ReadUUID(),
                TransactionID = p.ReadUUID()
            };
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                m.QueryData.Add(new DataEntry()
                {
                    OwnerID = p.ReadUUID(),
                    Name = p.ReadStringLen8(),
                    Description = p.ReadStringLen8(),
                    ActualArea = p.ReadInt32(),
                    BillableArea = p.ReadInt32(),
                    Flags = p.ReadUInt8(),
                    GlobalPos = p.ReadVector3f(),
                    SimName = p.ReadStringLen8(),
                    SnapshotID = p.ReadUUID(),
                    Price = p.ReadInt32()
                });
            }
            return m;
        }
    }
}
