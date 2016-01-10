// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Profile
{
    [UDPMessage(MessageType.AvatarPropertiesUpdate)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class AvatarPropertiesUpdate : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID ImageID = UUID.Zero;
        public UUID FLImageID = UUID.Zero;
        public string AboutText;
        public string FLAboutText;
        public bool AllowPublish;
        public bool MaturePublish;
        public string ProfileURL;

        public AvatarPropertiesUpdate()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            AvatarPropertiesUpdate m = new AvatarPropertiesUpdate();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ImageID = p.ReadUUID();
            m.FLImageID = p.ReadUUID();
            m.AboutText = p.ReadStringLen16();
            m.FLAboutText = p.ReadStringLen8();
            m.AllowPublish = p.ReadBoolean();
            m.MaturePublish = p.ReadBoolean();
            m.ProfileURL = p.ReadStringLen8();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(ImageID);
            p.WriteUUID(FLImageID);
            p.WriteStringLen16(AboutText);
            p.WriteStringLen8(FLAboutText);
            p.WriteBoolean(AllowPublish);
            p.WriteBoolean(MaturePublish);
            p.WriteStringLen8(ProfileURL);
        }
    }
}
