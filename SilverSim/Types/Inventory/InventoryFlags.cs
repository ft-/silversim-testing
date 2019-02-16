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

#pragma warning disable RCS1154

using System;

namespace SilverSim.Types.Inventory
{
    [Flags]
    public enum InventoryFlags : uint
    {
        None = 0,

        #region inventorytype landmark
        LandmarkVisited = 1,
        #endregion

        #region inventorytype gesture
        GestureActive = 1,
        #endregion

        #region inventorytype object
        /** <summary>When set, apply next owner permissions instead of base permissions.</summary> 
         * <remarks>Reset when assetid is updated.</remarks>
         */
        ObjectSlamPerm = 1 << 8,

        /** <summary>When set the sale information has been changed</summary> 
         * <remarks>Reset when assetid is updated.</remarks>
         */
        ObjectSlamSale = 1 << 12,

        /** <summary>When set, the inventory base permissions are used on rez. When not set, the asset base permissions are used on rez.</summary> 
         * <remarks>Reset when assetid is updated.</remarks>
         */
        ObjectPermOverwriteBase = 1 << 16,

        /** <summary>When set, the inventory owner permissions are used on rez. When not set, the asset owner permissions are used on rez.</summary> 
         * <remarks>Reset when assetid is updated.</remarks>
         */
        ObjectPermOverwriteOwner = 1 << 17,

        /** <summary>When set, the inventory group permissions are used on rez. When not set, the asset group permissions are used on rez.</summary> 
         * <remarks>Reset when assetid is updated.</remarks>
         */
        ObjectPermOverwriteGroup = 1 << 18,

        /** <summary>When set, the inventory everyone permissions are used on rez. When not set, the asset everyone permissions are used on rez.</summary> 
         * <remarks>Reset when assetid is updated.</remarks>
         */
        ObjectPermOverwriteEveryOne = 1 << 19,

        /** <summary>When set, the inventory nextowner permissions are used on rez. When not set, the asset nextowner permissions are used on rez.</summary>
         * <remarks>Reset when assetid is updated.</remarks>
         */
        ObjectPermOverwriteNextOwner = 1 << 20,

        /** <summary>When set, the inventory item is composed of multiple items</summary> */
        ObjectHasMultipleItems = 1 << 21,

        #endregion

        #region inventorytype_notecard

        /** <summary>When set, apply next owner permissions instead of base permissions. Triggers setting ObjectSlamPerm.</summary> 
         * <remarks>Reset when assetid is updated.</remarks>
         */
        NotecardSlamPerm = 1 << 8,

        /** <summary>When set the sale information has been changed. Triggers setting ObjectSlamSale.</summary> 
         * <remarks>Reset when assetid is updated.</remarks>
         */
        NotecardSlamSale = 1 << 12,
        #endregion

        #region inventorytype wearable
        WearablesTypeMask = 0xFF,

        WearableType_Shape = 0,
        WearableType_Skin = 1,
        WearableType_Hair = 2,
        WearableType_Eyes = 3,
        WearableType_Shirt = 4,
        WearableType_Pants = 5,
        WearableType_Shoes = 6,
        WearableType_Socks = 7,
        WearableType_Jacket = 8,
        WearableType_Gloves = 9,
        WearableType_Undershirt = 10,
        WearableType_Underpants = 11,
        WearableType_Skirt = 12,
        WearableType_Alpha = 13,
        WearableType_Tattoo = 14,
        WearableType_Physics = 15,
        WearableType_Universal = 16,
        #endregion

        #region inventorytype settings
        SettingsType_Mask = 0xFF,

        SettingsType_Sky = 0,
        SettingsType_Water = 1,
        SettingsType_Daycycle = 2,
        #endregion

        SharedSingleReference = 1 << 30,
        PermOverwriteMask = ObjectPermOverwriteBase | ObjectPermOverwriteOwner | ObjectPermOverwriteGroup | ObjectPermOverwriteEveryOne | ObjectPermOverwriteNextOwner
    }
}
