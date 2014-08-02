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

namespace ArribaSim.LL.Messages.Avatar
{
    public class AvatarAnimation : Message
    {
        public UUID Sender = UUID.Zero;
        public struct AnimationData
        {
            public UUID AnimID;
            public UInt32 AnimSequenceID;
        }
        public List<AnimationData> AnimationList = new List<AnimationData>();

        public struct AnimationSourceData
        {
            public UUID ObjectID;
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

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.AvatarAnimation;
            }
        }

        public new void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
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
    }
}
