using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Types.Primitive
{
    public enum PrimitiveCode : byte
    {
        None = 0,
        Prim = 9,
        Avatar = 47,
        Grass = 95,
        NewTree = 111,
        ParticleSystem = 143,
        Tree = 255
    }
}
