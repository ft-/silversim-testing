﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.IO;
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

        public static byte[] ToUTF8String(this string s)
        {
            return m_UTF8NoBOM.GetBytes(s);
        }

        public static int ToUTF8StringCount(this string s)
        {
            return m_UTF8NoBOM.GetByteCount(s);
        }

        public static string FromUTF8String(this byte[] data)
        {
            return m_UTF8NoBOM.GetString(data);
        }

        public static string FromUTF8String(this byte[] data, int index, int count)
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
    }
}
