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
        All = 0xFFFFFFFF,

        ExperienceGrantedPermissions = 
            Teleport | 
            ControlCamera | 
            TrackCamera | 
            TriggerAnimation | 
            TakeControls | 
            Attach
    }
}
