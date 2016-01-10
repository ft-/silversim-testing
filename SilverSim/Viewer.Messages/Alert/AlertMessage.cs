// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Alert
{
    [UDPMessage(MessageType.AlertMessage)]
    [Reliable]
    [Trusted]
    public class AlertMessage : Message
    {
        public struct Data
        {
            public string Message;
            public byte[] ExtraParams;
        }

        public string Message = string.Empty;
        public List<Data> AlertInfo = new List<Data>();

        public AlertMessage()
        {

        }

        public AlertMessage(string message)
        {
            Message = message;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteStringLen8(Message);
            p.WriteUInt8((byte)AlertInfo.Count);
            foreach(Data d in AlertInfo)
            {
                p.WriteStringLen8(d.Message);
                p.WriteUInt8((byte)d.ExtraParams.Length);
                p.WriteBytes(d.ExtraParams);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            AlertMessage m = new AlertMessage();
            m.Message = p.ReadStringLen8();
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                Data d = new Data();
                d.Message = p.ReadStringLen8();
                d.ExtraParams = p.ReadBytes(p.ReadUInt8());
                m.AlertInfo.Add(d);
            }
            return m;
        }
    }
}
