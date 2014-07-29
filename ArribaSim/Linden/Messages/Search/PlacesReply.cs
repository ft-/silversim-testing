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
using System;
using System.Collections.Generic;

namespace ArribaSim.Linden.Messages.Search
{
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

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.PlacesReply;
            }
        }

        public new void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
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
    }
}
