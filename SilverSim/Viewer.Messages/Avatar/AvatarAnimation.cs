// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Avatar
{
    [UDPMessage(MessageType.AvatarAnimation)]
    [Reliable]
    [Trusted]
    public class AvatarAnimation : Message
    {
        public UUID Sender = UUID.Zero;
        public struct AnimationData
        {
            public UUID AnimID;
            public UInt32 AnimSequenceID;
            public AnimationData(UUID animID, UInt32 seqID)
            {
                AnimID = animID;
                AnimSequenceID = seqID;
            }
        }
        public List<AnimationData> AnimationList = new List<AnimationData>();

        public struct AnimationSourceData
        {
            public UUID ObjectID;
            public AnimationSourceData(UUID objectID)
            {
                ObjectID = objectID;
            }
        }
        public List<AnimationSourceData> AnimationSourceList = new List<AnimationSourceData>();

        public struct PhysicalAvatarEventData
        {
            public byte[] TypeData;
        }
        public List<PhysicalAvatarEventData> PhysicalAvatarEventList = new List<PhysicalAvatarEventData>();

        public AvatarAnimation()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(Sender);
            p.WriteUInt8((byte)AnimationList.Count);
            foreach (AnimationData d in AnimationList)
            {
                p.WriteUUID(d.AnimID);
                p.WriteUInt32(d.AnimSequenceID);
            }
            p.WriteUInt8((byte)AnimationSourceList.Count);
            foreach(AnimationSourceData d in AnimationSourceList)
            {
                p.WriteUUID(d.ObjectID);
            }
            p.WriteUInt8((byte)PhysicalAvatarEventList.Count);
            foreach(PhysicalAvatarEventData d in PhysicalAvatarEventList)
            {
                p.WriteUInt8((byte)d.TypeData.Length);
                p.WriteBytes(d.TypeData);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            AvatarAnimation m = new AvatarAnimation();
            m.Sender = p.ReadUUID();
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                AnimationData d = new AnimationData();
                d.AnimID = p.ReadUUID();
                d.AnimSequenceID = p.ReadUInt32();
                m.AnimationList.Add(d);
            }
            n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                AnimationSourceData d = new AnimationSourceData();
                d.ObjectID = p.ReadUUID();
                m.AnimationSourceList.Add(d);
            }
            n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                PhysicalAvatarEventData d = new PhysicalAvatarEventData();
                d.TypeData = p.ReadBytes(p.ReadUInt8());
                m.PhysicalAvatarEventList.Add(d);
            }
            return m;
        }
    }
}
