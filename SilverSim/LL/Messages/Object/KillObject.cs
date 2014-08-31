﻿/*

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

using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Object
{
    public class KillObject : Message
    {
        public List<UInt32> LocalIDs = new List<UInt32>();

        public KillObject()
        {

        }

        public override MessageType Number
        {
            get
            {
                return MessageType.KillObject;
            }
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUInt8((byte)LocalIDs.Count);
            foreach (UInt32 i in LocalIDs)
            {
                p.WriteUInt32(i);
            }
        }
    }
}
