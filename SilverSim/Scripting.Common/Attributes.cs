using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scripting.Common
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CompilerUsesRunAndCollectMode : Attribute
    {
        public CompilerUsesRunAndCollectMode()
        {

        }
    }
}
