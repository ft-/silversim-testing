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

namespace SilverSim.LL.Messages.Region
{
    [UDPMessage(MessageType.GodUpdateRegionInfo)]
    [Reliable]
    [NotTrusted]
    public class GodUpdateRegionInfo : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public string SimName = string.Empty;
        public UInt32 EstateID = 0;
        public UInt32 ParentEstateID = 0;
        public UInt32 RegionFlags = 0;
        public double BillableFactor = 0;
        public Int32 PricePerMeter = 0;
        public Int32 RedirectGridX = 0;
        public Int32 RedirectGridY = 0;

        public GodUpdateRegionInfo()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            GodUpdateRegionInfo m = new GodUpdateRegionInfo();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.SimName = p.ReadStringLen8();
            m.EstateID = p.ReadUInt32();
            m.ParentEstateID = p.ReadUInt32();
            m.RegionFlags = p.ReadUInt32();
            m.BillableFactor = p.ReadFloat();
            m.PricePerMeter = p.ReadInt32();
            m.RedirectGridX = p.ReadInt32();
            m.RedirectGridY = p.ReadInt32();

            return m;
        }
    }
}
