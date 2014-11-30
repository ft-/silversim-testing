/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Scene.Types.Script;

namespace SilverSim.Scripting.LSL.API.Base
{
    public partial class Base_API
    {
        [APILevel(APIFlags.LSL)]
        public const double PI = 3.14159274f;
        [APILevel(APIFlags.LSL)]
        public const double TWO_PI = 6.28318548f;
        [APILevel(APIFlags.LSL)]
        public const double PI_BY_TWO = 1.57079637f;
        [APILevel(APIFlags.LSL)]
        public const double DEG_TO_RAD = 0.01745329238f;
        [APILevel(APIFlags.LSL)]
        public const double RAD_TO_DEG = 57.29578f;
        [APILevel(APIFlags.LSL)]
        public const double SQRT2 = 1.414213538f;

        [APILevel(APIFlags.LSL)]
        public static int llAbs(ScriptInstance Instance, int v)
        {
            if(v < 0)
            {
                return -v;
            }
            else
            {
                return v;
            }
        }

        [APILevel(APIFlags.LSL)]
        public static double llAcos(ScriptInstance Instance, double v)
        {
            return Math.Acos(v);
        }

        [APILevel(APIFlags.LSL)]
        public static double llAsin(ScriptInstance Instance, double v)
        {
            return Math.Asin(v);
        }

        [APILevel(APIFlags.LSL)]
        public static double llAtan2(ScriptInstance Instance, double y, double x)
        {
            return Math.Atan2(y, x);
        }

        [APILevel(APIFlags.LSL)]
        public static double llCos(ScriptInstance Instance, double v)
        {
            return Math.Cos(v);
        }

        [APILevel(APIFlags.LSL)]
        public static double llFabs(ScriptInstance Instance, double v)
        {
            return Math.Abs(v);
        }

        [APILevel(APIFlags.LSL)]
        public static double llLog(ScriptInstance Instance, double v)
        {
            return Math.Log(v);
        }

        [APILevel(APIFlags.LSL)]
        public static double llLog10(ScriptInstance Instance, double v)
        {
            return Math.Log10(v);
        }

        [APILevel(APIFlags.LSL)]
        public static double llPow(ScriptInstance Instance, double bas, double exponent)
        {
            return Math.Pow(bas, exponent);
        }

        [APILevel(APIFlags.LSL)]
        public static double llSin(ScriptInstance Instance, double v)
        {
            return Math.Sin(v);
        }

        [APILevel(APIFlags.LSL)]
        public static double llSqrt(ScriptInstance Instance, double v)
        {
            return Math.Sqrt(v);
        }

        [APILevel(APIFlags.LSL)]
        public static double llTan(ScriptInstance Instance, double v)
        {
            return Math.Tan(v);
        }

        [APILevel(APIFlags.LSL)]
        public static double llVecDist(ScriptInstance Instance, Vector3 a, Vector3 b)
        {
            return (a - b).Length;
        }

        [APILevel(APIFlags.LSL)]
        public static double llVecMag(ScriptInstance Instance, Vector3 v)
        {
            return v.Length;
        }

        [APILevel(APIFlags.LSL)]
        public static Vector3 llVecNorm(ScriptInstance Instance, Vector3 v)
        {
            return v / v.Length;
        }

        [APILevel(APIFlags.LSL)]
        public static int llModPow(ScriptInstance Instance, int a, int b, int c)
        {
            return ((int)Math.Pow(a, b)) % c;
        }

        [APILevel(APIFlags.LSL)]
        public static Vector3 llRot2Euler(ScriptInstance Instance, Quaternion q)
        {
            double roll, pitch, yaw;

            q.GetEulerAngles(out roll, out pitch, out yaw);
            return new Vector3(roll, pitch, yaw);
        }

        [APILevel(APIFlags.LSL)]
        public static double llRot2Angle(ScriptInstance Instance, Quaternion r)
        {
            /* based on http://wiki.secondlife.com/wiki/LlRot2Angle */
            double s2 = r.Z * r.Z; // square of the s-element
            double v2 = r.X * r.X + r.Y * r.Y + r.Z * r.Z; // sum of the squares of the v-elements

            if (s2 < v2) // compare the s-component to the v-component
                return 2.0 * Math.Acos(Math.Sqrt(s2 / (s2 + v2))); // use arccos if the v-component is dominant
            if (v2 != 0) // make sure the v-component is non-zero
                return 2.0 * Math.Asin(Math.Sqrt(v2 / (s2 + v2))); // use arcsin if the s-component is dominant

            return 0.0; // argument is scaled too small to be meaningful, or it is a zero rotation, so return zer
        }

        [APILevel(APIFlags.LSL)]
        public static Vector3 llRot2Axis(ScriptInstance Instance, Quaternion q)
        {
            return llVecNorm(Instance, new Vector3(q.X, q.Y, q.Z)) * Math.Sign(q.W);
        }

        [APILevel(APIFlags.LSL)]
        public static Quaternion llAxisAngle2Rot(ScriptInstance Instance, Vector3 axis, double angle)
        {
            return Quaternion.CreateFromAxisAngle(axis, angle);
        }

        [APILevel(APIFlags.LSL)]
        public static Quaternion llEuler2Rot(ScriptInstance Instance, Vector3 v)
        {
            return Quaternion.CreateFromEulers(v);
        }

        [APILevel(APIFlags.LSL)]
        public static double llAngleBetween(ScriptInstance Instance, Quaternion a, Quaternion b)
        {   /* based on http://wiki.secondlife.com/wiki/LlAngleBetween */
            Quaternion r = b / a;
            double s2 = r.W * r.W;
            double v2 = r.X * r.X + r.Y * r.Y + r.Z * r.Z;
            if (s2 < v2)
            {
                return 2.0 * Math.Acos(Math.Sqrt(s2 / (s2 + v2)));
            }
            else if (v2 > Double.Epsilon)
            {
                return 2.0 * Math.Asin(Math.Sqrt(v2 / (s2 + v2)));
            }
            return 0f;
        }

        [APILevel(APIFlags.LSL)]
        public static Quaternion llAxes2Rot(ScriptInstance Instance, Vector3 fwd, Vector3 left, Vector3 up)
        {
            double s;
            double t = fwd.X + left.Y + up.Z + 1.0;

            if(t >= 1.0)
            {
                s = 0.5 / Math.Sqrt(t);
                return new Quaternion((left.Z - up.Y) * s, (up.X - fwd.Z) * s, (fwd.Y - left.X) * s, 0.25 / s);
            }
            else
            {
                double m = (left.Y > up.Z) ? left.Y : up.Z;

                if(m < fwd.X)
                {
                    s = Math.Sqrt(fwd.X - (left.Y + up.Z) + 1.0);
                    return new Quaternion(
                        s * 0.5,
                        (fwd.Y + left.X) * (0.5 / s),
                        (up.X + fwd.Z) * (0.5 / s),
                        (left.Z - up.Y) * (0.5 / s));
                }
                else if(m == left.Y)
                {
                    s = Math.Sqrt(left.Y - (up.Z + fwd.X) + 1.0);
                    return new Quaternion(
                        (fwd.Y + left.X) * (0.5 / s),
                        s * 0.5,
                        (left.Z + up.Y) * (0.5 / s),
                        (up.X - fwd.Z) * (0.5 / s));
                }
                else
                {
                    s = Math.Sqrt(up.Z - (fwd.X + left.Y) + 1.0);
                    return new Quaternion(
                        (up.X + fwd.Z) * (0.5 / s),
                        (left.Z + up.Y) * (0.5 / s),
                        s * 0.5,
                        (fwd.Y - left.X) * (0.5 / s));
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public static Vector3 llRot2Fwd(ScriptInstance Instance, Quaternion r)
        {
            double x, y, z, sq;
            sq = r.LengthSquared;
            if(Math.Abs(1.0 -sq)>0.000001)
            {
                sq = 1.0 / Math.Sqrt(sq);
                r.X *= sq;
                r.Y *= sq;
                r.Z *= sq;
                r.W *= sq;
            }

            x = r.X * r.X - r.Y * r.Y - r.Z * r.Z + r.W * r.W;
            y = 2 * (r.X * r.Y + r.Z * r.W);
            z = 2 * (r.X * r.Z - r.Y * r.W);
            return new Vector3(x, y, z);
        }

        [APILevel(APIFlags.LSL)]
        public static Vector3 llRot2Left(ScriptInstance Instance, Quaternion r)
        {
            double x, y, z, sq;

            sq = r.LengthSquared;
            if (Math.Abs(1.0 - sq) > 0.000001)
            {
                sq = 1.0 / Math.Sqrt(sq);
                r.X *= sq;
                r.Y *= sq;
                r.Z *= sq;
                r.W *= sq;
            }

            x = 2 * (r.X * r.Y - r.Z * r.W);
            y = -r.X * r.X + r.Y * r.Y - r.Z * r.Z + r.W * r.W;
            z = 2 * (r.X * r.W + r.Y * r.Z);
            return new Vector3(x, y, z);
        }

        [APILevel(APIFlags.LSL)]
        public static Vector3 llRot2Up(ScriptInstance Instance, Quaternion r)
        {
            double x, y, z, sq;

            sq = r.LengthSquared;
            if (Math.Abs(1.0 - sq) > 0.000001)
            {
                sq = 1.0 / Math.Sqrt(sq);
                r.X *= sq;
                r.Y *= sq;
                r.Z *= sq;
                r.W *= sq;
            }

            x = 2 * (r.X * r.Z + r.Y * r.W);
            y = 2 * (-r.X * r.W + r.Y * r.Z);
            z = -r.X * r.X - r.Y * r.Y + r.Z * r.Z + r.W * r.W;
            return new Vector3(x, y, z);
        }

        [APILevel(APIFlags.LSL)]
        public static Quaternion llRotBetween(ScriptInstance Instance, Vector3 a, Vector3 b)
        {
            return Quaternion.RotBetween(a, b);
        }

        [APILevel(APIFlags.LSL)]
        public static int llFloor(ScriptInstance Instance, double f)
        {
            return (int)Math.Floor(f);
        }

        [APILevel(APIFlags.LSL)]
        public static int llCeil(ScriptInstance Instance, double f)
        {
            return (int)Math.Ceiling(f);
        }

        [APILevel(APIFlags.LSL)]
        public static int llRound(ScriptInstance Instance, double f)
        {
            return (int)Math.Round(f, MidpointRounding.AwayFromZero);
        }

        private static Random random = new Random();
        [APILevel(APIFlags.LSL)]
        public static double llFrand(ScriptInstance Instance, double mag)
        {
            return random.NextDouble() * mag;
        }
    }
}
