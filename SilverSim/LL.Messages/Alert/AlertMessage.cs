// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;

namespace SilverSim.LL.Messages.Alert
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
            p.WriteMessageType(Number);
            p.WriteStringLen8(Message);
            p.WriteUInt8((byte)AlertInfo.Count);
            foreach(Data d in AlertInfo)
            {
                p.WriteStringLen8(d.Message);
                p.WriteUInt8((byte)d.ExtraParams.Length);
                p.WriteBytes(d.ExtraParams);
            }
        }
    }
}
