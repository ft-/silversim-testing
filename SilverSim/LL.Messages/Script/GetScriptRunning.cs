// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Script
{
    [UDPMessage(MessageType.GetScriptRunning)]
    [Reliable]
    [NotTrusted]
    public class GetScriptRunning : Message
    {
        public UUID ObjectID;
        public UUID ItemID;

        public GetScriptRunning()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            GetScriptRunning m = new GetScriptRunning();
            m.ObjectID = p.ReadUUID();
            m.ItemID = p.ReadUUID();

            return m;
        }
    }
}
