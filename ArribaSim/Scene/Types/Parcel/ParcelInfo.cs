/*

ArribaSim is distributed under the terms of the
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ArribaSim.Types;

namespace ArribaSim.Scene.Types.Parcel
{
    public enum ObjectReturnType : uint
    {
        None = 0,
        Owner = 1 << 1,
        Group = 1 << 2,
        Other = 1 << 3,
        List = 1 << 4,
        Sell = 1 << 5
    }

    public enum ParcelAccessFlags : uint
    {
        NoAccess = 0,
        Access = 1
    }

    public enum TeleportLandingType : byte
    {
        None = 0,
        LandingPoint = 1,
        Direct = 2
    }

    public enum ParcelFlags : uint
    {
        None = 0,
        AllowFly = 1 << 0,
        AllowOtherScripts = 1 << 1,
        ForSale = 1 << 2,
        AllowLandmark = 1 << 3,
        AllowTerraform = 1 << 4,
        AllowDamage = 1 << 5,
        CreateObjects = 1 << 6,
        ForSaleObjects = 1 << 7,
        UseAccessGroup = 1 << 8,
        UseAccessList = 1 << 9,
        UseBanList = 1 << 10,
        UsePassList = 1 << 11,
        ShowDirectory = 1 << 12,
        AllowDeedToGroup = 1 << 13,
        ContributeWithDeed = 1 << 14,
        SoundLocal = 1 << 15,
        SellParcelObjects = 1 << 16,
        AllowPublish = 1 << 17,
        MaturePublish = 1 << 18,
        UrlWebPage = 1 << 19,
        UrlRawHtml = 1 << 20,
        RestrictPushObject = 1 << 21,
        DenyAnonymous = 1 << 22,
        LindenHome = 1 << 23,
        AllowGroupScripts = 1 << 25,
        CreateGroupScripts = 1 << 26,
        AllowAPrimitiveEntry = 1 << 27,
        AllowGroupObjectEntry = 1 << 28,
        AllowVoiceChat = 1 << 29,
        UseEstateVoiceChan = 1 << 30,
        DenyAgeUnverified = (uint)1 << 31
    }

    public enum ParcelStatus : int
    {
        None = -1,
        Leased = 0,
        LeasePending = 1,
        Abandoned = 2
    }

    public enum ParcelCategory : int
    {
        None = 0,
        LL,
        Adult,
        Arts,
        Business,
        Educational,
        Gaming,
        Hangout,
        Newcomer,
        Park,
        Residential,
        Shopping,
        Stage,
        Other,
        Any = -1
    }

    public class ParcelInfo
    {
        public int Area = 0;
        public uint AuctionID = 0;
        public UUI AuthBuyer = new UUI();
        public ParcelCategory Category = ParcelCategory.None;
        public Date ClaimDate = new Date();
        public int ClaimPrice = 0;
        public UUID GlobalID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public bool GroupOwned = false;
        public string Description = string.Empty;
        public uint Flags = (uint)ParcelFlags.AllowFly |
                            (uint)ParcelFlags.AllowLandmark |
                            (uint)ParcelFlags.AllowAPrimitiveEntry |
                            (uint)ParcelFlags.AllowDeedToGroup |
                            (uint)ParcelFlags.AllowTerraform |
                            (uint)ParcelFlags.CreateObjects |
                            (uint)ParcelFlags.AllowOtherScripts |
                            (uint)ParcelFlags.SoundLocal |
                            (uint)ParcelFlags.AllowVoiceChat;
        public TeleportLandingType LandingType = TeleportLandingType.None;
        public Vector3 LandingPosition = Vector3.Zero;
        public Vector3 LandingLookAt = Vector3.Zero;
        public string Name = string.Empty;
        public ParcelStatus Status = ParcelStatus.Leased;
        public int LocalID = 0;
        public URI MusicURI = null;
        public UUI Owner = new UUI();
        public UUID SnapshotID = UUID.Zero;

        public ParcelInfo()
        {

        }
    }
}
