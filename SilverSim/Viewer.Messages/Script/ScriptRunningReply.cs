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

        public static Message Decode(UDPPacket p)
        {
            ScriptRunningReply m = new ScriptRunningReply();
            m.ObjectID = p.ReadUUID();
            m.ItemID = p.ReadUUID();
            m.IsRunning = p.ReadBoolean();
            return m;
        }

        public override IValue SerializeEQG()
        {
            Types.Map script = new Types.Map();
            script.Add("ObjectID", ObjectID);
            script.Add("ItemID", ItemID);
            script.Add("Running", IsRunning);
            script.Add("Mono", true);

            AnArray scriptArr = new AnArray();
            scriptArr.Add(script);
            Types.Map body = new Types.Map();
            body.Add("Script", scriptArr);
            return body;
        }
    }
}
