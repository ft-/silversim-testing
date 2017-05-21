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

using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Object
{
    [Flags]
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public enum UpdateChangedFlags : ulong
    {
        None = 0,
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
