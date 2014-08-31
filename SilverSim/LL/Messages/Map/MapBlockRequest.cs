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

namespace SilverSim.LL.Messages.Map
{
    public class MapBlockRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UInt32 Flags;
        public UInt32 EstateID;
        public bool IsGodlike;
        public UInt16 MinX;
        public UInt16 MaxX;
        public UInt16 MinY;
        public UInt16 MaxY;

        public MapBlockRequest()
        {

        }

        public override MessageType Number
        {
            get
            {
                return MessageType.MapBlockRequest;
            }
        }

        public static Message Decode(UDPPacket p)
        {
            MapBlockRequest m = new MapBlockRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.Flags = p.ReadUInt32();
            m.EstateID = p.ReadUInt32();
            m.IsGodlike = p.ReadBoolean();
            m.MinX = p.ReadUInt16();
            m.MaxX = p.ReadUInt8();
            m.MinY = p.ReadUInt8();
            m.MaxY = p.ReadUInt8();

            return m;
        }
    }
}
