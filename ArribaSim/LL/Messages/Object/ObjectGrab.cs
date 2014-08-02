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

namespace ArribaSim.LL.Messages.Object
{
    public class ObjectGrab : Message
    {
        public struct Data
        {
            public Vector3 UVCoord;
            public Vector3 STCoord;
            public Int32 FaceIndex;
            public Vector3 Position;
            public Vector3 Normal;
            public Vector3 Binormal;
        }

        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UInt32 ObjectLocalID = 0;
        public Vector3 GrabOffset = Vector3.Zero;

        public List<Data> ObjectData = new List<Data>();

        public ObjectGrab()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.ObjectGrab;
            }
        }

        public static Message Decode(UDPPacket p)
        {
            ObjectGrab m = new ObjectGrab();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ObjectLocalID = p.ReadUInt32();
            m.GrabOffset = p.ReadVector3f();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                Data d = new Data();
                d.UVCoord = p.ReadVector3f();
                d.STCoord = p.ReadVector3f();
                d.FaceIndex = p.ReadInt32();
                d.Position = p.ReadVector3f();
                d.Normal = p.ReadVector3f();
                d.Binormal = p.ReadVector3f();
                m.ObjectData.Add(d);
            }
            return m;
        }
    }
}
