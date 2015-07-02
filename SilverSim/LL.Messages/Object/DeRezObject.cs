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
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Object
{
    [UDPMessage(MessageType.DeRezObject)]
    [Reliable]
    public class DeRezObject : Message
    {
        public enum DeRezAction : byte
        {
            SaveToExistingUserInventoryItem = 0,
            TakeCopy = 1,
            Take = 4,
            GodTakeCopy = 5,
            Delete = 6,
            Return = 9
        }

        public UUID AgentID;
        public UUID SessionID;
        public UUID GroupID;
        public DeRezAction Destination;
        public UUID DestinationID;
        public UUID TransactionID;
        public byte PacketCount;
        public byte PacketNumber;

        public List<UInt32> ObjectLocalIDs = new List<UInt32>();

        public DeRezObject()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            DeRezObject m = new DeRezObject();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.Destination = (DeRezAction)p.ReadUInt8();
            m.DestinationID = p.ReadUUID();
            m.TransactionID = p.ReadUUID();
            m.PacketCount = p.ReadUInt8();
            m.PacketNumber = p.ReadUInt8();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.ObjectLocalIDs.Add(p.ReadUInt32());
            }

            return m;
        }
    }
}
