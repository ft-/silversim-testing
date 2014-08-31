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
    public class AvatarPropertiesReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID AvatarID = UUID.Zero;
        public UUID ImageID = UUID.Zero;
        public UUID FLImageID = UUID.Zero;
        public UUID PartnerID = UUID.Zero;
        public string AboutText;
        public string FLAboutText;
        public string BornOn;
        public string ProfileURL;
        public byte[] CharterMember = new byte[1];
        public UInt32 Flags = 0;

        public AvatarPropertiesReply()
        {

        }

        public override MessageType Number
        {
            get
            {
                return MessageType.AvatarPropertiesReply;
            }
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(AvatarID);
            p.WriteUUID(ImageID);
            p.WriteUUID(FLImageID);
            p.WriteStringLen16(AboutText);
            p.WriteStringLen8(FLAboutText);
            p.WriteStringLen8(BornOn);
            p.WriteStringLen8(ProfileURL);
            p.WriteUInt8((byte)CharterMember.Length);
            p.WriteBytes(CharterMember);
            p.WriteUInt32(Flags);
        }
    }
}
