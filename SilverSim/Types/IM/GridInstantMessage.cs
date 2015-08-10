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
        public bool IsFromGroup = false;
        public string Message = string.Empty;
        public UUID IMSessionID = UUID.Zero;
        public bool IsOffline = false;
        public Vector3 Position = Vector3.Zero;
        public byte[] BinaryBucket = new byte[0];
        public uint ParentEstateID = 0;
        public UUID RegionID = UUID.Zero;
        public Date Timestamp = new Date();

        public delegate void OnResultDelegate(GridInstantMessage im, bool success);
        public OnResultDelegate OnResult;
        public bool NoOfflineIMStore = false;
        public bool IsSystemMessage = false;

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
