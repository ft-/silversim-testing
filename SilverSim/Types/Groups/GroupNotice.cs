// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.Asset;
namespace SilverSim.Types.Groups
{
    public class GroupNotice
    {
        public UGI Group = UGI.Unknown;
        public UUID ID = UUID.Zero;
        public Date Timestamp = new Date();
        public string FromName = string.Empty;
        public string Subject = string.Empty;
        public string Message = string.Empty;
        public bool HasAttachment;
        public AssetType AttachmentType;
        public string AttachmentName = string.Empty;
        public UUID AttachmentItemID = UUID.Zero;
        public UUI AttachmentOwner = UUI.Unknown;

        public GroupNotice()
        {

        }
    }
}
