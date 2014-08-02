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

namespace SilverSim.LL.Messages.Search
{
    public class PlacesQuery : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID QueryID;
        public UUID TransactionID;
        public string QueryText;
        public UInt32 QueryFlags;
        public sbyte Category;
        public string SimName;

        public PlacesQuery()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.PlacesQuery;
            }
        }

        public static Message Decode(UDPPacket p)
        {
            PlacesQuery m = new PlacesQuery();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.QueryID = p.ReadUUID();
            m.TransactionID = p.ReadUUID();
            m.QueryText = p.ReadStringLen8();
            m.QueryFlags = p.ReadUInt32();
            m.Category = p.ReadInt8();
            m.SimName = p.ReadStringLen8();

            return m;
        }
    }
}
