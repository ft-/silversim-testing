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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;

namespace SilverSim.LL.Messages.Appearance
{
    [UDPMessage(MessageType.AvatarAppearance)]
    [Reliable]
    public class AvatarAppearance : Message
    {
        public UUID Sender = UUID.Zero;
        public bool IsTrial = false;

        public byte[] TextureEntry;

        public byte[] VisualParams;

        public struct AppearanceDataEntry
        {
            public byte AppearanceVersion;
            public Int32 CofVersion;
            public UInt32 Flags;
        }

        public List<AppearanceDataEntry> AppearanceData = new List<AppearanceDataEntry>();

        public AvatarAppearance()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(Sender);
            p.WriteBoolean(IsTrial);
            p.WriteUInt16((UInt16)TextureEntry.Length);
            p.WriteBytes(TextureEntry);
            p.WriteUInt8((byte)VisualParams.Length);
            p.WriteBytes(VisualParams);
            p.WriteUInt8((byte)AppearanceData.Count);
            foreach(AppearanceDataEntry d in AppearanceData)
            {
                p.WriteUInt8(d.AppearanceVersion);
                p.WriteInt32(d.CofVersion);
                p.WriteUInt32(d.Flags);
            }
        }
    }
}
