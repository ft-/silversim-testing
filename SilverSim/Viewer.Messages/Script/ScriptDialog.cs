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

namespace SilverSim.Viewer.Messages.Script
{
    [UDPMessage(MessageType.ScriptDialog)]
    [Reliable]
    [Trusted]
    public class ScriptDialog : Message
    {
        public UUID ObjectID;
        public string FirstName;
        public string LastName;
        public string ObjectName;
        public string Message;
        public Int32 ChatChannel;
        public UUID ImageID;

        public List<string> Buttons = new List<string>();

        public List<UUID> OwnerData = new List<UUID>();

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(ObjectID);
            p.WriteStringLen8(FirstName);
            p.WriteStringLen8(LastName);
            p.WriteStringLen8(ObjectName);
            p.WriteStringLen16(Message);
            p.WriteInt32(ChatChannel);
            p.WriteUUID(ImageID);

            p.WriteUInt8((byte)Buttons.Count);
            foreach (var d in Buttons)
            {
                p.WriteStringLen8(d);
            }

            p.WriteUInt8((byte)OwnerData.Count);
            foreach(var d in OwnerData)
            {
                p.WriteUUID(d);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            var m = new ScriptDialog
            {
                ObjectID = p.ReadUUID(),
                FirstName = p.ReadStringLen8(),
                LastName = p.ReadStringLen8(),
                ObjectName = p.ReadStringLen8(),
                Message = p.ReadStringLen16(),
                ChatChannel = p.ReadInt32(),
                ImageID = p.ReadUUID()
            };
            uint n = p.ReadUInt8();

            while(n-- != 0)
            {
                m.Buttons.Add(p.ReadStringLen8());
            }

            n = p.ReadUInt8();
            while(n-- != 0)
            {
                m.OwnerData.Add(p.ReadUUID());
            }
            return m;
        }
    }
}
