using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;

namespace ArribaSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public void llLinkParticleSystem(Integer link, AnArray rules)
        {

        }

        public void llParticleSystem(AnArray rules)
        {
            llLinkParticleSystem(LINK_THIS, rules);
        }
    }
}
