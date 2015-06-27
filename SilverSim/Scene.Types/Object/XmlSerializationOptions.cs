using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scene.Types.Object
{
    [Flags]
    public enum XmlSerializationOptions
    {
        None = 0,
        WriteOwnerInfo = 1,
        AdjustForNextOwner = 2,
        WriteXml2 = 4
    }
}
