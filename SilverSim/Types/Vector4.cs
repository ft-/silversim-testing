// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace SilverSim.Types
{
    public struct Vector4 : IComparable<Vector4>, IEquatable<Vector4>
    {
        public double X;
        public double Y;
        public double Z;
        public double W;

        #region Constructors

        public Vector4(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vector4(Vector3 v, double w)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
            W = w;
        }

        public Vector4(double v)
        {
            X = v;
            Y = v;
            Z = v;
            W = v;
        }

        public Vector4(byte[] byteArray, int pos)
        {
            X = Y = Z = W = 0f;
            FromBytes(byteArray, pos);
        }

        public Vector4(Vector4 value)
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
            W = value.W;
        }

        #endregion Constructors

        #region Public Methods

        public double Length()
        {
            return Math.Sqrt(DistanceSquared(this, Zero));
        }

        public double LengthSquared()
        {
            return DistanceSquared(this, Zero);
        }

        public void Normalize()
        {
            this = Normalize(this);
        }

        public bool ApproxEquals(Vector4 vec, double tolerance)
        {
            Vector4 diff = this - vec;
            return (diff.LengthSquared() <= tolerance * tolerance);
        }

        public int CompareTo(Vector4 vector)
        {
            return Length().CompareTo(vector.Length());
        }

        private static bool isFinite(double value)
        {
            return !(Double.IsNaN(value) || Double.IsInfinity(value));
        }
        public bool IsFinite()
        {
            return (isFinite(X) && isFinite(Y) && isFinite(Z) && isFinite(W));
        }

        public void FromBytes(byte[] byteArray, int pos)
        {
            if (!BitConverter.IsLittleEndian)
            {
                // Big endian architecture
                byte[] conversionBuffer = new byte[16];

                Buffer.BlockCopy(byteArray, pos, conversionBuffer, 0, 16);

                Array.Reverse(conversionBuffer, 0, 4);
                Array.Reverse(conversionBuffer, 4, 4);
                Array.Reverse(conversionBuffer, 8, 4);
                Array.Reverse(conversionBuffer, 12, 4);

                X = BitConverter.ToSingle(conversionBuffer, 0);
                Y = BitConverter.ToSingle(conversionBuffer, 4);
                Z = BitConverter.ToSingle(conversionBuffer, 8);
                W = BitConverter.ToSingle(conversionBuffer, 12);
            }
            else
            {
                // Little endian architecture
                X = BitConverter.ToSingle(byteArray, pos);
                Y = BitConverter.ToSingle(byteArray, pos + 4);
                Z = BitConverter.ToSingle(byteArray, pos + 8);
                W = BitConverter.ToSingle(byteArray, pos + 12);
            }
        }

        /// <summary>
        /// Returns the raw bytes for this vector
        /// </summary>
        /// <returns>A 16 byte array containing X, Y, Z, and W</returns>
        public byte[] GetBytes()
        {
            byte[] byteArray = new byte[16];
            ToBytes(byteArray, 0);
            return byteArray;
        }

        /// <summary>
        /// Writes the raw bytes for this vector to a byte array
        /// </summary>
        /// <param name="dest">Destination byte array</param>
        /// <param name="pos">Position in the destination array to start
        /// writing. Must be at least 16 bytes before the end of the array</param>
        public void ToBytes(byte[] dest, int pos)
        {
            Buffer.BlockCopy(BitConverter.GetBytes((float)X), 0, dest, pos + 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes((float)Y), 0, dest, pos + 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes((float)Z), 0, dest, pos + 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes((float)W), 0, dest, pos + 12, 4);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(dest, pos + 0, 4);
                Array.Reverse(dest, pos + 4, 4);
                Array.Reverse(dest, pos + 8, 4);
                Array.Reverse(dest, pos + 12, 4);
            }
        }

        #endregion Public Methods

        #region Static Methods

        public static Vector3 Lerp(Vector3 lhs, Vector3 rhs, double c)
        {
            return lhs + (rhs - lhs) * c;
        }

        public static Vector4 Add(Vector4 value1, Vector4 value2)
        {
            value1.W += value2.W;
            value1.X += value2.X;
            value1.Y += value2.Y;
            value1.Z += value2.Z;
            return value1;
        }

        private static double clamp(double val, double min, double max)
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
        public static Vector4 Clamp(Vector4 value1, Vector4 min, Vector4 max)
        {
            return new Vector4(
                clamp(value1.X, min.X, max.X),
                clamp(value1.Y, min.Y, max.Y),
                clamp(value1.Z, min.Z, max.Z),
                clamp(value1.W, min.W, max.W));
        }

        public static double Distance(Vector4 value1, Vector4 value2)
        {
            return Math.Sqrt(DistanceSquared(value1, value2));
        }

        public static double DistanceSquared(Vector4 value1, Vector4 value2)
        {
            return
                (value1.W - value2.W) * (value1.W - value2.W) +
                (value1.X - value2.X) * (value1.X - value2.X) +
                (value1.Y - value2.Y) * (value1.Y - value2.Y) +
                (value1.Z - value2.Z) * (value1.Z - value2.Z);
        }

        public static Vector4 Divide(Vector4 value1, Vector4 value2)
        {
            value1.W /= value2.W;
            value1.X /= value2.X;
            value1.Y /= value2.Y;
            value1.Z /= value2.Z;
            return value1;
        }

        public static Vector4 Divide(Vector4 value1, float divider)
        {
            float factor = 1f / divider;
            value1.W *= factor;
            value1.X *= factor;
            value1.Y *= factor;
            value1.Z *= factor;
            return value1;
        }

        public static double Dot(Vector4 vector1, Vector4 vector2)
        {
            return vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z + vector1.W * vector2.W;
        }

        public static Vector4 Max(Vector4 value1, Vector4 value2)
        {
            return new Vector4(
               Math.Max(value1.X, value2.X),
               Math.Max(value1.Y, value2.Y),
               Math.Max(value1.Z, value2.Z),
               Math.Max(value1.W, value2.W));
        }

        public static Vector4 Min(Vector4 value1, Vector4 value2)
        {
            return new Vector4(
               Math.Min(value1.X, value2.X),
               Math.Min(value1.Y, value2.Y),
               Math.Min(value1.Z, value2.Z),
               Math.Min(value1.W, value2.W));
        }

        public static Vector4 Multiply(Vector4 value1, Vector4 value2)
        {
            value1.W *= value2.W;
            value1.X *= value2.X;
            value1.Y *= value2.Y;
            value1.Z *= value2.Z;
            return value1;
        }

        public static Vector4 Multiply(Vector4 value1, float scaleFactor)
        {
            value1.W *= scaleFactor;
            value1.X *= scaleFactor;
            value1.Y *= scaleFactor;
            value1.Z *= scaleFactor;
            return value1;
        }

        public static Vector4 Negate(Vector4 value)
        {
            value.X = -value.X;
            value.Y = -value.Y;
            value.Z = -value.Z;
            value.W = -value.W;
            return value;
        }

        public static Vector4 Normalize(Vector4 vector)
        {
            const float MAG_THRESHOLD = 0.0000001f;
            double factor = DistanceSquared(vector, Zero);
            if (factor > MAG_THRESHOLD)
            {
                factor = 1f / Math.Sqrt(factor);
                vector.X *= factor;
                vector.Y *= factor;
                vector.Z *= factor;
                vector.W *= factor;
            }
            else
            {
                vector.X = 0f;
                vector.Y = 0f;
                vector.Z = 0f;
                vector.W = 0f;
            }
            return vector;
        }

        private static double smoothStep(double a, double b, double v)
        {
            return (b- a) * v + a;
        }
        public static Vector4 SmoothStep(Vector4 value1, Vector4 value2, double amount)
        {
            return new Vector4(
                smoothStep(value1.X, value2.X, amount),
                smoothStep(value1.Y, value2.Y, amount),
                smoothStep(value1.Z, value2.Z, amount),
                smoothStep(value1.W, value2.W, amount));
        }

        public static Vector4 Subtract(Vector4 value1, Vector4 value2)
        {
            value1.W -= value2.W;
            value1.X -= value2.X;
            value1.Y -= value2.Y;
            value1.Z -= value2.Z;
            return value1;
        }

        public static Vector4 Transform(Vector3 position, Matrix4 matrix)
        {
            return new Vector4(
                (position.X * matrix.M11) + (position.Y * matrix.M21) + (position.Z * matrix.M31) + matrix.M41,
                (position.X * matrix.M12) + (position.Y * matrix.M22) + (position.Z * matrix.M32) + matrix.M42,
                (position.X * matrix.M13) + (position.Y * matrix.M23) + (position.Z * matrix.M33) + matrix.M43,
                (position.X * matrix.M14) + (position.Y * matrix.M24) + (position.Z * matrix.M34) + matrix.M44);
        }

        public static Vector4 Transform(Vector4 vector, Matrix4 matrix)
        {
            return new Vector4(
                (vector.X * matrix.M11) + (vector.Y * matrix.M21) + (vector.Z * matrix.M31) + (vector.W * matrix.M41),
                (vector.X * matrix.M12) + (vector.Y * matrix.M22) + (vector.Z * matrix.M32) + (vector.W * matrix.M42),
                (vector.X * matrix.M13) + (vector.Y * matrix.M23) + (vector.Z * matrix.M33) + (vector.W * matrix.M43),
                (vector.X * matrix.M14) + (vector.Y * matrix.M24) + (vector.Z * matrix.M34) + (vector.W * matrix.M44));
        }

        public static Vector4 Parse(string val)
        {
            char[] splitChar = { ',' };
            string[] split = val.Replace("<", String.Empty).Replace(">", String.Empty).Split(splitChar);
            return new Vector4(
                float.Parse(split[0].Trim(), EnUsCulture),
                float.Parse(split[1].Trim(), EnUsCulture),
                float.Parse(split[2].Trim(), EnUsCulture),
                float.Parse(split[3].Trim(), EnUsCulture));
        }

        public static bool TryParse(string val, out Vector4 result)
        {
            try
            {
                result = Parse(val);
                return true;
            }
            catch (Exception)
            {
                result = new Vector4();
                return false;
            }
        }

        #endregion Static Methods

        #region Overrides

        public override bool Equals(object obj)
        {
            return (obj is Vector4) ? this == (Vector4)obj : false;
        }

        public bool Equals(Vector4 other)
        {
            return W == other.W
                && X == other.X
                && Y == other.Y
                && Z == other.Z;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^ W.GetHashCode();
        }

        public override string ToString()
        {
            return String.Format(EnUsCulture, "<{0}, {1}, {2}, {3}>", X, Y, Z, W);
        }

        #endregion Overrides

        #region Operators

        public static bool operator ==(Vector4 value1, Vector4 value2)
        {
            return value1.W == value2.W
                && value1.X == value2.X
                && value1.Y == value2.Y
                && value1.Z == value2.Z;
        }

        public static bool operator !=(Vector4 value1, Vector4 value2)
        {
            return !(value1 == value2);
        }

        public static Vector4 operator +(Vector4 value1, Vector4 value2)
        {
            value1.W += value2.W;
            value1.X += value2.X;
            value1.Y += value2.Y;
            value1.Z += value2.Z;
            return value1;
        }

        public static Vector4 operator -(Vector4 value)
        {
            return new Vector4(-value.X, -value.Y, -value.Z, -value.W);
        }

        public static Vector4 operator -(Vector4 value1, Vector4 value2)
        {
            value1.W -= value2.W;
            value1.X -= value2.X;
            value1.Y -= value2.Y;
            value1.Z -= value2.Z;
            return value1;
        }

        public static Vector4 operator *(Vector4 value1, Vector4 value2)
        {
            value1.W *= value2.W;
            value1.X *= value2.X;
            value1.Y *= value2.Y;
            value1.Z *= value2.Z;
            return value1;
        }

        public static Vector4 operator *(Vector4 value1, float scaleFactor)
        {
            value1.W *= scaleFactor;
            value1.X *= scaleFactor;
            value1.Y *= scaleFactor;
            value1.Z *= scaleFactor;
            return value1;
        }

        public static Vector4 operator /(Vector4 value1, Vector4 value2)
        {
            value1.W /= value2.W;
            value1.X /= value2.X;
            value1.Y /= value2.Y;
            value1.Z /= value2.Z;
            return value1;
        }

        public static Vector4 operator /(Vector4 value1, float divider)
        {
            float factor = 1f / divider;
            value1.W *= factor;
            value1.X *= factor;
            value1.Y *= factor;
            value1.Z *= factor;
            return value1;
        }

        #endregion Operators

        public readonly static Vector4 Zero = new Vector4();
        public readonly static Vector4 One = new Vector4(1f, 1f, 1f, 1f);
        public readonly static Vector4 UnitX = new Vector4(1f, 0f, 0f, 0f);
        public readonly static Vector4 UnitY = new Vector4(0f, 1f, 0f, 0f);
        public readonly static Vector4 UnitZ = new Vector4(0f, 0f, 1f, 0f);
        public readonly static Vector4 UnitW = new Vector4(0f, 0f, 0f, 1f);

        private readonly static CultureInfo EnUsCulture = new CultureInfo("en-us");
    }
}
