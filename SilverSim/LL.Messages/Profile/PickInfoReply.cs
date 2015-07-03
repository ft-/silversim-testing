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

namespace SilverSim.LL.Messages.Profile
{
    [UDPMessage(MessageType.PickInfoReply)]
    [Reliable]
    [NotTrusted]
    public class PickInfoReply : Message
    {
        public UUID AgentID;
        public UUID PickID;
        public UUID CreatorID;
        public bool TopPick;
        public UUID ParcelID;
        public string Name;
        public string Description;
        public UUID SnapshotID;
        public string User;
        public string OriginalName;
        public Vector3 PosGlobal;
        public Int32 SortOrder;
        public bool IsEnabled;

        public PickInfoReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(PickID);
            p.WriteUUID(CreatorID);
            p.WriteBoolean(TopPick);
            p.WriteUUID(ParcelID);
            p.WriteStringLen8(Name);
            p.WriteStringLen16(Description);
            p.WriteUUID(SnapshotID);
            p.WriteStringLen8(User);
            p.WriteStringLen8(OriginalName);
            p.WriteVector3d(PosGlobal);
            p.WriteInt32(SortOrder);
            p.WriteBoolean(IsEnabled);
        }
    }
}
