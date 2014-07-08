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

namespace ArribaSim.Linden.Messages.Parcel
{
    public class ParcelAccessListReply : Message
    {
        public UUID AgentID;
        public Int32 SequenceID;
        public UInt32 Flags;
        public Int32 LocalID;

        public struct Data
        {
            public UUID ID;
            public UInt32 Time;
            public UInt32 Flags;
        }

        public List<Data> AccessList = new List<Data>();

        public ParcelAccessListReply()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.ParcelAccessListReply;
            }
        }

        public new void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteInt32(SequenceID);
            p.WriteUInt32(Flags);
            p.WriteInt32(LocalID);

            p.WriteUInt8((byte)AccessList.Count);
            foreach (Data d in AccessList)
            {
                p.WriteUUID(d.ID);
                p.WriteUInt32(d.Time);
                p.WriteUInt32(d.Flags);
            }
        }
    }
}
