// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;

namespace SilverSim.Viewer.Messages.Transfer
{
    [UDPMessage(MessageType.AssetUploadRequest)]
    [Reliable]
    [NotTrusted]
    public class AssetUploadRequest : Message
    {
        public UUID TransactionID;
        public AssetType AssetType;
        public bool IsTemporary;
        public bool StoreLocal;
        public byte[] AssetData = new byte[0];

        public AssetUploadRequest()
        {

        }

        public static AssetUploadRequest Decode(UDPPacket p)
        {
            AssetUploadRequest m = new AssetUploadRequest();
            m.TransactionID = p.ReadUUID();
            m.AssetType = (AssetType)p.ReadInt8();
            m.IsTemporary = p.ReadBoolean();
            m.StoreLocal = p.ReadBoolean();
            uint c = p.ReadUInt16();
            m.AssetData = p.ReadBytes((int)c);
            return m;
        }
    }
}
