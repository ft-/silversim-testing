// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Script;
using System;

namespace SilverSim.Viewer.Messages.Script
{
    [UDPMessage(MessageType.ScriptQuestion)]
    [Reliable]
    public class ScriptQuestion : Message
    {
        public UUID TaskID = UUID.Zero;
        public UUID ItemID = UUID.Zero;
        public string ObjectName;
        public string ObjectOwner;
        public ScriptPermissions Questions;
        public UUID ExperienceID = UUID.Zero;

        public ScriptQuestion()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(TaskID);
            p.WriteUUID(ItemID);
            p.WriteStringLen8(ObjectName);
            p.WriteStringLen8(ObjectOwner);
            p.WriteUInt32((uint)Questions);
            p.WriteUUID(ExperienceID);
        }

        public static Message Decode(UDPPacket p)
        {
            ScriptQuestion m = new ScriptQuestion();
            m.TaskID = p.ReadUUID();
            m.ItemID = p.ReadUUID();
            m.ObjectName = p.ReadStringLen8();
            m.ObjectOwner = p.ReadStringLen8();
            m.Questions = (ScriptPermissions)p.ReadUInt32();
            try
            {
                m.ExperienceID = p.ReadUUID();
            }
            catch
            {
                m.ExperienceID = UUID.Zero;
            }
            return m;
        }
    }
}
