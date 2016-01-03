// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace SilverSim.Types
{
    public static class TypeExtensionMethods
    {
        public static double Lerp(this double a, double b, double u)
        {
            return a + ((b - a) * u);
        }

        public static Vector3 ClampElements(this Vector3 val, double min, double max)
        {
            return new Vector3(val.X.Clamp(min, max), val.Y.Clamp(min, max), val.Z.Clamp(min, max));
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

        public static bool IsFinite(this double value)
        {
            return !(Double.IsNaN(value) || Double.IsInfinity(value));
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

        public static Vector3 AgentLookAt(this Quaternion quat)
        {
            double roll;
            double pitch;
            double yaw;
            quat.GetEulerAngles(out roll, out pitch, out yaw);
            return new Vector3(Math.Cos(yaw), Math.Sin(yaw), 0);
        }

        public static Quaternion AgentLookAtToQuaternion(this Vector3 lookat)
        {
            double yaw = Math.Atan2(lookat.Y, lookat.X);
            return Quaternion.CreateFromEulers(0, 0, yaw);
        }

        static readonly UTF8Encoding m_UTF8NoBOM = new UTF8Encoding(false);

        public static byte[] ToUTF8Bytes(this string s)
        {
            return m_UTF8NoBOM.GetBytes(s);
        }

        public static int ToUTF8ByteCount(this string s)
        {
            return m_UTF8NoBOM.GetByteCount(s);
        }

        public static string FromUTF8Bytes(this byte[] data)
        {
            return m_UTF8NoBOM.GetString(data);
        }

        public static string FromUTF8Bytes(this byte[] data, int index, int count)
        {
            return m_UTF8NoBOM.GetString(data, index, count);
        }

        public static XmlTextWriter UTF8XmlTextWriter(this Stream s)
        {
            return new XmlTextWriter(s, m_UTF8NoBOM);
        }

        public static StreamReader UTF8StreamReader(this Stream s)
        {
            return new StreamReader(s, m_UTF8NoBOM);
        }

        public static StreamWriter UTF8StreamWriter(this Stream s)
        {
            return new StreamWriter(s, m_UTF8NoBOM);
        }

        public static byte[] FromHexStringToByteArray(this string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static string ToHexString(this byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", string.Empty);
        }

        public static string TrimToMaxLength(this string s, int length)
        {
            return length < s.Length ? 
                (
                    length < 0 ? 
                    string.Empty : 
                    s.Substring(0, length)
                ) : 
                s;
        }
    }
}
