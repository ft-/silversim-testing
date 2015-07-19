/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

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
