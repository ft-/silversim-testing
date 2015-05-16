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

namespace SilverSim.LL.Messages.Transfer
{
    public class RequestXfer : Message
    {
        public UInt64 ID;
        public string Filename;
        public byte FilePath;
        public bool DeleteOnCompletion;
        public bool UseBigPackets;
        public UUID VFileID;
        public Int16 VFileType;

        public RequestXfer()
        {

        }

        public override MessageType Number
        {
            get
            {
                return MessageType.RequestXfer;
            }
        }

        public override bool IsReliable
        {
            get
            {
                return true;
            }
        }

        public override bool ZeroFlag
        {
            get
            {
                return true;
            }
        }

        public static RequestXfer Decode(UDPPacket p)
        {
            RequestXfer m = new RequestXfer();
            m.ID = p.ReadUInt64();
            m.Filename = p.ReadStringLen8();
            m.FilePath = p.ReadUInt8();
            m.DeleteOnCompletion = p.ReadBoolean();
            m.UseBigPackets = p.ReadBoolean();
            m.VFileID = p.ReadUUID();
            m.VFileType = p.ReadInt16();
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUInt64(ID);
            p.WriteStringLen8(Filename);
            p.WriteUInt8(FilePath);
            p.WriteBoolean(DeleteOnCompletion);
            p.WriteBoolean(UseBigPackets);
            p.WriteUUID(VFileID);
            p.WriteInt16(VFileType);
        }
    }
}
