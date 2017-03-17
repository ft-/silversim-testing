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
