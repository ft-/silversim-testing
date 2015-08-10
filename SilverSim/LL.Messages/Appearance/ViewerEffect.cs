// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Appearance
{
    [UDPMessage(MessageType.ViewerEffect)]
    [NotTrusted]
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
            public ColorAlpha EffectColor;
            public byte[] TypeData;
        }

        public List<EffectData> Effects = new List<EffectData>();

        public ViewerEffect()
        {

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
                byte[] t = p.ReadBytes(4);
                d.EffectColor = new ColorAlpha();
                d.EffectColor.R_AsByte = t[0];
                d.EffectColor.G_AsByte = t[1];
                d.EffectColor.B_AsByte = t[2];
                d.EffectColor.A_AsByte = t[3];
                d.TypeData = p.ReadBytes(p.ReadUInt8());
            }

            return m;
        }

        public override void Serialize(UDPPacket p)
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
                p.WriteBytes(d.EffectColor.AsByte);
                p.WriteUInt8((byte)d.TypeData.Length);
                p.WriteBytes(d.TypeData);
            }
        }
    }
}
