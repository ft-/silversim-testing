// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
        public static bool NeedsInitialPosition(this TeleportFlags flags)
        {
            return (flags & (TeleportFlags.ViaLogin | TeleportFlags.ViaHGLogin | TeleportFlags.ViaLocation | TeleportFlags.ViaLandmark)) != 0;
        }

        public static bool IsLogin(this TeleportFlags flags)
        {
            return (flags & (TeleportFlags.ViaLogin | TeleportFlags.ViaHGLogin)) != 0;
        }

        public static bool IsGodlike(this TeleportFlags flags)
        {
            return (flags & TeleportFlags.Godlike) != 0;
        }

        public static bool IsLure(this TeleportFlags flags)
        {
            return (flags & TeleportFlags.ViaLure) != 0;
        }
    }
}
