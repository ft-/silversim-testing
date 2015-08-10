// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Types.Script
{
    [Flags]
    public enum ScriptPermissions : uint
    {
        None = 0,
        Debit = 0x00000002,
        TakeControls = 0x00000004,
        RemapControls = 0x00000008, /* no function */
        TriggerAnimation = 0x00000010,
        Attach = 0x00000020,
        ReleaseOwnership = 0x00000040, /* no function */
        ChangeLinks = 0x00000080,
        ChangeJoints = 0x00000100, /* no function */
        ChangePermissions = 0x00000200, /* no function */
        TrackCamera = 0x00000400,
        ControlCamera = 0x00000800,
        Teleport = 0x00001000,
        Experience = 0x0002000,
        SilentEstateManagement = 0x00004000,
        OverrideAnimations = 0x00008000,
        ReturnObjects = 0x00010000,
        All = 0xFFFFFFFF
    }
}
