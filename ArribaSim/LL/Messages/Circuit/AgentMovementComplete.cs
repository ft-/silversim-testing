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

namespace ArribaSim.LL.Messages.Circuit
{
    public class AgentMovementComplete : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public Vector3 Position;
        public Vector3 LookAt;
        public UInt64 RegionHandle;
        public UInt32 Timestamp;
        public string ChannelVersion;

        public AgentMovementComplete()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.AgentMovementComplete;
            }
        }

        public new void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteVector3f(Position);
            p.WriteVector3f(LookAt);
            p.WriteUInt64(RegionHandle);
            p.WriteUInt32(Timestamp);
            p.WriteStringLen16(ChannelVersion);
        }
    }
}
