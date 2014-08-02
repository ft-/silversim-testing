/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using System;

namespace SilverSim.Types.Grid
{
    [Flags]
    public enum TeleportFlags : uint
    {
        None = 0,

        SetHomeToTarget = 1 << 0,
        SetLastToTarget = 1 << 1,
        ViaLure = 1 << 2,
        ViaLandmark = 1 << 3,
        ViaLocation = 1 << 4,
        ViaHome = 1 << 5,
        ViaTelehub = 1 << 6,
        ViaLogin = 1 << 7,
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
        ViaHGLogin = 1 << 30
    }
}
