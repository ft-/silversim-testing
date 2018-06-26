// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

#pragma warning disable RCS1123

using System;
using System.Globalization;

namespace SilverSim.Types
{
    public struct Vector4 : IEquatable<Vector4>
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
            X = 0f;
            Y = 0f;
            Z = 0f;
            W = 0f;
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

        public double Length => Math.Sqrt(DistanceSquared(this, Zero));

        public double LengthSquared => DistanceSquared(this, Zero);

        public void Normalize()
        {
            this = Normalize(this);
        }

        public bool ApproxEquals(Vector4 vec, double tolerance)
        {
            Vector4 diff = this - vec;
            return diff.LengthSquared <= tolerance * tolerance;
        }

        public int CompareTo(Vector4 vector)
        {
            return Length.CompareTo(vector.Length);
        }

        public bool IsFinite() => X.IsFinite() && Y.IsFinite() && Z.IsFinite() && W.IsFinite();

        public void FromBytes(byte[] byteArray, int pos)
        {
            if (!BitConverter.IsLittleEndian)
            {
                // Big endian architecture
                var conversionBuffer = new byte[16];

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
            var byteArray = new byte[16];
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

        public static Vector3 Lerp(Vector3 lhs, Vector3 rhs, double c) => lhs + (rhs - lhs) * c;

        public static Vector4 Add(Vector4 value1, Vector4 value2)
        {
            value1.W += value2.W;
            value1.X += value2.X;
            value1.Y += value2.Y;
            value1.Z += value2.Z;
            return value1;
        }

        public static Vector4 Clamp(Vector4 value1, Vector4 min, Vector4 max) => new Vector4(
                value1.X.Clamp(min.X, max.X),
                value1.Y.Clamp(min.Y, max.Y),
                value1.Z.Clamp(min.Z, max.Z),
                value1.W.Clamp(min.W, max.W));

        public static double Distance(Vector4 value1, Vector4 value2) => Math.Sqrt(DistanceSquared(value1, value2));

        public static double DistanceSquared(Vector4 value1, Vector4 value2) => (value1.W - value2.W) * (value1.W - value2.W) +
                (value1.X - value2.X) * (value1.X - value2.X) +
                (value1.Y - value2.Y) * (value1.Y - value2.Y) +
                (value1.Z - value2.Z) * (value1.Z - value2.Z);

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

        public static double Dot(Vector4 vector1, Vector4 vector2) => vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z + vector1.W * vector2.W;

        public static Vector4 Max(Vector4 value1, Vector4 value2) => new Vector4(
               Math.Max(value1.X, value2.X),
               Math.Max(value1.Y, value2.Y),
               Math.Max(value1.Z, value2.Z),
               Math.Max(value1.W, value2.W));

        public static Vector4 Min(Vector4 value1, Vector4 value2) => new Vector4(
               Math.Min(value1.X, value2.X),
               Math.Min(value1.Y, value2.Y),
               Math.Min(value1.Z, value2.Z),
               Math.Min(value1.W, value2.W));

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

        public static Vector4 Subtract(Vector4 value1, Vector4 value2)
        {
            value1.W -= value2.W;
            value1.X -= value2.X;
            value1.Y -= value2.Y;
            value1.Z -= value2.Z;
            return value1;
        }

        public static Vector4 Transform(Vector3 position, Matrix4 matrix) => new Vector4(
                (position.X * matrix.M11) + (position.Y * matrix.M21) + (position.Z * matrix.M31) + matrix.M41,
                (position.X * matrix.M12) + (position.Y * matrix.M22) + (position.Z * matrix.M32) + matrix.M42,
                (position.X * matrix.M13) + (position.Y * matrix.M23) + (position.Z * matrix.M33) + matrix.M43,
                (position.X * matrix.M14) + (position.Y * matrix.M24) + (position.Z * matrix.M34) + matrix.M44);

        public static Vector4 Transform(Vector4 vector, Matrix4 matrix) => new Vector4(
                (vector.X * matrix.M11) + (vector.Y * matrix.M21) + (vector.Z * matrix.M31) + (vector.W * matrix.M41),
                (vector.X * matrix.M12) + (vector.Y * matrix.M22) + (vector.Z * matrix.M32) + (vector.W * matrix.M42),
                (vector.X * matrix.M13) + (vector.Y * matrix.M23) + (vector.Z * matrix.M33) + (vector.W * matrix.M43),
                (vector.X * matrix.M14) + (vector.Y * matrix.M24) + (vector.Z * matrix.M34) + (vector.W * matrix.M44));

        public static Vector4 Parse(string val)
        {
            Vector4 v;
            if(!TryParse(val, out v))
            {
                throw new ArgumentException("Invalid Vector4 string given");
            }
            return v;
        }

        public static bool TryParse(string val, out Vector4 result)
        {
            result = default(Vector4);
            char[] splitChar = { ',' };
            var split = val.Replace("<", String.Empty).Replace(">", String.Empty).Split(splitChar);

            if (split.Length != 4)
            {
                return false;
            }

            double x;
            double y;
            double z;
            double w;

            if(!double.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) ||
                !double.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y) ||
                !double.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z) ||
                !double.TryParse(split[3], NumberStyles.Float, CultureInfo.InvariantCulture, out w))
            {
                return false;
            }
            result = new Vector4(x, y, z, w);
            return true;
        }

        #endregion Static Methods

        #region Overrides

        public override bool Equals(object obj) => (obj is Vector4) && this == (Vector4)obj;

        public bool Equals(Vector4 other) => Math.Abs(W - other.W) < Double.Epsilon
                && Math.Abs(X - other.X) < Double.Epsilon
                && Math.Abs(Y - other.Y) < Double.Epsilon
                && Math.Abs(Z - other.Z) < Double.Epsilon;

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^ W.GetHashCode();

        public override string ToString() => String.Format(CultureInfo.InvariantCulture, "<{0}, {1}, {2}, {3}>", X, Y, Z, W);

        #endregion Overrides

        #region Operators

        public static bool operator ==(Vector4 value1, Vector4 value2) => Math.Abs(value1.W - value2.W) < Double.Epsilon
                && Math.Abs(value1.X - value2.X) < Double.Epsilon
                && Math.Abs(value1.Y - value2.Y) < Double.Epsilon
                && Math.Abs(value1.Z - value2.Z) < Double.Epsilon;

        public static bool operator !=(Vector4 value1, Vector4 value2) => !(value1 == value2);

        public static Vector4 operator +(Vector4 value1, Vector4 value2)
        {
            value1.W += value2.W;
            value1.X += value2.X;
            value1.Y += value2.Y;
            value1.Z += value2.Z;
            return value1;
        }

        public static Vector4 operator -(Vector4 value) => new Vector4(-value.X, -value.Y, -value.Z, -value.W);

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

        public bool IsNaN => double.IsNaN(X) || double.IsNaN(Y) || double.IsNaN(Z) || double.IsNaN(W);

        public readonly static Vector4 Zero = new Vector4();
        public readonly static Vector4 One = new Vector4(1f, 1f, 1f, 1f);
        public readonly static Vector4 UnitX = new Vector4(1f, 0f, 0f, 0f);
        public readonly static Vector4 UnitY = new Vector4(0f, 1f, 0f, 0f);
        public readonly static Vector4 UnitZ = new Vector4(0f, 0f, 1f, 0f);
        public readonly static Vector4 UnitW = new Vector4(0f, 0f, 0f, 1f);
    }
}
