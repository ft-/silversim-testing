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
