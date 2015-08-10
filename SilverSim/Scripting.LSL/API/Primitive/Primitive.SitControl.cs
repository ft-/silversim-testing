// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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

        [APILevel(APIFlags.ASSL)]
        public AnArray asGetSitTarget(ScriptInstance Instance)
        {
            AnArray res = new AnArray();
            lock(Instance)
            {
                res.Add(Instance.Part.SitTargetOffset);
                res.Add(Instance.Part.SitTargetOrientation);
            }
            return res;
        }

        [APILevel(APIFlags.ASSL)]
        public AnArray asGetLinkSitTarget(ScriptInstance Instance, int link)
        {
            ObjectPart part;
            AnArray res = new AnArray();
            lock (Instance)
            {
                if (link == LINK_THIS)
                {
                    part = Instance.Part;
                }
                else if (!Instance.Part.ObjectGroup.TryGetValue(link, out part))
                {
                    return res;
                }

                res.Add(part.SitTargetOffset);
                res.Add(part.SitTargetOrientation);
            }
            return res;
        }
        #endregion

        #region Sit control
        [APILevel(APIFlags.LSL)]
        public LSLKey llAvatarOnSitTarget(ScriptInstance Instance)
        {
            return llAvatarOnLinkSitTarget(Instance, LINK_THIS);
        }

        [APILevel(APIFlags.LSL)]
        public LSLKey llAvatarOnLinkSitTarget(ScriptInstance Instance, int link)
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
        public void llUnSit(ScriptInstance Instance, LSLKey id)
        {
#warning Implement llUnSit(UUID)
        }
        #endregion
    }
}
