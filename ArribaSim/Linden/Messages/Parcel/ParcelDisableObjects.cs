﻿/*

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

namespace ArribaSim.Linden.Messages.Parcel
{
    public class ParcelDisableObjects : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public Int32 LocalID = 0;
        public UInt32 ReturnType = 0;
        public List<UUID> TaskIDs = new List<UUID>();
        public List<UUID> OwnerIDs = new List<UUID>();

        public ParcelDisableObjects()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.ParcelDisableObjects;
            }
        }

        public static Message Decode(UDPPacket p)
        {
            ParcelDisableObjects m = new ParcelDisableObjects();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.LocalID = p.ReadInt32();
            m.ReturnType = p.ReadUInt32();

            uint cnt;
            cnt = p.ReadUInt8();
            for (uint i = 0; i < cnt; ++i)
            {
                m.TaskIDs.Add(p.ReadUUID());
            }

            cnt = p.ReadUInt8();
            for (uint i = 0; i < cnt; ++i)
            {
                m.OwnerIDs.Add(p.ReadUUID());
            }
            return m;
        }
    }
}
