using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.LL.Core.LayerData
{
    public class LayerPatch
    {
        public int X;
        public int Y;

        public float[,] Data = new float[16,16];
    }
}
