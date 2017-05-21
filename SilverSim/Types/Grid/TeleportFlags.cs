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

namespace SilverSim.Types.Grid
{
    [Flags]
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public enum TeleportFlags : uint
    {
        None = 0,

        SetHomeToTarget = 1 << 0,
        SetLastToTarget = 1 << 1,
        /** <summary>Teleport request from another user</summary> */
        ViaLure = 1 << 2,
        /** <summary>Teleport request triggered by landmark</summary> */
        ViaLandmark = 1 << 3,
        /** <summary>Teleport request triggered by specified location</summary> */
        ViaLocation = 1 << 4,
        /** <summary>Teleport request triggered by home point</summary> */
        ViaHome = 1 << 5,
        /** <summary>Teleport request triggered by telehub</summary> */
        ViaTelehub = 1 << 6,
        /** <summary>Teleport request triggered by login</summary> */
        ViaLogin = 1 << 7,
        /** <summary>Teleport request triggered by border crossing</summary> */
        ViaGodlikeLure = 1 << 8,
        Godlike = 1 << 9,
        NineOneOne = 1 << 10,
        DisableCancel = 1 << 11,
        ViaRegionID = 1 << 12,
        IsFlying = 1 << 13,
        ResetHome = 1 << 14,
        ForceRedirect = 1 << 15,
        FinishedViaLure = 1 << 26,
        FinishedViaNewSim = 1 << 28,
        FinishedViaSameSim = 1 << 29,
        /** <summary>Teleport request triggered by HG teleport</summary> */
        ViaHGLogin = 1 << 30
    }

    public static class TeleportFlagsExtensionMethods
    {
        public static bool NeedsInitialPosition(this TeleportFlags flags) => (flags & (TeleportFlags.ViaLogin | TeleportFlags.ViaHGLogin | TeleportFlags.ViaLocation | TeleportFlags.ViaLandmark)) != 0;

        public static bool IsLogin(this TeleportFlags flags) => (flags & (TeleportFlags.ViaLogin | TeleportFlags.ViaHGLogin)) != 0;

        public static bool IsGodlike(this TeleportFlags flags) => (flags & TeleportFlags.Godlike) != 0;

        public static bool IsLure(this TeleportFlags flags) => (flags & TeleportFlags.ViaLure) != 0;
    }
}
