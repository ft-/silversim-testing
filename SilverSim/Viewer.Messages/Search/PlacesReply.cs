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

        public PlacesReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(QueryID);
            p.WriteUUID(TransactionID);
            p.WriteUInt8((byte)QueryData.Count);
            foreach (DataEntry d in QueryData)
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
            PlacesReply m = new PlacesReply();
            m.AgentID = p.ReadUUID();
            m.QueryID = p.ReadUUID();
            m.TransactionID = p.ReadUUID();
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                DataEntry d = new DataEntry();
                d.OwnerID = p.ReadUUID();
                d.Name = p.ReadStringLen8();
                d.Description = p.ReadStringLen8();
                d.ActualArea = p.ReadInt32();
                d.BillableArea = p.ReadInt32();
                d.Flags = p.ReadUInt8();
                d.GlobalPos = p.ReadVector3f();
                d.SimName = p.ReadStringLen8();
                d.SnapshotID = p.ReadUUID();
                d.Price = p.ReadInt32();
                m.QueryData.Add(d);
            }
            return m;
        }
    }
}
