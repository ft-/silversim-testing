using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Scene.Types.Script;
using ArribaSim.Types;

namespace ArribaSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        #region LSL Constants
        public readonly Integer PUBLIC_CHANNEL = new Integer(0);
        public readonly Integer DEBUG_CHANNEL = new Integer(0x7FFFFFFF);

        public readonly Integer LINK_ROOT = new Integer(1);
        public readonly Integer LINK_SET = new Integer(-1);
        public readonly Integer LINK_ALL_OTHERS = new Integer(-2);
        public readonly Integer LINK_ALL_CHILDREN = new Integer(-3);
        public readonly Integer LINK_THIS = new Integer(-4);

        public readonly Integer PRIM_OMEGA = new Integer(32);
        #endregion
    }
}
