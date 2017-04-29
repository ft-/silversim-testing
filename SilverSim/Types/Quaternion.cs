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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace SilverSim.Types
{
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("Gendarme.Rules.Design", "EnsureSymmetryForOverloadedOperatorsRule")]
    public struct Quaternion : IEquatable<Quaternion>, IValue
    {
        /// <summary>X value</summary>
        public double X;
        /// <summary>Y value</summary>
        public double Y;
        /// <summary>Z value</summary>
        public double Z;
        /// <summary>W value</summary>
        public double W;

        #region Properties
        public ValueType Type
        {
            get
            {
                return ValueType.Rotation;
            }
        }

        public LSLValueType LSL_Type
        {
            get
            {
                return LSLValueType.Rotation;
            }
        }
        #endregion Properties

        #region Constructors

        public Quaternion(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Quaternion(Vector3 vectorPart, double scalarPart)
        {
            X = vectorPart.X;
            Y = vectorPart.Y;
            Z = vectorPart.Z;
            W = scalarPart;
        }

        /// <summary>
        /// Build a quaternion from normalized double values
        /// </summary>
        /// <param name="x">X value from -1.0 to 1.0</param>
        /// <param name="y">Y value from -1.0 to 1.0</param>
        /// <param name="z">Z value from -1.0 to 1.0</param>
        public Quaternion(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;

            double xyzsum = 1 - X * X - Y * Y - Z * Z;
            W = (xyzsum > 0) ? Math.Sqrt(xyzsum) : 0;
        }

        public Quaternion(Quaternion q)
        {
            X = q.X;
            Y = q.Y;
            Z = q.Z;
            W = q.W;
        }

        #endregion Constructors

        #region Public Methods

        public bool ApproxEquals(Quaternion quat, double tolerance)
        {
            Quaternion diff = this - quat;
            return (diff.LengthSquared <= tolerance * tolerance);
        }

        public double Length
        {
            get
            {
                return Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
            }
        }

        public double LengthSquared
        {
            get
            {
                return (X * X + Y * Y + Z * Z + W * W);
            }
        }

        public Vector3 GetEulerAngles()
        {
            Vector3 v = new Vector3();
            GetEulerAngles(out v.X, out v.Y, out v.Z);
            return v;
        }

        const double GimbalThreshold = 0.000436;

        public void GetEulerAngles(out double roll, out double pitch, out double yaw)
        {
            double sx = 2 * (X * W - Y * Z);
            double sy = 2 * (Y * W + X * Z);
            double ys = W * W - Y * Y;
            double xz = X * X - Z * Z;
            double cx = ys - xz;
            double cy = Math.Sqrt(sx * sx + cx * cx);
            if(cy > GimbalThreshold)
            {
                roll = Math.Atan2(sx, cx);
                pitch = Math.Atan2(sy, cy);
                yaw = Math.Atan2(2 * (Z * W - X * Y), ys + xz);
            }
            else if(sy > 0)
            {
                roll = 0;
                pitch = Math.PI / 2d;
                yaw = 2 * Math.Atan2(Z + X, W + Y);
            }
            else
            {
                roll = 0;
                pitch = -Math.PI / 2d;
                yaw = 2 * Math.Atan2(Z - X, W - Y);
            }
        }

        /// <summary>
        /// Convert this quaternion to an angle around an axis
        /// </summary>
        /// <param name="axis">Unit vector describing the axis</param>
        /// <param name="angle">Angle around the axis, in radians</param>
        public void GetAxisAngle(out Vector3 axis, out double angle)
        {
            axis = new Vector3();
            double scale = Math.Sqrt(X * X + Y * Y + Z * Z);

            if (scale < Double.Epsilon || W > 1.0f || W < -1.0f)
            {
                angle = 0.0f;
                axis.X = 0.0f;
                axis.Y = 1.0f;
                axis.Z = 0.0f;
            }
            else
            {
                angle = 2.0f * Math.Acos(W);
                double ooscale = 1f / scale;
                axis.X = X * ooscale;
                axis.Y = Y * ooscale;
                axis.Z = Z * ooscale;
            }
        }

        #endregion Public Methods

        #region Static Methods

        /// <summary>
        /// Build a quaternion from an axis and an angle of rotation around
        /// that axis
        /// </summary>
        public static Quaternion CreateFromAxisAngle(double axisX, double axisY, double axisZ, double angle)
        {
            Vector3 axis = new Vector3(axisX, axisY, axisZ);
            return CreateFromAxisAngle(axis, angle);
        }

        /// <summary>
        /// Build a quaternion from an axis and an angle of rotation around
        /// that axis
        /// </summary>
        /// <param name="axis">Axis of rotation</param>
        /// <param name="angle">Angle of rotation</param>
        public static Quaternion CreateFromAxisAngle(Vector3 axis, double angle)
        {
            Quaternion q;
            axis = Vector3.Normalize(axis);

            angle *= 0.5f;
            double c = Math.Cos(angle);
            double s = Math.Sin(angle);

            q.X = axis.X * s;
            q.Y = axis.Y * s;
            q.Z = axis.Z * s;
            q.W = c;

            q.NormalizeSelf();
            return q;
        }

        /// <summary>
        /// Creates a quaternion from a vector containing roll, pitch, and yaw
        /// in radians
        /// </summary>
        /// <param name="eulers">Vector representation of the euler angles in
        /// radians</param>
        /// <returns>Quaternion representation of the euler angles</returns>
        public static Quaternion CreateFromEulers(Vector3 eulers)
        {
            return CreateFromEulers(eulers.X, eulers.Y, eulers.Z);
        }

        /// <summary>
        /// Creates a quaternion from roll, pitch, and yaw euler angles in
        /// radians
        /// </summary>
        /// <param name="roll">X angle in radians</param>
        /// <param name="pitch">Y angle in radians</param>
        /// <param name="yaw">Z angle in radians</param>
        /// <returns>Quaternion representation of the euler angles</returns>
        public static Quaternion CreateFromEulers(double roll, double pitch, double yaw)
        {
            if (roll > Math.PI * 2 || pitch > Math.PI * 2 || yaw > Math.PI * 2)
            {
                throw new ArgumentException("Euler angles must be in radians");
            }

            double atCos = Math.Cos(roll / 2f);
            double atSin = Math.Sin(roll / 2f);
            double leftCos = Math.Cos(pitch / 2f);
            double leftSin = Math.Sin(pitch / 2f);
            double upCos = Math.Cos(yaw / 2f);
            double upSin = Math.Sin(yaw / 2f);
            double atLeftCos = atCos * leftCos;
            double atLeftSin = atSin * leftSin;
            return new Quaternion(
                (atSin * leftCos * upCos + atCos * leftSin * upSin),
                (atCos * leftSin * upCos - atSin * leftCos * upSin),
                (atLeftCos * upSin + atLeftSin * upCos),
                (atLeftCos * upCos - atLeftSin * upSin)
            );
        }

        public static Quaternion CreateFromRotationMatrix(Matrix4 m)
        {
            Quaternion quat;

            double trace = m.Trace();

            if (trace > Double.Epsilon)
            {
                double s = Math.Sqrt(trace + 1f);
                quat.W = s * 0.5f;
                s = 0.5f / s;
                quat.X = (m.M23 - m.M32) * s;
                quat.Y = (m.M31 - m.M13) * s;
                quat.Z = (m.M12 - m.M21) * s;
            }
            else
            {
                if (m.M11 > m.M22 && m.M11 > m.M33)
                {
                    double s = Math.Sqrt(1f + m.M11 - m.M22 - m.M33);
                    quat.X = 0.5f * s;
                    s = 0.5f / s;
                    quat.Y = (m.M12 + m.M21) * s;
                    quat.Z = (m.M13 + m.M31) * s;
                    quat.W = (m.M23 - m.M32) * s;
                }
                else if (m.M22 > m.M33)
                {
                    double s = Math.Sqrt(1f + m.M22 - m.M11 - m.M33);
                    quat.Y = 0.5f * s;
                    s = 0.5f / s;
                    quat.X = (m.M21 + m.M12) * s;
                    quat.Z = (m.M32 + m.M23) * s;
                    quat.W = (m.M31 - m.M13) * s;
                }
                else
                {
                    double s = Math.Sqrt(1f + m.M33 - m.M11 - m.M22);
                    quat.Z = 0.5f * s;
                    s = 0.5f / s;
                    quat.X = (m.M31 + m.M13) * s;
                    quat.Y = (m.M32 + m.M23) * s;
                    quat.W = (m.M12 - m.M21) * s;
                }
            }

            return quat;
        }

        public static double Dot(Quaternion q1, Quaternion q2)
        {
            return (q1.X * q2.X) + (q1.Y * q2.Y) + (q1.Z * q2.Z) + (q1.W * q2.W);
        }

        /// <summary>
        /// Conjugates and renormalizes a vector
        /// </summary>
        public static Quaternion Inverse(Quaternion quaternion)
        {
            double norm = quaternion.LengthSquared;

            if (Math.Abs(norm) < Double.Epsilon)
            {
                quaternion.X = 0f;
                quaternion.Y = 0f;
                quaternion.Z = 0f;
                quaternion.W = 0f;
            }
            else
            {
                double oonorm = 1f / norm;
                quaternion = -quaternion;

                quaternion.X *= oonorm;
                quaternion.Y *= oonorm;
                quaternion.Z *= oonorm;
                quaternion.W *= oonorm;
            }

            return quaternion;
        }

        /// <summary>
        /// Spherical linear interpolation between two quaternions
        /// </summary>
        public static Quaternion Slerp(Quaternion q1, Quaternion q2, double amount)
        {
            double angle = Dot(q1, q2);

            if (angle < 0f)
            {
                q1 *= -1f;
                angle *= -1f;
            }

            double scale;
            double invscale;

            if ((angle + 1f) > 0.05f)
            {
                if ((1f - angle) >= 0.05f)
                {
                    // slerp
                    double theta = Math.Acos(angle);
                    double invsintheta = 1f / Math.Sin(theta);
                    scale = Math.Sin(theta * (1f - amount)) * invsintheta;
                    invscale = Math.Sin(theta * amount) * invsintheta;
                }
                else
                {
                    // lerp
                    scale = 1f - amount;
                    invscale = amount;
                }
            }
            else
            {
                q2.X = -q1.Y;
                q2.Y = q1.X;
                q2.Z = -q1.W;
                q2.W = q1.Z;

                scale = Math.Sin(Math.PI * (0.5f - amount));
                invscale = Math.Sin(Math.PI * amount);
            }

            return (q1 * scale) + (q2 * invscale);
        }

        /// <summary>
        /// Normalizes the quaternion
        /// </summary>
        public Quaternion Normalize()
        {
            Quaternion q = new Quaternion(this);
            q.NormalizeSelf();
            return q;
        }

        public void NormalizeSelf()
        {
            const double MAG_THRESHOLD = 0.0000001f;
            double mag = Length;

            // Catch very small rounding errors when normalizing
            if (mag > MAG_THRESHOLD)
            {
                double oomag = 1f / mag;
                X *= oomag;
                Y *= oomag;
                Z *= oomag;
                W *= oomag;
            }
            else
            {
                X = 0;
                Y = 0;
                Z = 0;
                W = 1;
            }
        }

        public static Quaternion RotBetween(Vector3 a, Vector3 b)
        {
            Quaternion rotBetween;
            // Check for zero vectors. If either is zero, return zero rotation. Otherwise,
            // continue calculation.
            if (a == new Vector3(0.0f, 0.0f, 0.0f) || b == new Vector3(0.0f, 0.0f, 0.0f))
            {
                rotBetween = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
            }
            else
            {
                a = Vector3.Normalize(a);
                b = Vector3.Normalize(b);
                double dotProduct = a.Dot(b);
                // There are two degenerate cases possible. These are for vectors 180 or
                // 0 degrees apart. These have to be detected and handled individually.
                //
                // Check for vectors 180 degrees apart.
                // A dot product of -1 would mean the angle between vectors is 180 degrees.
                if (dotProduct < -0.9999999f)
                {
                    // First assume X axis is orthogonal to the vectors.
                    Vector3 orthoVector = new Vector3(1.0f, 0.0f, 0.0f);
                    orthoVector = orthoVector - a * (a.X / a.Dot(a));
                    // Check for near zero vector. A very small non-zero number here will create
                    // a rotation in an undesired direction.
                    rotBetween = (orthoVector.Length > 0.0001) ?
                        new Quaternion(orthoVector.X, orthoVector.Y, orthoVector.Z, 0.0f) :
                        // If the magnitude of the vector was near zero, then assume the X axis is not
                        // orthogonal and use the Z axis instead.
                        // Set 180 z rotation.
                        new Quaternion(0.0f, 0.0f, 1.0f, 0.0f);
                }
                // Check for parallel vectors.
                // A dot product of 1 would mean the angle between vectors is 0 degrees.
                else if (dotProduct > 0.9999999f)
                {
                    // Set zero rotation.
                    rotBetween = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
                }
                else
                {
                    // All special checks have been performed so get the axis of rotation.
                    Vector3 crossProduct = a.Cross(b);
                    // Quarternion s value is the length of the unit vector + dot product.
                    double qs = 1.0 + dotProduct;
                    rotBetween = new Quaternion(crossProduct.X, crossProduct.Y, crossProduct.Z, qs);
                    // Normalize the rotation.
                    double mag = rotBetween.Length;
                    // We shouldn't have to worry about a divide by zero here. The qs value will be
                    // non-zero because we already know if we're here, then the dotProduct is not -1 so
                    // qs will not be zero. Also, we've already handled the input vectors being zero so the
                    // crossProduct vector should also not be zero.
                    rotBetween.X /= mag;
                    rotBetween.Y /= mag;
                    rotBetween.Z /= mag;
                    rotBetween.W /= mag;
                    // Check for undefined values and set zero rotation if any found. This code might not actually be required
                    // any longer since zero vectors are checked for at the top.
                    if (Double.IsNaN(rotBetween.X) || Double.IsNaN(rotBetween.Y) || Double.IsNaN(rotBetween.Z) || Double.IsNaN(rotBetween.W))
                    {
                        rotBetween = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
                    }
                }
            }
            return rotBetween;

        }

        public static Quaternion Parse(string val)
        {
            Quaternion q;
            if(!TryParse(val, out q))
            {
                throw new ArgumentException("Invalid quaternion string passed");
            }
            return q;
        }

        public static bool TryParse(string val, out Quaternion result)
        {
            result = default(Quaternion);
            char[] splitChar = { ',' };
            string[] split = val.Replace("<", System.String.Empty).Replace(">", System.String.Empty).Split(splitChar);
            int splitLength = split.Length;
            if (splitLength < 3 || splitLength > 4)
            {
                return false;
            }
            double x;
            double y;
            double z;
            double w;
            if(!double.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) ||
               !double.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y) ||
                !double.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
            {
                return false;
            }
            if (splitLength == 3)
            {
                result = new Quaternion(x, y, z);
            }
            else
            {
                if (!double.TryParse(split[3], NumberStyles.Float, CultureInfo.InvariantCulture, out w))
                {
                    return false;
                }
                result = new Quaternion(x, y, z, w);
            }
            return true;
        }
        #endregion Static Methods

        #region Axes
        public static Quaternion Axes2Rot(Vector3 fwd, Vector3 left, Vector3 up)
        {
            double s;
            double tr = fwd.X + left.Y + up.Z + 1.0;

            if(fwd.Length + left.Length + up.Length < double.Epsilon)
            {
                return new Quaternion(1, 0, 0, 0);
            }
            else if (tr >= 1.0)
            {
                s = 0.5 / Math.Sqrt(tr);
                return new Quaternion(
                        (left.Z - up.Y) * s,
                        (up.X - fwd.Z) * s,
                        (fwd.Y - left.X) * s,
                        0.25 / s);
            }
            else
            {
                double max = (left.Y > up.Z) ? left.Y : up.Z;

                if (max < fwd.X)
                {
                    s = Math.Sqrt(fwd.X - (left.Y + up.Z) + 1.0);
                    double x = s * 0.5;
                    s = 0.5 / s;
                    return new Quaternion(
                            x,
                            (fwd.Y + left.X) * s,
                            (up.X + fwd.Z) * s,
                            (left.Z - up.Y) * s);
                }
                else if (max == left.Y)
                {
                    s = Math.Sqrt(left.Y - (up.Z + fwd.X) + 1.0);
                    double y = s * 0.5;
                    s = 0.5 / s;
                    return new Quaternion(
                            (fwd.Y + left.X) * s,
                            y,
                            (left.Z + up.Y) * s,
                            (up.X - fwd.Z) * s);
                }
                else
                {
                    s = Math.Sqrt(up.Z - (fwd.X + left.Y) + 1.0);
                    double z = s * 0.5;
                    s = 0.5 / s;
                    return new Quaternion(
                            (up.X + fwd.Z) * s,
                            (left.Z + up.Y) * s,
                            z,
                            (fwd.Y - left.X) * s);
                }
            }
        }

        public Vector3 FwdAxis
        {
            get
            {
                double x;
                double y;
                double z;

                Quaternion t = Normalize();

                x = t.X * t.X - t.Y * t.Y - t.Z * t.Z + t.W * t.W;
                y = 2 * (t.X * t.Y + t.Z * t.W);
                z = 2 * (t.X * t.Z - t.Y * t.W);
                return new Vector3(x, y, z);
            }
        }

        public Vector3 LeftAxis
        {
            get
            {
                double x;
                double y;
                double z;

                Quaternion t = Normalize();

                x = 2 * (t.X * t.Y - t.Z * t.W);
                y = -t.X * t.X + t.Y * t.Y - t.Z * t.Z + t.W * t.W;
                z = 2 * (t.X * t.W + t.Y * t.Z);
                return new Vector3(x, y, z);
            }
        }

        public Vector3 UpAxis
        {
            get
            {
                double x;
                double y;
                double z;

                Quaternion t = Normalize();

                x = 2 * (t.X * t.Z + t.Y * t.W);
                y = 2 * (-t.X * t.W + t.Y * t.Z);
                z = -t.X * t.X - t.Y * t.Y + t.Z * t.Z + t.W * t.W;
                return new Vector3(x, y, z);
            }
        }
        #endregion

        public Matrix4 GetMatrix()
        {
            Matrix4 matrix = new Matrix4();

            double xx = X * X;
            double yy = Y * Y;
            double zz = Z * Z;
            double xy = X * Y;
            double zw = Z * W;
            double zx = Z * X;
            double yw = Y * W;
            double yz = Y * Z;
            double xw = X * W;

            matrix.M11 = 1f - (2f * (yy + zz));
            matrix.M12 = 2f * (xy + zw);
            matrix.M13 = 2f * (zx - yw);

            matrix.M21 = 2f * (xy - zw);
            matrix.M22 = 1f - (2f * (zz + xx));
            matrix.M23 = 2f * (yz + xw);

            matrix.M31 = 2f * (zx + yw);
            matrix.M32 = 2f * (yz - xw);
            matrix.M33 = 1f - (2f * (yy + xx));

            matrix.M44 = 1f;

            return matrix;
        }

        #region Overrides

        public override bool Equals(object obj)
        {
            return (obj is Quaternion) && this == (Quaternion)obj;
        }

        public bool Equals(Quaternion other)
        {
            return Math.Abs(W - other.W) < Double.Epsilon
                && Math.Abs(X - other.X) < Double.Epsilon
                && Math.Abs(Y - other.Y) < Double.Epsilon
                && Math.Abs(Z - other.Z) < Double.Epsilon;
        }

        public override int GetHashCode()
        {
            return (X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^ W.GetHashCode());
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "<{0},{1},{2},{3}>", X, Y, Z, W);
        }

        public string X_String
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}", X);
            }
        }

        public string Y_String
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}", Y);
            }
        }

        public string Z_String
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}", Z);
            }
        }

        public string W_String
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}", W);
            }
        }

        #endregion Overrides

        #region Operators

        public static bool operator ==(Quaternion quaternion1, Quaternion quaternion2)
        {
            return quaternion1.Equals(quaternion2);
        }

        public static bool operator !=(Quaternion quaternion1, Quaternion quaternion2)
        {
            return !(quaternion1 == quaternion2);
        }

        public static Quaternion operator +(Quaternion quaternion1, Quaternion quaternion2)
        {
            return new Quaternion(quaternion1.X + quaternion2.X,
                quaternion1.Y + quaternion2.Y,
                quaternion1.Z + quaternion2.Z,
                quaternion1.W + quaternion2.W);
        }

        public static AnArray operator+(Quaternion q, AnArray a)
        {
            AnArray b = new AnArray();
            b.Add(q);
            b.AddRange(a);
            return b;
        }

        /** <summary> do not use to produce the inverse rotation. Only use Conjugate() for inversing the rotation</summary> */
        public static Quaternion operator -(Quaternion quaternion)
        {
            return new Quaternion(-quaternion.X, -quaternion.Y, -quaternion.Z, -quaternion.W);
        }

        public static Quaternion operator -(Quaternion quaternion1, Quaternion quaternion2)
        {
            return new Quaternion(quaternion1.X - quaternion2.X,
                quaternion1.Y - quaternion2.Y,
                quaternion1.Z - quaternion2.Z,
                quaternion1.W - quaternion2.W);
        }

        public static Quaternion operator *(Quaternion a, Quaternion b)
        {
            return new Quaternion(
                a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
                a.W * b.Y + a.Y * b.W + a.Z * b.X - a.X * b.Z,
                a.W * b.Z + a.Z * b.W + a.X * b.Y - a.Y * b.X,
                a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z
            );
        }

        public static Quaternion operator *(Quaternion quaternion, double scaleFactor)
        {
            return new Quaternion(quaternion.X * scaleFactor, quaternion.Y * scaleFactor, quaternion.Z * scaleFactor, quaternion.W * scaleFactor);
        }

        public static Quaternion operator *(double scaleFactor, Quaternion quaternion)
        {
            return new Quaternion(quaternion.X * scaleFactor, quaternion.Y * scaleFactor, quaternion.Z * scaleFactor, quaternion.W * scaleFactor);
        }

        public Quaternion Conjugate()
        {
            return new Quaternion(-X, -Y, -Z, W);
        }

        public static Quaternion operator /(Quaternion a, Quaternion b)
        {
            return a * b.Conjugate();
        }


        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public static explicit operator Quaternion(string val)
        {
            return Quaternion.Parse(val);
        }

        public static explicit operator string(Quaternion val)
        {
            return val.ToString();
        }

        #endregion Operators

        public bool IsLSLTrue
        {
            get
            {
                return !Equals(Identity);
            }
        }

        #region Byte conversion
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule")]
        public byte[] AsByte
        {
            get
            {
                byte[] bytes = new byte[12];
                ToBytes(bytes, 0);
                return bytes;
            }
            set
            {
                FromBytes(value, 0, true);
            }
        }

        public void FromBytes(byte[] byteArray, int pos, bool normalized)
        {
            if (!normalized)
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
            else
            {
                if (!BitConverter.IsLittleEndian)
                {
                    // Big endian architecture
                    byte[] conversionBuffer = new byte[16];

                    Buffer.BlockCopy(byteArray, pos, conversionBuffer, 0, 12);

                    Array.Reverse(conversionBuffer, 0, 4);
                    Array.Reverse(conversionBuffer, 4, 4);
                    Array.Reverse(conversionBuffer, 8, 4);

                    X = BitConverter.ToSingle(conversionBuffer, 0);
                    Y = BitConverter.ToSingle(conversionBuffer, 4);
                    Z = BitConverter.ToSingle(conversionBuffer, 8);
                }
                else
                {
                    // Little endian architecture
                    X = BitConverter.ToSingle(byteArray, pos);
                    Y = BitConverter.ToSingle(byteArray, pos + 4);
                    Z = BitConverter.ToSingle(byteArray, pos + 8);
                }

                double xyzsum = 1f - X * X - Y * Y - Z * Z;
                W = (xyzsum > 0f) ? Math.Sqrt(xyzsum) : 0f;
            }
        }
        public void ToBytes(byte[] dest, int pos)
        {
            double norm = Math.Sqrt(X * X + Y * Y + Z * Z + W * W);

            if (Math.Abs(norm) >= Double.Epsilon)
            {
                norm = 1f / norm;

                double x;
                double y;
                double z;

                if (W >= 0f)
                {
                    x = X;
                    y = Y;
                    z = Z;
                }
                else
                {
                    x = -X;
                    y = -Y;
                    z = -Z;
                }

                Buffer.BlockCopy(BitConverter.GetBytes((float)(norm * x)), 0, dest, pos + 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes((float)(norm * y)), 0, dest, pos + 4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes((float)(norm * z)), 0, dest, pos + 8, 4);

                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(dest, pos + 0, 4);
                    Array.Reverse(dest, pos + 4, 4);
                    Array.Reverse(dest, pos + 8, 4);
                }
            }
            else
            {
                throw new InvalidOperationException(String.Format(
                    "Quaternion {0} has been normalized to zero", ToString()));
            }
        }
        #endregion

        #region Helpers
        public ABoolean AsBoolean { get { return new ABoolean(Length >= Double.Epsilon); } }
        public Integer AsInteger { get { return new Integer((int)Length); } }
        public Quaternion AsQuaternion { get { return new Quaternion(X, Y, Z, W); } }
        public Real AsReal { get { return new Real(Length); } }
        public AString AsString { get { return new AString(ToString()); } }
        public UUID AsUUID { get { return new UUID(); } }
        public Vector3 AsVector3 { get { return new Vector3(X, Y, Z); } }
        public uint AsUInt { get { return (uint)Length; } }
        public int AsInt { get { return (int)Length; } }
        public ulong AsULong { get { return (ulong)Length; } }
        #endregion

        /// <summary>A quaternion with a value of 0,0,0,1</summary>
        public readonly static Quaternion Identity = new Quaternion(0f, 0f, 0f, 1f);
    }
}
