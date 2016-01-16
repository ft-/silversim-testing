// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.Inventory;
using System;

namespace SilverSim.Viewer.Messages.Object
{
    [Flags]
    public enum AggregatePermissionsMask : byte
    {
        None = 0,
        /* Compressed bit mask of InventoryPermissionsMask
        00TTMMCC
        MSB -> LSB
        TT = transfer, MM = modify, and CC = copy
        */
        CopyEmpty = 0,
        CopyNone = 1,
        CopySome = 2,
        CopyAll = 3,

        ModifyEmpty = 0 << 2,
        ModifyNone = 1 << 2,
        ModifySome = 2 << 2,
        ModifyAll = 3 << 2,

        TransferEmpty = 0 << 4,
        TransferNone = 1 << 4,
        TransferSome = 2 << 4,
        TransferAll = 3 << 4,
    }

    public static class AggregatePermsHelpers
    {
        public static AggregatePermissionsMask GetAggregatePermissions(this InventoryPermissionsMask mask)
        {
            AggregatePermissionsMask aggregateMask = AggregatePermissionsMask.CopyNone | AggregatePermissionsMask.ModifyNone | AggregatePermissionsMask.TransferNone;
            if((mask & InventoryPermissionsMask.Copy) != 0)
            {
                aggregateMask |= AggregatePermissionsMask.CopyAll;
            }
            if ((mask & InventoryPermissionsMask.Modify) != 0)
            {
                aggregateMask |= AggregatePermissionsMask.ModifyAll;
            }
            if ((mask & InventoryPermissionsMask.Transfer) != 0)
            {
                aggregateMask |= AggregatePermissionsMask.TransferAll;
            }
            return aggregateMask;
        }
    }
}
