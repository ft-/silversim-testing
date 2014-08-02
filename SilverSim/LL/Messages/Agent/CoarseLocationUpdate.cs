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

namespace SilverSim.LL.Messages.Agent
{
    public class CoarseLocationUpdate : Message
    {
        public byte X = 0;
        public byte Y = 0;
        public byte Z = 0;
        public Int16 You = 0;
        public Int16 Prey = 0;

        public List<UUID> AgentData = new List<UUID>();

        public CoarseLocationUpdate()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.CoarseLocationUpdate;
            }
        }

        public new void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUInt8(X);
            p.WriteUInt8(Y);
            p.WriteUInt8(Z);
            p.WriteInt16(You);
            p.WriteInt16(Prey);
            p.WriteUInt8((byte)AgentData.Count);
            foreach (UUID d in AgentData)
            {
                p.WriteStringLen8(d);
            }
        }
    }
}
