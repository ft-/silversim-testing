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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Object;

namespace SilverSim.Scripting.LSL.API.Primitive
{
    public partial class Primitive_API
    {
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_PITCH = 0;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS_OFFSET = 1;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS_OFFSET_X = 2;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS_OFFSET_Y = 3;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS_OFFSET_Z = 4;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_POSITION_LAG = 5;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS_LAG = 6;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_DISTANCE = 7;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_BEHINDNESS_ANGLE = 8;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_BEHINDNESS_LAG = 9;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_POSITION_THRESHOLD = 10;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS_THRESHOLD = 11;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_ACTIVE = 12;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_POSITION = 13;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_POSITION_X = 14;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_POSITION_Y = 15;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_POSITION_Z = 16;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS = 17;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS_X = 18;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS_Y = 19;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS_Z = 20;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_POSITION_LOCKED = 21;
        [APILevel(APIFlags.LSL)]
        public const int CAMERA_FOCUS_LOCKED = 22;

        [APILevel(APIFlags.LSL)]
        public void llSetCameraAtOffset(ScriptInstance Instance, Vector3 offset)
        {
        }

        [APILevel(APIFlags.LSL)]
        public void llSetLinkCamera(ScriptInstance Instance, int link, Vector3 eye, Vector3 at)
        {

        }

        [APILevel(APIFlags.LSL)]
        public void llSetCameraOffset(ScriptInstance Instance, Vector3 offset)
        {

        }

        [APILevel(APIFlags.LSL)]
        public void llClearCameraParams(ScriptInstance Instance)
        {
            lock (Instance)
            {
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = Instance.Item.PermsGranter;
                if (grantinfo.PermsGranter != UUI.Unknown && (grantinfo.PermsMask & ScriptPermissions.ControlCamera) != 0)
                {

                }
            }

        }

        [APILevel(APIFlags.LSL)]
        public void llSetCameraParams(ScriptInstance Instance, AnArray rules)
        {
            lock (Instance)
            {
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = Instance.Item.PermsGranter;
                if (grantinfo.PermsGranter != UUI.Unknown && (grantinfo.PermsMask & ScriptPermissions.ControlCamera) != 0)
                {

                }
            }

        }

        [APILevel(APIFlags.LSL)]
        public Vector3 llGetCameraPos(ScriptInstance Instance)
        {
            ObjectPartInventoryItem.PermsGranterInfo grantinfo = Instance.Item.PermsGranter;
            if (grantinfo.PermsGranter != UUI.Unknown && (grantinfo.PermsMask & ScriptPermissions.TrackCamera) != 0)
            {

            }
            return Vector3.Zero;
        }

        [APILevel(APIFlags.LSL)]
        public Quaternion llGetCameraRot(ScriptInstance Instance)
        {
            ObjectPartInventoryItem.PermsGranterInfo grantinfo = Instance.Item.PermsGranter;
            if (grantinfo.PermsGranter != UUI.Unknown && (grantinfo.PermsMask & ScriptPermissions.TrackCamera) != 0)
            {

            }
            return Quaternion.Identity;
        }
    }
}
