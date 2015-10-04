// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Script
{
    [UDPMessage(MessageType.SetScriptRunning)]
    [Reliable]
    [NotTrusted]
    public class SetScriptRunning : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID ObjectID;
        public UUID ItemID;
        public bool IsRunning;

        public SetScriptRunning()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            SetScriptRunning m = new SetScriptRunning();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ObjectID = p.ReadUUID();
            m.ItemID = p.ReadUUID();
            m.IsRunning = p.ReadBoolean();

            return m;
        }
    }
}
