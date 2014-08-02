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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;

namespace ArribaSim.LL.Messages.Map
{
    public class MapLayerReply : Message
    {
        public UUID AgentID;
        public UInt32 Flags;

        public struct LayerDataEntry
        {
            public UInt32 Left;
            public UInt32 Right;
            public UInt32 Top;
            public UInt32 Bottom;
            public UUID ImageID;
        }

        public List<LayerDataEntry> LayerData = new List<LayerDataEntry>();

        public MapLayerReply()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.MapLayerReply;
            }
        }

        public new void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUInt32(Flags);
            p.WriteUInt8((byte)LayerData.Count);
            foreach (LayerDataEntry d in LayerData)
            {
                p.WriteUInt32(d.Left);
                p.WriteUInt32(d.Right);
                p.WriteUInt32(d.Top);
                p.WriteUInt32(d.Bottom);
                p.WriteUUID(d.ImageID);
            }
        }
    }
}
