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

namespace SilverSim.Scene.Types.Physics.Vehicle
{
    [Flags]
    public enum VehicleFlags : int
    {
        None = 0,
        NoDeflectionUp = 0x0001,
        LimitRollOnly = 0x0002,
        HoverWaterOnly = 0x0004,
        HoverTerrainOnly = 0x0008,
        HoverGlobalHeight = 0x0010,
        HoverUpOnly = 0x0020,
        LimitMotorUp = 0x0040,
        MouselookSteer = 0x0080,
        MouselookBank = 0x0100,
        CameraDecoupled = 0x0200
    }
}
