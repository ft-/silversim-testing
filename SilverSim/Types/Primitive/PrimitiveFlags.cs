// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types.Primitive
{
    [Flags]
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public enum PrimitiveFlags : uint
    {
        None = 0,
        Physics = 1 << 0,
        CreateSelected = 1 << 1,
        ObjectModify = 1 << 2,
        ObjectCopy = 1 << 3,
        ObjectAnyOwner = 1 << 4,
        ObjectYouOwner = 1 << 5,
        Scripted = 1 << 6,
        Touch = 1 << 7,
        ObjectMove = 1 << 8,
        TakesMoney = 1 << 9,
        Phantom = 1 << 10,
        InventoryEmpty = 1 << 11,
        AffectsNavmesh = 1 << 12,
        Character = 1 << 13,
        VolumeDetect = 1 << 14,
        IncludeInSearch = 1 << 15,
        AllowInventoryDrop = 1 << 16,
        ObjectTransfer = 1 << 17,
        ObjectGroupOwned = 1 << 18,
        CameraDecoupled = 1 << 20,
        AnimSource = 1 << 21,
        CameraSource = 1 << 22,
        ObjectOwnerModify = 1 << 28,
        TemporaryOnRez = 1 << 29,
        Temporary = 1 << 30,
    }
}
