// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scene.Types.Script;
using System.Reflection;

namespace SilverSim.Scripting.LSL.API.Primitive
{
    public partial class Primitive_API
    {
        /// <summary>
        /// Set parameters for light projection in host prim 
        /// </summary>
        [APILevel(APIFlags.OSSL)]
        public void osSetProjectionParams(ScriptInstance Instance, int projection, LSLKey texture, double fov, double focus, double amb)
        {
            osSetLinkProjectionParams(Instance, LINK_THIS, projection, texture, fov, focus, amb);
        }

        [APILevel(APIFlags.OSSL)]
        public void osSetLinkProjectionParams(ScriptInstance Instance, int link, int projection, LSLKey texture, double fov, double focus, double amb)
        {
            ObjectPart.ProjectionParam p = new ObjectPart.ProjectionParam();
            p.IsProjecting = projection != 0;
            p.ProjectionTextureID = texture;
            p.ProjectionFOV = fov;
            p.ProjectionFocus = focus;
            p.ProjectionAmbience = amb;

            foreach(ObjectPart part in GetLinkTargets(Instance, link))
            {
                part.Projection = p;
            }
        }

        /// <summary>
        /// Set parameters for light projection with uuid of target prim
        /// </summary>
        [APILevel(APIFlags.OSSL)]
        public void osSetProjectionParams(ScriptInstance Instance, LSLKey prim, int projection, LSLKey texture, double fov, double focus, double amb)
        {
            lock (Instance)
            {
                if (UUID.Zero != prim)
                {
                    Instance.CheckThreatLevel(MethodBase.GetCurrentMethod().Name, ScriptInstance.ThreatLevelType.High);
                }

                ObjectPart part;
                if (prim == UUID.Zero)
                {
                    part = Instance.Part;
                }
                else
                {
                    try
                    {
                        part = Instance.Part.ObjectGroup.Scene.Primitives[prim];
                    }
                    catch
                    {
                        return;
                    }
                }

                ObjectPart.ProjectionParam p = new ObjectPart.ProjectionParam();
                p.IsProjecting = projection != 0;
                p.ProjectionTextureID = texture;
                p.ProjectionFOV = fov;
                p.ProjectionFocus = focus;
                p.ProjectionAmbience = amb;
                part.Projection = p;
            }
        }

    }
}
