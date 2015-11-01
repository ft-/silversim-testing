// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3


using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Object
{
    [Flags]
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public enum UpdateChangedFlags : ulong
    {
        /* bits 0 - 31 are Viewer Protocol */
        Inventory = 0x001,
        Color = 0x002,
        Shape = 0x004,
        Scale = 0x008,
        Texture = 0x010,
        Link = 0x020,
        AllowedDrop = 0x040,
        Owner = 0x080,
        Region = 0x100,
        Teleport = 0x200,
        RegionStart = 0x400,
        Media = 0x800,

        Physics = 1 << 32,
        PartPermissions = 1 << 33,
    }
}
