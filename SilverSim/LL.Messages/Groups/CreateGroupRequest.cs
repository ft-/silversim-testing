// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.CreateGroupRequest)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class CreateGroupRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public string Name;
        public string Charter;
        public bool ShowInList;
        public UUID InsigniaID;
        public int MembershipFee;
        public bool OpenEnrollment;
        public bool AllowPublish;
        public bool MaturePublish;

        public CreateGroupRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            CreateGroupRequest m = new CreateGroupRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.Name = p.ReadStringLen8();
            m.Charter = p.ReadStringLen16();
            m.ShowInList = p.ReadBoolean();
            m.InsigniaID = p.ReadUUID();
            m.MembershipFee = p.ReadInt32();
            m.OpenEnrollment = p.ReadBoolean();
            m.AllowPublish = p.ReadBoolean();
            m.MaturePublish = p.ReadBoolean();

            return m;
        }
    }
}
