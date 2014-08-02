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

namespace SilverSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public int llClearLinkMedia(int link, int face)
        {
            return 0;
        }

        public int llClearPrimMedia(int face)
        {
            return 0;
        }

        public const int PRIM_MEDIA_ALT_IMAGE_ENABLE = 0;
        public const int PRIM_MEDIA_CONTROLS = 1;
        public const int PRIM_MEDIA_CURRENT_URL = 2;
        public const int PRIM_MEDIA_HOME_URL = 3;
        public const int PRIM_MEDIA_AUTO_LOOP = 4;
        public const int PRIM_MEDIA_AUTO_PLAY = 5;
        public const int PRIM_MEDIA_AUTO_SCALE = 6;
        public const int PRIM_MEDIA_AUTO_ZOOM = 7;
        public const int PRIM_MEDIA_FIRST_CLICK_INTERACT = 8;
        public const int PRIM_MEDIA_WIDTH_PIXELS = 9;
        public const int PRIM_MEDIA_HEIGHT_PIXELS = 10;
        public const int PRIM_MEDIA_WHITELIST_ENABLE = 11;
        public const int PRIM_MEDIA_WHITELIST = 12;
        public const int PRIM_MEDIA_PERMS_INTERACT = 13;
        public const int PRIM_MEDIA_PERMS_CONTROL = 14;

        public const int PRIM_MEDIA_PERM_NONE = 0x0;
        public const int PRIM_MEDIA_PERM_OWNER = 0x1;
        public const int PRIM_MEDIA_PERM_GROUP = 0x2;
        public const int PRIM_MEDIA_PERM_ANYONE = 0x4;

        public const int PRIM_MEDIA_CONTROLS_STANDARD = 0;
        public const int PRIM_MEDIA_CONTROLS_MINI = 1;

        public AnArray llGetPrimMediaParams(int face, AnArray param)
        {
            return new AnArray();
        }

        public const int STATUS_OK = 0;
        public const int STATUS_MALFORMED_PARAMS = 1000;
        public const int STATUS_TYPE_MISMATCH = 1001;
        public const int STATUS_BOUNDS_ERROR = 1002;
        public const int STATUS_NOT_FOUND = 1003;
        public const int STATUS_NOT_SUPPORTED = 1004;
        public const int STATUS_INTERNAL_ERROR = 1999;
        public const int STATUS_WHITELIST_FAILED = 2001;

        public int llSetLinkMedia(int link, int face, AnArray param)
        {
            return 0;
        }

        public int llSetPrimMediaParams(int face, AnArray param)
        {
            return llSetLinkMedia(LINK_THIS, face, param);
        }
    }
}
