/*

SilverSim is distributed under the terms of the
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

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Script
{
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

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.ScriptDialog;
            }
        }

        public new void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
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
