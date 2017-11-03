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

        public GridInstantMessage Clone()
        {
            var gim = new GridInstantMessage
            {
                FromAgent = FromAgent,
                FromGroup = FromGroup,
                ToAgent = ToAgent,
                Dialog = Dialog,
                IsFromGroup = IsFromGroup,
                Message = Message,
                IMSessionID = IMSessionID,
                IsOffline = IsOffline,
                Position = Position,
                BinaryBucket = new byte[BinaryBucket.Length],
                ParentEstateID = ParentEstateID,
                RegionID = RegionID,
                Timestamp = new Date(Timestamp),
                OnResult = OnResult,
                NoOfflineIMStore = NoOfflineIMStore,
                IsSystemMessage = IsSystemMessage,
                ResultInfo = ResultInfo
            };
            Buffer.BlockCopy(BinaryBucket, 0, gim.BinaryBucket, 0, BinaryBucket.Length);
            return gim;
        }
    }
}
