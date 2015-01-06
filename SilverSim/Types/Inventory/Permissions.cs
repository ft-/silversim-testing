using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Types.Inventory
{
    [Flags] 
    public enum InventoryPermissionsMask : uint
    {
        None = 0,
        Transfer = 1 << 13,
        Modify = 1 << 14,
        Copy = 1 << 15,
        Export = 1 << 16,
        Move = 1 << 19,
        Damage = 1 << 20,
        All = Transfer | Modify | Copy | Move,
        Every = 0x7FFFFFFF
    }

    public struct PermissionsData
    {
        public InventoryPermissionsMask Base;
        public InventoryPermissionsMask Current;
        public InventoryPermissionsMask EveryOne;
        public InventoryPermissionsMask Group;
        public InventoryPermissionsMask NextOwner;

        public bool CheckAgentPermissions(UUI creator, UUI owner, UUI accessor, InventoryPermissionsMask wanted)
        {
            if(accessor == creator)
            {
                return true;
            }
            else if(accessor == owner)
            {
                return (wanted & Current) == wanted;
            }
            else
            {
                return (wanted & EveryOne) == wanted;
            }
        }

        public bool CheckGroupPermissions(UUI creator, UGI ownergroup, UUI accessor, UGI accessorgroup, InventoryPermissionsMask wanted)
        {
            if(accessor == creator)
            {
                return true;
            }
            else if(accessorgroup == ownergroup)
            {
                return (wanted & Group) == wanted;
            }
            else
            {
                return (wanted & EveryOne) == wanted;
            }
        }
    }
}
