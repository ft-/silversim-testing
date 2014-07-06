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
using System.Collections.Generic;

namespace ArribaSim.Linden.Messages.Land
{
    public class ModifyLand : Message
    {
        public struct Data
        {
            public Int32 LocalID;
            public double West;
            public double South;
            public double East;
            public double North;
        }

        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        
        public byte Action = 0;
        public byte BrushSize = 0;
        public double Seconds = 0;
        public double Height = 0;

        public List<Data> ParcelData = new List<Data>();
        public List<double> ModifyBlockExtended = new List<double>();

        public ModifyLand()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.ModifyLand;
            }
        }

        public static Message Decode(UDPPacket p)
        {
            ModifyLand m = new ModifyLand();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.Action = p.ReadUInt8();
            m.BrushSize = p.ReadUInt8();
            m.Seconds = p.ReadFloat();
            m.Height = p.ReadFloat();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                Data d = new Data();
                d.LocalID = p.ReadInt32();
                d.West = p.ReadFloat();
                d.South = p.ReadFloat();
                d.East = p.ReadFloat();
                d.North = p.ReadFloat();
                m.ParcelData.Add(d);
            }

            c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.ModifyBlockExtended.Add(p.ReadFloat());
            }
            return m;
        }
    }
}
