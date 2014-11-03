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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Types.Inventory
{
    [Flags]
    public enum InventoryFlags : uint
    {
        None = 0,
        LandmarkVisited = 1,
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
