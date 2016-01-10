﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Types.Asset;

namespace SilverSim.Viewer.Messages.Transfer
{
    [UDPMessage(MessageType.AssetUploadComplete)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class AssetUploadComplete : Message
    {
        public UUID AssetID;
        public AssetType AssetType;
        public bool Success;

        public AssetUploadComplete()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            AssetUploadComplete m = new AssetUploadComplete();
            m.AssetID = p.ReadUUID();
            m.AssetType = (AssetType)p.ReadInt8();
            m.Success = p.ReadBoolean();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AssetID);
            p.WriteInt8((sbyte)AssetType);
            p.WriteBoolean(Success);
        }
    }
}
