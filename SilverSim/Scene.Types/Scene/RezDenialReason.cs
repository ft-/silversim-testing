using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilverSim.Scene.Types.Scene
{
    public enum RezDenialReason
    {
        Blacklisted = 1,
        ParcelNotAllowed = 2,
        ParcelNotFound = 3,
    }
}
