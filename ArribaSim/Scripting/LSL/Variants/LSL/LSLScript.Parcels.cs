﻿/*

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;

namespace ArribaSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public AnArray llGetParcelDetails(Vector3 pos, AnArray param)
        {
            return new AnArray();
        }

        public int llGetParcelFlags(Vector3 pos)
        {
            return 0;
        }

        public int llGetParcelMaxPrims(Vector3 pos, int sim_wide)
        {
            return 0;
        }

        public string llGetParcelMusicURL()
        {
            return string.Empty;
        }

        public void llSetParcelMusicURL(string url)
        {

        }

        public int llReturnObjectsByID(AnArray objects)
        {
            return 0;
        }

        public int llReturnObjectsByOwner(UUID owner, int scope)
        {
            return 0;
        }

        public UUID llGetLandOwnerAt(Vector3 pos)
        {
            return UUID.Zero;
        }

        public int llGetParcelPrimCount(Vector3 pos, int category, int sim_wide)
        {
            return 0;
        }

        public AnArray llGetParcelPrimOwners(Vector3 pos)
        {
            return new AnArray();
        }
    }
}
