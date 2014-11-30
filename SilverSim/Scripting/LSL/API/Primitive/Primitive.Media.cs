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

using SilverSim.Scene.Types.Script;
using SilverSim.Types;

namespace SilverSim.Scripting.LSL.API.Primitive
{
    public partial class Primitive_API
    {
        [APILevel(APIFlags.LSL)]
        public static int llClearLinkMedia(ScriptInstance Instance, int link, int face)
        {
#warning Implement llClearLinkMedia(int, int)
            return 0;
        }

        [APILevel(APIFlags.LSL)]
        public static int llClearPrimMedia(ScriptInstance Instance, int face)
        {
            return llClearLinkMedia(Instance, LINK_THIS, face);
        }

        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_ALT_IMAGE_ENABLE = 0;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_CONTROLS = 1;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_CURRENT_URL = 2;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_HOME_URL = 3;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_AUTO_LOOP = 4;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_AUTO_PLAY = 5;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_AUTO_SCALE = 6;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_AUTO_ZOOM = 7;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_FIRST_CLICK_INTERACT = 8;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_WIDTH_PIXELS = 9;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_HEIGHT_PIXELS = 10;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_WHITELIST_ENABLE = 11;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_WHITELIST = 12;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_PERMS_INTERACT = 13;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_PERMS_CONTROL = 14;

        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_PERM_NONE = 0x0;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_PERM_OWNER = 0x1;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_PERM_GROUP = 0x2;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_PERM_ANYONE = 0x4;

        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_CONTROLS_STANDARD = 0;
        [APILevel(APIFlags.LSL)]
        public const int PRIM_MEDIA_CONTROLS_MINI = 1;

        [APILevel(APIFlags.LSL)]
        public const int LSL_STATUS_OK = 0;
        [APILevel(APIFlags.LSL)]
        public const int LSL_STATUS_MALFORMED_PARAMS = 1000;
        [APILevel(APIFlags.LSL)]
        public const int LSL_STATUS_TYPE_MISMATCH = 1001;
        [APILevel(APIFlags.LSL)]
        public const int LSL_STATUS_BOUNDS_ERROR = 1002;
        [APILevel(APIFlags.LSL)]
        public const int LSL_STATUS_NOT_FOUND = 1003;
        [APILevel(APIFlags.LSL)]
        public const int LSL_STATUS_NOT_SUPPORTED = 1004;
        [APILevel(APIFlags.LSL)]
        public const int LSL_STATUS_INTERNAL_ERROR = 1999;
        [APILevel(APIFlags.LSL)]
        public const int LSL_STATUS_WHITELIST_FAILED = 2001;

        [APILevel(APIFlags.LSL)]
        public static AnArray llGetPrimMediaParams(ScriptInstance Instance, int face, AnArray param)
        {
#warning Implement llGetPrimMediaParams(int, AnArray)
            return new AnArray();
        }

        [APILevel(APIFlags.LSL)]
        public const int STATUS_OK = 0;
        [APILevel(APIFlags.LSL)]
        public const int STATUS_MALFORMED_PARAMS = 1000;
        [APILevel(APIFlags.LSL)]
        public const int STATUS_TYPE_MISMATCH = 1001;
        [APILevel(APIFlags.LSL)]
        public const int STATUS_BOUNDS_ERROR = 1002;
        [APILevel(APIFlags.LSL)]
        public const int STATUS_NOT_FOUND = 1003;
        [APILevel(APIFlags.LSL)]
        public const int STATUS_NOT_SUPPORTED = 1004;
        [APILevel(APIFlags.LSL)]
        public const int STATUS_INTERNAL_ERROR = 1999;
        [APILevel(APIFlags.LSL)]
        public const int STATUS_WHITELIST_FAILED = 2001;

        [APILevel(APIFlags.LSL)]
        public static int llSetLinkMedia(ScriptInstance Instance, int link, int face, AnArray param)
        {
#warning Implement llSetLinkMedia(int, int, AnArray)
            return 0;
        }

        [APILevel(APIFlags.LSL)]
        public static int llSetPrimMediaParams(ScriptInstance Instance, int face, AnArray param)
        {
            return llSetLinkMedia(Instance, LINK_THIS, face, param);
        }
    }
}
