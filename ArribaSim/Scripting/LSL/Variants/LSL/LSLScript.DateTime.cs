using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;

namespace ArribaSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public Integer GetUnixTime()
        {
            return new Integer((int)Date.GetUnixTime());
        }
    }
}
