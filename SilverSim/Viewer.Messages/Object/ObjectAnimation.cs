// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectAnimation)]
    [Trusted]
    [Reliable]
    public class ObjectAnimation : Message
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

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(Sender);
            p.WriteUInt8((byte)AnimationList.Count);
            foreach (AnimationData d in AnimationList)
            {
                p.WriteUUID(d.AnimID);
                p.WriteUInt32(d.AnimSequenceID);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            var m = new ObjectAnimation()
            {
                Sender = p.ReadUUID()
            };
            uint n = p.ReadUInt8();
            for (uint i = 0; i < n; ++i)
            {
                m.AnimationList.Add(new AnimationData()
                {
                    AnimID = p.ReadUUID(),
                    AnimSequenceID = p.ReadUInt32()
                });
            }
            return m;
        }
    }
}
