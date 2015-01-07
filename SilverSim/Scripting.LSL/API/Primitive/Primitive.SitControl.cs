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
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types;
using SilverSim.Types;

namespace SilverSim.Scripting.LSL.API.Primitive
{
    public partial class Primitive_API
    {
        #region Sit Targets
        [APILevel(APIFlags.LSL)]
        public void llSitTarget(ScriptInstance Instance, Vector3 offset, Quaternion rot)
        {
            lock (Instance)
            {
                Instance.Part.SitTargetOffset = offset;
                Instance.Part.SitTargetOrientation = rot;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llLinkSitTarget(ScriptInstance Instance, int link, Vector3 offset, Quaternion rot)
        {
            ObjectPart part;
            lock (Instance)
            {
                if (link == LINK_THIS)
                {
                    part = Instance.Part;
                }
                else if (!Instance.Part.ObjectGroup.TryGetValue(link, out part))
                {
                    return;
                }

                part.SitTargetOffset = offset;
                part.SitTargetOrientation = rot;
            }
        }
        #endregion

        #region Sit control
        [APILevel(APIFlags.LSL)]
        public UUID llAvatarOnSitTarget(ScriptInstance Instance)
        {
            return llAvatarOnLinkSitTarget(Instance, LINK_THIS);
        }

        [APILevel(APIFlags.LSL)]
        public UUID llAvatarOnLinkSitTarget(ScriptInstance Instance, int link)
        {
#warning Implement llAvatarOnLinkSitTarget(int)
            return UUID.Zero;
        }

        [APILevel(APIFlags.LSL)]
        public void llForceMouselook(ScriptInstance Instance, int mouselook)
        {
#warning Implement llForceMouselook(int)
        }

        [APILevel(APIFlags.LSL)]
        public void llUnSit(ScriptInstance Instance, UUID id)
        {
#warning Implement llUnSit(UUID)
        }
        #endregion
    }
}
