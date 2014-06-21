using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Scene.Types.Script;
using ArribaSim.Scene.Types.Object;
using ArribaSim.Scene.Types;
using ArribaSim.Types;

namespace ArribaSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        #region Sit Targets
        void llSitTarget(Vector3 offset, Quaternion rot)
        {
            Part.SitTargetOffset = offset;
            Part.SitTargetOrientation = rot;
        }

        void llLinkSitTarget(Integer link, Vector3 offset, Quaternion rot)
        {
            ObjectPart part;
            if (link == LINK_THIS)
            {
                part = Part;
            }
            else if (!Part.Group.TryGetValue(link, out part))
            {
                return;
            }

            part.SitTargetOffset = offset;
            part.SitTargetOrientation = rot;
        }
        #endregion

        #region Sit control
        public UUID llAvatarOnSitTarget()
        {
            return llAvatarOnLinkSitTarget(LINK_THIS);
        }

        public UUID llAvatarOnLinkSitTarget(Integer link)
        {
            return UUID.Zero;
        }

        public void llForceMouselook(Integer mouselook)
        {

        }
        #endregion
    }
}
