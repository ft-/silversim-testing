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
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Generic
{
    public abstract class GenericMessageFormat : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID TransactionID = UUID.Random;
        public string Method = string.Empty;
        public UUID Invoice = UUID.Zero;

        public List<byte[]> ParamList = new List<byte[]>();

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(TransactionID);
            p.WriteStringLen8(Method);
            p.WriteUUID(Invoice);
            p.WriteUInt8((byte)ParamList.Count);
            foreach (byte[] b in ParamList)
            {
                p.WriteUInt8((byte)b.Length);
                p.WriteBytes(b);
            }
        }

        public static Message Decode(UDPPacket p, GenericMessageFormat m)
        {
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.TransactionID = p.ReadUUID();
            m.Method = p.ReadStringLen8();
            m.Invoice = p.ReadUUID();
            int c = p.ReadUInt8();
            for (int i = 0; i < c; ++i)
            {
                m.ParamList.Add(p.ReadBytes(p.ReadUInt8()));
            }

            return m;
        }
    }
}
