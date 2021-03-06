﻿// SilverSim is distributed under the terms of the
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
    [UDPMessage(MessageType.ObjectScale)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    [UDPDeprecated]
    public class ObjectScale : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public struct ObjectDataEntry
        {
            public UInt32 ObjectLocalID;
            public Vector3 Size;
        }

        public List<ObjectDataEntry> ObjectData = new List<ObjectDataEntry>();

        public static ObjectScale Decode(UDPPacket p)
        {
            var m = new ObjectScale
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID()
            };
            uint cnt = p.ReadUInt8();
            while(cnt-- != 0)
            {
                m.ObjectData.Add(new ObjectDataEntry
                {
                    ObjectLocalID = p.ReadUInt32(),
                    Size = p.ReadVector3f()
                });
            }
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);

            p.WriteUInt8((byte)ObjectData.Count);
            foreach (var d in ObjectData)
            {
                p.WriteUInt32(d.ObjectLocalID);
                p.WriteVector3f(d.Size);
            }
        }
    }
}
