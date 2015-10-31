// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
namespace SilverSim.Types.IM
{
    public class GridInstantMessage
    {
        public ulong ID;
        public UUI FromAgent = UUI.Unknown;
        public UGI FromGroup = UGI.Unknown;
        public UUI ToAgent = UUI.Unknown;
        public GridInstantMessageDialog Dialog;
        public bool IsFromGroup;
        public string Message = string.Empty;
        public UUID IMSessionID = UUID.Zero;
        public bool IsOffline;
        public Vector3 Position = Vector3.Zero;
        public byte[] BinaryBucket = new byte[0];
        public uint ParentEstateID;
        public UUID RegionID = UUID.Zero;
        public Date Timestamp = new Date();

        public Action<GridInstantMessage, bool /* success */> OnResult;
        public bool NoOfflineIMStore;
        public bool IsSystemMessage;

        /* can be used for storing the result */
        public bool ResultInfo;
        /* informational in certain modules */

        public GridInstantMessage()
        {

        }

        public GridInstantMessage Clone()
        {
            GridInstantMessage gim = new GridInstantMessage();
            gim.FromAgent = FromAgent;
            gim.FromGroup = FromGroup;
            gim.ToAgent = ToAgent;
            gim.Dialog = Dialog;
            gim.IsFromGroup = IsFromGroup;
            gim.Message = Message;
            gim.IMSessionID = IMSessionID;
            gim.IsOffline = IsOffline;
            gim.Position = Position;
            gim.BinaryBucket = new byte[BinaryBucket.Length];
            Buffer.BlockCopy(BinaryBucket, 0, gim.BinaryBucket, 0, BinaryBucket.Length);
            gim.ParentEstateID = ParentEstateID;
            gim.RegionID = RegionID;
            gim.Timestamp = new Date(Timestamp);
            gim.OnResult = OnResult;
            gim.NoOfflineIMStore = NoOfflineIMStore;
            gim.IsSystemMessage = IsSystemMessage;
            gim.ResultInfo = ResultInfo;
            return gim;
        }
    }
}
