// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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

        public ScriptDialog()
        {

        }

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
            foreach (string d in Buttons)
            {
                p.WriteStringLen8(d);
            }

            p.WriteUInt8((byte)OwnerData.Count);
            foreach(UUID d in OwnerData)
            {
                p.WriteUUID(d);
            }
        }
    }
}
