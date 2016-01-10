// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Agent
{
    [UDPMessage(MessageType.AgentAnimation)]
    [Reliable]
    [NotTrusted]
    public class AgentAnimation : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public struct AnimationEntry
        {
            public UUID AnimID;
            public bool StartAnim;
        }

        public List<AnimationEntry> AnimationEntryList = new List<AnimationEntry>();
        public List<byte[]> PhysicalAvatarEventList = new List<byte[]>();


        public AgentAnimation()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            AgentAnimation m = new AgentAnimation();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            int n;
            int i;

            n = p.ReadUInt8();
            for (i = 0; i < n; ++i)
            {
                AnimationEntry e = new AnimationEntry();
                e.AnimID = p.ReadUUID();
                e.StartAnim = p.ReadBoolean();
                m.AnimationEntryList.Add(e);
            }

            n = p.ReadUInt8();
            for (i = 0; i < n; ++i)
            {
                m.PhysicalAvatarEventList.Add(p.ReadBytes(p.ReadUInt8()));
            }

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);

            p.WriteUInt8((byte)AnimationEntryList.Count);
            foreach(AnimationEntry e in AnimationEntryList)
            {
                p.WriteUUID(e.AnimID);
                p.WriteBoolean(e.StartAnim);
            }

            p.WriteUInt8((byte)PhysicalAvatarEventList.Count);
            foreach(byte[] d in PhysicalAvatarEventList)
            {
                p.WriteUInt8((byte)d.Length);
                p.WriteBytes(d);
            }
        }
    }
}
