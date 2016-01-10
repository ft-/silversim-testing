// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Appearance
{
    [UDPMessage(MessageType.AvatarAppearance)]
    [Reliable]
    [Trusted]
    public class AvatarAppearance : Message
    {
        public UUID Sender = UUID.Zero;
        public bool IsTrial;

        public byte[] TextureEntry;

        public byte[] VisualParams;

        public struct AppearanceDataEntry
        {
            public byte AppearanceVersion;
            public Int32 CofVersion;
            public UInt32 Flags;
        }

        public List<AppearanceDataEntry> AppearanceData = new List<AppearanceDataEntry>();

        public AvatarAppearance()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(Sender);
            p.WriteBoolean(IsTrial);
            p.WriteUInt16((UInt16)TextureEntry.Length);
            p.WriteBytes(TextureEntry);
            p.WriteUInt8((byte)VisualParams.Length);
            p.WriteBytes(VisualParams);
            p.WriteUInt8((byte)AppearanceData.Count);
            foreach(AppearanceDataEntry d in AppearanceData)
            {
                p.WriteUInt8(d.AppearanceVersion);
                p.WriteInt32(d.CofVersion);
                p.WriteUInt32(d.Flags);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            AvatarAppearance m = new AvatarAppearance();
            m.Sender = p.ReadUUID();
            m.IsTrial = p.ReadBoolean();
            m.TextureEntry = p.ReadBytes(p.ReadUInt16());
            m.VisualParams = p.ReadBytes(p.ReadUInt8());
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                AppearanceDataEntry d = new AppearanceDataEntry();
                d.AppearanceVersion = p.ReadUInt8();
                d.CofVersion = p.ReadInt32();
                d.Flags = p.ReadUInt32();
            }
            return m;
        }
    }
}
