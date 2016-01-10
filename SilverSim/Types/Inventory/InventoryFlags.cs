// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace SilverSim.Types.Inventory
{
    [Flags]
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public enum InventoryFlags : uint
    {
        None = 0,
        LandmarkVisited = 1,
        GestureActive = 1,
        ObjectSlamPerm = 1 << 8,
        ObjectSlamSale = 1 << 12,
        ObjectPermOverwriteBase = 1 << 16,
        ObjectPermOverwriteOwner = 1 << 17,
        ObjectPermOverwriteGroup = 1 << 18,
        ObjectPermOverwriteEveryOne = 1 << 19,
        ObjectPermOverwriteNextOwner = 1 << 20,
        ObjectHasMultipleItems = 1 << 21,

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

        SharedSingleReference = 1 << 30,
        PermOverwriteMask = ObjectPermOverwriteBase | ObjectPermOverwriteOwner | ObjectPermOverwriteGroup | ObjectPermOverwriteEveryOne | ObjectPermOverwriteNextOwner
    }
}
