/*

ArribaSim is distributed under the terms of the
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

namespace ArribaSim.Scene.Types.Script.Events
{
    public struct RuntimePermissionsEvent : IScriptEvent
    {
        public enum RuntimePermissions : int
        {
            Debit = 0x2,
            TakeControls = 0x4,
            TriggerAnimation = 0x10,
            Attach = 0x20,
            ChangeLinks = 0x80,
            TrackCamera = 0x400,
            ControlCamera = 0x800,
            Teleport = 0x1000,
            SilentEstateManagement = 0x4000,
            OverrideAnimations = 0x8000,
            ReturnObjects = 0x10000
        }

        public int Permissions;
    }
}
