using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scene.Types.Object
{
    [Flags]
    public enum DetectedTypeFlags
    {
        Agent = 1,
        Active = 2,
        Passive = 4,
        Scripted = 8
    }
}
