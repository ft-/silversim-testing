// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types.Inventory
{
    [Flags]
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    public enum InventoryPermissionsMask : uint
    {
        None = 0,
        UnusedBit0 = 1 << 0,
        UnusedBit1 = 1 << 1,
        UnusedBit2 = 1 << 2,
        UnusedBit3 = 1 << 3,
        UnusedBit4 = 1 << 4,
        UnusedBit5 = 1 << 5,
        UnusedBit6 = 1 << 6,
        UnusedBit7 = 1 << 7,
        UnusedBit8 = 1 << 8,
        UnusedBit9 = 1 << 9,
        UnusedBit10 = 1 << 10,
        UnusedBit11 = 1 << 11,
        UnusedBit12 = 1 << 12,
        Transfer = 1 << 13,
        Modify = 1 << 14,
        Copy = 1 << 15,
        Export = 1 << 16,
        Move = 1 << 19,
        Damage = 1 << 20, /* deprecated */
        Unused21 = 1 << 21,
        Unused22 = 1 << 22,
        Unused23 = 1 << 23,
        Unused24 = 1 << 24,
        Unused25 = 1 << 25,
        Unused26 = 1 << 26,
        Unused27 = 1 << 27,
        Unused28 = 1 << 28,
        Unused29 = 1 << 29,
        Unused30 = 1 << 30,
        Unused31 = (uint)1 << 31,
        All = Transfer | Modify | Copy | Move,
        ObjectPermissionsChangeable = 0xFFFFFFF8,
        Every = 0x7FFFFFFF
    }

    public struct InventoryPermissionsData
    {
        public InventoryPermissionsMask Base;
        public InventoryPermissionsMask Current;
        public InventoryPermissionsMask EveryOne;
        public InventoryPermissionsMask Group;
        public InventoryPermissionsMask NextOwner;

        public bool CheckAgentPermissions(UUI creator, UUI owner, UUI accessor, InventoryPermissionsMask wanted)
        {
            if(accessor.EqualsGrid(creator))
            {
                return true;
            }
            else if (wanted == InventoryPermissionsMask.None)
            {
                return false;
            }
            else if (accessor.EqualsGrid(owner))
            {
                return (wanted & Base & Current) == wanted;
            }
            else
            {
                return (wanted & Base & EveryOne) == wanted;
            }
        }

        public bool CheckGroupPermissions(UUI creator, UGI ownergroup, UUI accessor, UGI accessorgroup, InventoryPermissionsMask wanted)
        {
            if(accessor.EqualsGrid(creator))
            {
                return true;
            }
            else if(wanted == InventoryPermissionsMask.None)
            {
                return false;
            }
            else if(accessorgroup.Equals(ownergroup))
            {
                return (wanted & Base & Group) == wanted;
            }
            else
            {
                return (wanted & Base & EveryOne) == wanted;
            }
        }
    }
}
