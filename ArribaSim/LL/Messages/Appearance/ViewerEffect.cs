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
using System.Collections.Generic;

namespace ArribaSim.LL.Messages.Appearance
{
    public class ViewerEffect : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public struct EffectData
        {
            public UUID ID;
            public UUID AgentID;
            public byte Type;
            public double Duration;
            public byte[] EffectColor;
            public byte[] TypeData;
        }

        public List<EffectData> Effects = new List<EffectData>();

        public ViewerEffect()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.ViewerEffect;
            }
        }

        public static Message Decode(UDPPacket p)
        {
            ViewerEffect m = new ViewerEffect();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                EffectData d;
                d.ID = p.ReadUUID();
                d.AgentID = p.ReadUUID();
                d.Type = p.ReadUInt8();
                d.Duration = p.ReadFloat();
                d.EffectColor = p.ReadBytes(4);
                d.TypeData = p.ReadBytes(p.ReadUInt8());
            }

            return m;
        }

        public new void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);

            p.WriteUInt8((byte)Effects.Count);
            foreach(EffectData d in Effects)
            {
                p.WriteUUID(d.ID);
                p.WriteUUID(d.AgentID);
                p.WriteUInt8(d.Type);
                p.WriteFloat((float)d.Duration);
                p.WriteBytes(d.EffectColor);
                p.WriteUInt8((byte)d.TypeData.Length);
                p.WriteBytes(d.TypeData);
            }
        }
    }
}
