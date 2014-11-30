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

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Scene.Types.Script;
using SilverSim.Types.Primitive;

namespace SilverSim.Scripting.LSL.API.Primitive
{
    public partial class Primitive_API
    {
        #region Faces
        [APILevel(APIFlags.LSL)]
        public static int llGetNumberOfSides(ScriptInstance Instance)
        {
            return Instance.Part.NumberOfSides;
        }

        [APILevel(APIFlags.LSL)]
        public static double llGetAlpha(ScriptInstance Instance, int face)
        {
            lock (Instance)
            {
                try
                {
                    TextureEntryFace te = Instance.Part.TextureEntry[(uint)face];
                    return te.TextureColor.A;
                }
                catch
                {
                    return 0f;
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public static void llSetAlpha(ScriptInstance Instance, double alpha, int faces)
        {
            llSetLinkAlpha(Instance, LINK_THIS, alpha, faces);
        }

        [APILevel(APIFlags.LSL)]
        public static void llSetLinkAlpha(ScriptInstance Instance, int link, double alpha, int face)
        {
            if (alpha < 0) alpha = 0;
            if (alpha > 1) alpha = 1;

            if (face == ALL_SIDES)
            {
                foreach (ObjectPart part in GetLinkTargets(Instance, link))
                {
                    TextureEntry te = part.TextureEntry;
                    for (face = 0; face < te.FaceTextures.Length; ++face)
                    {
                        te.FaceTextures[face].TextureColor.A = alpha;
                    }
                    part.TextureEntry = te;
                }
            }
            else
            {
                foreach (ObjectPart part in GetLinkTargets(Instance, link))
                {
                    try
                    {
                        TextureEntry te = part.TextureEntry;
                        te.FaceTextures[face].TextureColor.A = alpha;
                        part.TextureEntry = te;
                    }
                    catch
                    {

                    }
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public static void llSetTexture(ScriptInstance Instance, string texture, int face)
        {
            llSetLinkTexture(Instance, LINK_THIS, texture, face);
        }

        [APILevel(APIFlags.LSL)]
        public static void llSetLinkTexture(ScriptInstance Instance, int link, string texture, int face)
        {
            UUID textureID = getTextureAssetID(Instance, texture);

            if (face == ALL_SIDES)
            {
                foreach (ObjectPart part in GetLinkTargets(Instance, link))
                {
                    TextureEntry te = part.TextureEntry;
                    for (face = 0; face < te.FaceTextures.Length; ++face)
                    {
                        te.FaceTextures[face].TextureID = textureID;
                    }
                    part.TextureEntry = te;
                }
            }
            else
            {
                foreach (ObjectPart part in GetLinkTargets(Instance, link))
                {
                    try
                    {
                        TextureEntry te = part.TextureEntry;
                        te.FaceTextures[face].TextureID = textureID;
                        part.TextureEntry = te;
                    }
                    catch
                    {

                    }
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public static Vector3 llGetColor(ScriptInstance Instance, int face)
        {
            return Vector3.Zero;
        }

        [APILevel(APIFlags.LSL)]
        public static void llSetColor(ScriptInstance Instance, Vector3 color, int face)
        {
            llSetLinkColor(Instance, LINK_THIS, color, face);
        }

        [APILevel(APIFlags.LSL)]
        public static void llSetLinkColor(ScriptInstance Instance, int link, Vector3 color, int face)
        {
            if (color.X < 0) color.X = 0;
            if (color.X > 1) color.X = 1;
            if (color.Y < 0) color.Y = 0;
            if (color.Y > 1) color.Y = 1;
            if (color.Z < 0) color.Z = 0;
            if (color.Z > 1) color.Z = 1;

            if (face == ALL_SIDES)
            {
                foreach (ObjectPart part in GetLinkTargets(Instance, link))
                {
                    TextureEntry te = part.TextureEntry;
                    for (face = 0; face < te.FaceTextures.Length; ++face)
                    {
                        te.FaceTextures[face].TextureColor.R = color.X;
                        te.FaceTextures[face].TextureColor.G = color.Y;
                        te.FaceTextures[face].TextureColor.B = color.Z;
                    }
                    part.TextureEntry = te;
                }
            }
            else
            {
                foreach (ObjectPart part in GetLinkTargets(Instance, link))
                {
                    try
                    {
                        TextureEntry te = part.TextureEntry;
                        te.FaceTextures[face].TextureColor.R = color.X;
                        te.FaceTextures[face].TextureColor.G = color.Y;
                        te.FaceTextures[face].TextureColor.B = color.Z;
                        part.TextureEntry = te;
                    }
                    catch
                    {

                    }
                }
            }
        }
        #endregion
    }
}
