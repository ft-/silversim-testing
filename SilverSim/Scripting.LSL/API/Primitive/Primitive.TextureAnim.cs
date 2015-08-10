// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;

namespace SilverSim.Scripting.LSL.API.Primitive
{
    public partial class Primitive_API
    {
        #region Texture Animation
        [APILevel(APIFlags.LSL)]
        public void llSetLinkTextureAnim(ScriptInstance Instance, int link, int mode, int face, int sizeX, int sizeY, double start, double length, double rate)
        {
            TextureAnimationEntry te = new TextureAnimationEntry();
            te.Flags = (TextureAnimationEntry.TextureAnimMode)mode;
            te.Face = (sbyte)face;
            te.SizeX = (byte)sizeX;
            te.SizeY = (byte)sizeY;
            te.Start = (float)start;
            te.Length = (float)length;
            te.Rate = (float)rate;

            foreach(ObjectPart part in GetLinkTargets(Instance, link))
            {
                part.TextureAnimation = te;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetTextureAnim(ScriptInstance Instance, int mode, int face, int sizeX, int sizeY, double start, double length, double rate)
        {
            llSetLinkTextureAnim(Instance, LINK_THIS, mode, face, sizeX, sizeY, start, length, rate);
        }
        #endregion
    }
}
