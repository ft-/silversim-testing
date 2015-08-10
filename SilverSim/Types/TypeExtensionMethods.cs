// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Types
{
    public static class TypeExtensionMethods
    {
        public static double Lerp(this double a, double b, double u)
        {
            return a + ((b - a) * u);
        }

        public static double Clamp(this double val, double min, double max)
        {
            if(val < min)
            {
                return min;
            }
            else if(val > max)
            {
                return max;
            }
            else
            {
                return val;
            }
        }

        public static int Clamp(this int val, int min, int max)
        {
            if (val < min)
            {
                return min;
            }
            else if(val > max)
            {
                return max;
            }
            else
            {
                return val;
            }
        }

        public static double Clamp(this Real val, double min, double max)
        {
            if (val < min)
            {
                return min;
            }
            else if (val > max)
            {
                return max;
            }
            else
            {
                return val;
            }
        }
    }
}
