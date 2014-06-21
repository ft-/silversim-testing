using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;

namespace ArribaSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        #region Texture Animation
        void llSetLinkTextureAnim(Integer link, Integer mode, Integer face, Integer sizeX, Integer sizeY, Real start, Real length, Real rate)
        {

        }

        void llSetTextureAnim(Integer mode, Integer face, Integer sizeX, Integer sizeY, Real start, Real length, Real rate)
        {
            llSetLinkTextureAnim(new Integer(LINK_THIS), mode, face, sizeX, sizeY, start, length, rate);
        }
        #endregion
    }
}
