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

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(Sender);
            p.WriteBoolean(IsTrial);
            p.WriteUInt16((UInt16)TextureEntry.Length);
            p.WriteBytes(TextureEntry);
            if (VisualParams.Length > 255)
            {
                p.WriteUInt8(255);
                p.WriteBytes(VisualParams, 255);
            }
            else
            {
                p.WriteUInt8((byte)VisualParams.Length);
                p.WriteBytes(VisualParams);
            }
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
            var m = new AvatarAppearance()
            {
                Sender = p.ReadUUID(),
                IsTrial = p.ReadBoolean(),
                TextureEntry = p.ReadBytes(p.ReadUInt16()),
                VisualParams = p.ReadBytes(p.ReadUInt8())
            };
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                m.AppearanceData.Add(new AppearanceDataEntry()
                {
                    AppearanceVersion = p.ReadUInt8(),
                    CofVersion = p.ReadInt32(),
                    Flags = p.ReadUInt32()
                });
            }
            return m;
        }
    }
}
