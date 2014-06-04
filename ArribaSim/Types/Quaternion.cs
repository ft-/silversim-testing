/*
 * ArribaSim is distributed under the terms of the
 * GNU General Public License v2 
 * with the following clarification and special exception.
 * 
 * Linking this code statically or dynamically with other modules is
 * making a combined work based on this code. Thus, the terms and
 * conditions of the GNU General Public License cover the whole
 * combination.
 * 
 * As a special exception, the copyright holders of this code give you
 * permission to link this code with independent modules to produce an
 * executable, regardless of the license terms of these independent
 * modules, and to copy and distribute the resulting executable under
 * terms of your choice, provided that you also meet, for each linked
 * independent module, the terms and conditions of the license of that
 * module. An independent module is a module which is not derived from
 * or based on this code. If you modify this code, you may extend
 * this exception to your version of the code, but you are not
 * obligated to do so. If you do not wish to do so, delete this
 * exception statement from your version.
 * 
 * License text is derived from GNU classpath text
 */

using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ArribaSim.Types
{
    [StructLayout(LayoutKind.Sequential)]
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


        /// <summary>
        /// Convert this quaternion to euler angles
        /// </summary>
        /// <param name="roll">X euler angle</param>
        /// <param name="pitch">Y euler angle</param>
        /// <param name="yaw">Z euler angle</param>
        public void GetEulerAngles(out double roll, out double pitch, out double yaw)
        {
            roll = 0f;
            pitch = 0f;
            yaw = 0f;

            Quaternion t = new Quaternion(this.X * this.X, this.Y * this.Y, this.Z * this.Z, this.W * this.W);

            double m = (t.X + t.Y + t.Z + t.W);
            if (Math.Abs(m) < 0.001d) return;
            double n = 2 * (this.Y * this.W + this.X * this.Z);
            double p = m * m - n * n;

            if (p > 0f)
            {
                roll = Math.Atan2(2.0f * (this.X * this.W - this.Y * this.Z), (-t.X - t.Y + t.Z + t.W));
                pitch = Math.Atan2(n, Math.Sqrt(p));
                yaw = Math.Atan2(2.0f * (this.Z * this.W - this.X * this.Y), t.X - t.Y - t.Z + t.W);
            }
            else if (n > 0f)
            {
                roll = 0f;
                pitch = (Math.PI / 2d);
                yaw = Math.Atan2((this.Z * this.W + this.X * this.Y), 0.5f - t.X - t.Y);
            }
            else
            {
                roll = 0f;
                pitch = -(Math.PI / 2d);
                yaw = Math.Atan2((this.Z * this.W + this.X * this.Y), 0.5f - t.X - t.Z);
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

            if (scale < Single.Epsilon || W > 1.0f || W < -1.0f)
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

            return Quaternion.Normalize(q);
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
                throw new ArgumentException("Euler angles must be in radians");

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

            if (trace > Single.Epsilon)
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

            if (norm == 0f)
            {
                quaternion.X = quaternion.Y = quaternion.Z = quaternion.W = 0f;
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

        public static Quaternion Normalize(Quaternion q)
        {
            return new Quaternion(q).Normalize();
        }

        /// <summary>
        /// Normalizes the quaternion
        /// </summary>
        public Quaternion Normalize()
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
                X = 0f;
                Y = 0f;
                Z = 0f;
                W = 1f;
            }

            return this;
        }


        public static Quaternion Parse(string val)
        {
            char[] splitChar = { ',' };
            string[] split = val.Replace("<", System.String.Empty).Replace(">", System.String.Empty).Split(splitChar);
            if (split.Length == 3)
            {
                return new Quaternion(
                    double.Parse(split[0].Trim(), EnUsCulture),
                    double.Parse(split[1].Trim(), EnUsCulture),
                    double.Parse(split[2].Trim(), EnUsCulture));
            }
            else
            {
                return new Quaternion(
                    double.Parse(split[0].Trim(), EnUsCulture),
                    double.Parse(split[1].Trim(), EnUsCulture),
                    double.Parse(split[2].Trim(), EnUsCulture),
                    double.Parse(split[3].Trim(), EnUsCulture));
            }
        }

        public static bool TryParse(string val, out Quaternion result)
        {
            try
            {
                result = Parse(val);
                return true;
            }
            catch (Exception)
            {
                result = new Quaternion();
                return false;
            }
        }

        #endregion Static Methods

        #region Overrides

        public override bool Equals(object obj)
        {
            return (obj is Quaternion) ? this == (Quaternion)obj : false;
        }

        public bool Equals(Quaternion other)
        {
            return W == other.W
                && X == other.X
                && Y == other.Y
                && Z == other.Z;
        }

        public override int GetHashCode()
        {
            return (X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^ W.GetHashCode());
        }

        public override string ToString()
        {
            return System.String.Format(EnUsCulture, "<{0},{1},{2},{3}>", X, Y, Z, W);
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

        public static Quaternion operator /(Quaternion quaternion1, Quaternion quaternion2)
        {
            double x = quaternion1.X;
            double y = quaternion1.Y;
            double z = quaternion1.Z;
            double w = quaternion1.W;

            double q2lensq = quaternion2.LengthSquared;
            double ooq2lensq = 1f / q2lensq;
            double x2 = -quaternion2.X * ooq2lensq;
            double y2 = -quaternion2.Y * ooq2lensq;
            double z2 = -quaternion2.Z * ooq2lensq;
            double w2 = quaternion2.W * ooq2lensq;

            return new Quaternion(
                ((x * w2) + (x2 * w)) + (y * z2) - (z * y2),
                ((y * w2) + (y2 * w)) + (z * x2) - (x * z2),
                ((z * w2) + (z2 * w)) + (x * y2) - (y * x2),
                (w * w2) - ((x * x2) + (y * y2)) + (z * z2));
        }


        public static explicit operator Quaternion(string val)
        {
            return Quaternion.Parse(val);
        }

        public static explicit operator string(Quaternion val)
        {
            return val.ToString();
        }

        #endregion Operators

        #region Helpers
        public ABoolean AsBoolean { get { return new ABoolean(Length >= Single.Epsilon); } }
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

        private readonly static CultureInfo EnUsCulture = new CultureInfo("en-us");
    }
}
