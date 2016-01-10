// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Script
{
    [UDPMessage(MessageType.ScriptRunningReply)]
    [Reliable]
    [EventQueueGet("ScriptRunningReply")]
    [Trusted]
    [UDPDeprecated]
    public class ScriptRunningReply : Message
    {
        public UUID ObjectID;
        public UUID ItemID;
        public bool IsRunning;

        public ScriptRunningReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(ObjectID);
            p.WriteUUID(ItemID);
            p.WriteBoolean(IsRunning);
        }

        public override SilverSim.Types.IValue SerializeEQG()
        {
            SilverSim.Types.Map script = new SilverSim.Types.Map();
            script.Add("ObjectID", ObjectID);
            script.Add("ItemID", ItemID);
            script.Add("Running", IsRunning);
            script.Add("Mono", true);

            AnArray scriptArr = new AnArray();
            scriptArr.Add(script);
            SilverSim.Types.Map body = new SilverSim.Types.Map();
            body.Add("Script", scriptArr);
            return body;
        }
    }
}
