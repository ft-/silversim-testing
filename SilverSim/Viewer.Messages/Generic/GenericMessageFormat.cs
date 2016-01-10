// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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

        public GenericMessageFormat()
        {

        }

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
            int c = (int)p.ReadUInt8();
            for (int i = 0; i < c; ++i)
            {
                m.ParamList.Add(p.ReadBytes((int)p.ReadUInt8()));
            }

            return m;
        }
    }
}
