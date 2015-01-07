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

namespace SilverSim.LL.Messages.Event
{
    public class EventInfoReply : Message
    {
        public UUID AgentID;
        public UInt32 EventID;
        public string Creator;
        public string Name;
        public string Category;
        public string Desc;
        public string Date;
        public UInt32 DateUTC;
        public UInt32 Duration;
        public UInt32 Cover;
        public UInt32 Amount;
        public string SimName;
        public Vector3 GlobalPos;
        public UInt32 EventFlags;

        public EventInfoReply()
        {

        }

        public override MessageType Number
        {
            get
            {
                return MessageType.EventInfoReply;
            }
        }

        public override bool IsReliable
        {
            get
            {
                return true;
            }
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUInt32(EventID);
            p.WriteStringLen8(Creator);
            p.WriteStringLen8(Name);
            p.WriteStringLen8(Category);
            p.WriteStringLen16(Desc);
            p.WriteStringLen8(Date);
            p.WriteUInt32(DateUTC);
            p.WriteUInt32(Duration);
            p.WriteUInt32(Cover);
            p.WriteUInt32(Amount);
            p.WriteStringLen8(SimName);
            p.WriteVector3d(GlobalPos);
            p.WriteUInt32(EventFlags);
        }
    }
}
