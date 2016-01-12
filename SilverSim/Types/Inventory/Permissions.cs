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
        Transfer = 1 << 13,
        Modify = 1 << 14,
        Copy = 1 << 15,
        Export = 1 << 16,
        Move = 1 << 19,
        Damage = 1 << 20, /* deprecated */
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
