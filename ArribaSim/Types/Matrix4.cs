/*

ArribaSim is distributed under the terms of the
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
using System.Globalization;
using System.Runtime.InteropServices;

namespace ArribaSim.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Matrix4 : IEquatable<Matrix4>
    {
        public double M11, M12, M13, M14;
        public double M21, M22, M23, M24;
        public double M31, M32, M33, M34;
        public double M41, M42, M43, M44;

        #region Properties

        public Vector3 AtAxis
        {
            get
            {
                return new Vector3(M11, M21, M31);
            }
            set
            {
                M11 = value.X;
                M21 = value.Y;
                M31 = value.Z;
            }
        }

        public Vector3 LeftAxis
        {
            get
            {
                return new Vector3(M12, M22, M32);
            }
            set
            {
                M12 = value.X;
                M22 = value.Y;
                M32 = value.Z;
            }
        }

        public Vector3 UpAxis
        {
            get
            {
                return new Vector3(M13, M23, M33);
            }
            set
            {
                M13 = value.X;
                M23 = value.Y;
                M33 = value.Z;
            }
        }

        #endregion Properties

        #region Constructors

        public Matrix4(
            double m11, double m12, double m13, double m14,
            double m21, double m22, double m23, double m24,
            double m31, double m32, double m33, double m34,
            double m41, double m42, double m43, double m44)
        {
            M11 = m11;
            M12 = m12;
            M13 = m13;
            M14 = m14;

            M21 = m21;
            M22 = m22;
            M23 = m23;
            M24 = m24;

            M31 = m31;
            M32 = m32;
            M33 = m33;
            M34 = m34;

            M41 = m41;
            M42 = m42;
            M43 = m43;
            M44 = m44;
        }

        public Matrix4(double roll, double pitch, double yaw)
        {
            this = CreateFromEulers(roll, pitch, yaw);
        }

        public Matrix4(Matrix4 m)
        {
            M11 = m.M11;
            M12 = m.M12;
            M13 = m.M13;
            M14 = m.M14;

            M21 = m.M21;
            M22 = m.M22;
            M23 = m.M23;
            M24 = m.M24;

            M31 = m.M31;
            M32 = m.M32;
            M33 = m.M33;
            M34 = m.M34;

            M41 = m.M41;
            M42 = m.M42;
            M43 = m.M43;
            M44 = m.M44;
        }

        #endregion Constructors

        #region Public Methods

        public double Determinant
        {
            get
            {
                return
                    M14 * M23 * M32 * M41 - M13 * M24 * M32 * M41 - M14 * M22 * M33 * M41 + M12 * M24 * M33 * M41 +
                    M13 * M22 * M34 * M41 - M12 * M23 * M34 * M41 - M14 * M23 * M31 * M42 + M13 * M24 * M31 * M42 +
                    M14 * M21 * M33 * M42 - M11 * M24 * M33 * M42 - M13 * M21 * M34 * M42 + M11 * M23 * M34 * M42 +
                    M14 * M22 * M31 * M43 - M12 * M24 * M31 * M43 - M14 * M21 * M32 * M43 + M11 * M24 * M32 * M43 +
                    M12 * M21 * M34 * M43 - M11 * M22 * M34 * M43 - M13 * M22 * M31 * M44 + M12 * M23 * M31 * M44 +
                    M13 * M21 * M32 * M44 - M11 * M23 * M32 * M44 - M12 * M21 * M33 * M44 + M11 * M22 * M33 * M44;
            }
        }

        public double Determinant3x3
        {
            get
            {
                double det = 0f;

                double diag1 = M11 * M22 * M33;
                double diag2 = M12 * M32 * M31;
                double diag3 = M13 * M21 * M32;
                double diag4 = M31 * M22 * M13;
                double diag5 = M32 * M23 * M11;
                double diag6 = M33 * M21 * M12;

                det = diag1 + diag2 + diag3 - (diag4 + diag5 + diag6);

                return det;
            }
        }

        /// <summary>
        /// Convert this matrix to euler rotations
        /// </summary>
        /// <param name="roll">X euler angle</param>
        /// <param name="pitch">Y euler angle</param>
        /// <param name="yaw">Z euler angle</param>
        public void GetEulerAngles(out double roll, out double pitch, out double yaw)
        {
            double angleX, angleY, angleZ;
            double cx, cy, cz; // cosines
            double sx, sz; // sines
            double M13_a = M13;
            if (M13_a < -1f)
            {
                M13_a = -1f;
            }
            else if (M13_a > 1f)
            {
                M13_a = 1f;
            }

            angleY = Math.Asin(M13_a);
            cy = Math.Cos(angleY);

            if (Math.Abs(cy) > 0.005f)
            {
                // No gimbal lock
                cx = M33 / cy;
                sx = (-M23) / cy;

                angleX = Math.Atan2(sx, cx);

                cz = M11 / cy;
                sz = (-M12) / cy;

                angleZ = Math.Atan2(sz, cz);
            }
            else
            {
                // Gimbal lock
                angleX = 0;

                cz = M22;
                sz = M21;

                angleZ = Math.Atan2(sz, cz);
            }

            // Return only positive angles in [0,360]
            if (angleX < 0) angleX += 360d;
            if (angleY < 0) angleY += 360d;
            if (angleZ < 0) angleZ += 360d;

            roll = angleX;
            pitch = angleY;
            yaw = angleZ;
        }

        public double Trace()
        {
            return M11 + M22 + M33 + M44;
        }

        /// <summary>
        /// Convert this matrix to a quaternion rotation
        /// </summary>
        /// <returns>A quaternion representation of this rotation matrix</returns>
        public Quaternion GetQuaternion()
        {
            Quaternion quat = new Quaternion();
            double trace = Trace() + 1f;

            if (trace > Single.Epsilon)
            {
                double s = 0.5f / Math.Sqrt(trace);

                quat.X = (M32 - M23) * s;
                quat.Y = (M13 - M31) * s;
                quat.Z = (M21 - M12) * s;
                quat.W = 0.25f / s;
            }
            else
            {
                if (M11 > M22 && M11 > M33)
                {
                    double s = 2.0f * Math.Sqrt(1.0f + M11 - M22 - M33);

                    quat.X = 0.25f * s;
                    quat.Y = (M12 + M21) / s;
                    quat.Z = (M13 + M31) / s;
                    quat.W = (M23 - M32) / s;
                }
                else if (M22 > M33)
                {
                    double s = 2.0f * Math.Sqrt(1.0f + M22 - M11 - M33);

                    quat.X = (M12 + M21) / s;
                    quat.Y = 0.25f * s;
                    quat.Z = (M23 + M32) / s;
                    quat.W = (M13 - M31) / s;
                }
                else
                {
                    double s = 2.0f * Math.Sqrt(1.0f + M33 - M11 - M22);

                    quat.X = (M13 + M31) / s;
                    quat.Y = (M23 + M32) / s;
                    quat.Z = 0.25f * s;
                    quat.W = (M12 - M21) / s;
                }
            }

            return quat;
        }

        #endregion Public Methods

        #region Static Methods

        public static Matrix4 operator+(Matrix4 matrix1, Matrix4 matrix2)
        {
            Matrix4 matrix = new Matrix4();
            matrix.M11 = matrix1.M11 + matrix2.M11;
            matrix.M12 = matrix1.M12 + matrix2.M12;
            matrix.M13 = matrix1.M13 + matrix2.M13;
            matrix.M14 = matrix1.M14 + matrix2.M14;

            matrix.M21 = matrix1.M21 + matrix2.M21;
            matrix.M22 = matrix1.M22 + matrix2.M22;
            matrix.M23 = matrix1.M23 + matrix2.M23;
            matrix.M24 = matrix1.M24 + matrix2.M24;

            matrix.M31 = matrix1.M31 + matrix2.M31;
            matrix.M32 = matrix1.M32 + matrix2.M32;
            matrix.M33 = matrix1.M33 + matrix2.M33;
            matrix.M34 = matrix1.M34 + matrix2.M34;

            matrix.M41 = matrix1.M41 + matrix2.M41;
            matrix.M42 = matrix1.M42 + matrix2.M42;
            matrix.M43 = matrix1.M43 + matrix2.M43;
            matrix.M44 = matrix1.M44 + matrix2.M44;
            return matrix;
        }

        public static Matrix4 CreateFromAxisAngle(Vector3 axis, double angle)
        {
            Matrix4 matrix = new Matrix4();

            double x = axis.X;
            double y = axis.Y;
            double z = axis.Z;
            double sin = Math.Sin(angle);
            double cos = Math.Cos(angle);
            double xx = x * x;
            double yy = y * y;
            double zz = z * z;
            double xy = x * y;
            double xz = x * z;
            double yz = y * z;

            matrix.M11 = xx + (cos * (1f - xx));
            matrix.M12 = (xy - (cos * xy)) + (sin * z);
            matrix.M13 = (xz - (cos * xz)) - (sin * y);
            //matrix.M14 = 0f;

            matrix.M21 = (xy - (cos * xy)) - (sin * z);
            matrix.M22 = yy + (cos * (1f - yy));
            matrix.M23 = (yz - (cos * yz)) + (sin * x);
            //matrix.M24 = 0f;

            matrix.M31 = (xz - (cos * xz)) + (sin * y);
            matrix.M32 = (yz - (cos * yz)) - (sin * x);
            matrix.M33 = zz + (cos * (1f - zz));
            //matrix.M34 = 0f;

            //matrix.M41 = matrix.M42 = matrix.M43 = 0f;
            matrix.M44 = 1f;

            return matrix;
        }

        /// <summary>
        /// Construct a matrix from euler rotation values in radians
        /// </summary>
        /// <param name="roll">X euler angle in radians</param>
        /// <param name="pitch">Y euler angle in radians</param>
        /// <param name="yaw">Z euler angle in radians</param>
        public static Matrix4 CreateFromEulers(double roll, double pitch, double yaw)
        {
            Matrix4 m;

            double a, b, c, d, e, f;
            double ad, bd;

            a = Math.Cos(roll);
            b = Math.Sin(roll);
            c = Math.Cos(pitch);
            d = Math.Sin(pitch);
            e = Math.Cos(yaw);
            f = Math.Sin(yaw);

            ad = a * d;
            bd = b * d;

            m.M11 = c * e;
            m.M12 = -c * f;
            m.M13 = d;
            m.M14 = 0f;

            m.M21 = bd * e + a * f;
            m.M22 = -bd * f + a * e;
            m.M23 = -b * c;
            m.M24 = 0f;

            m.M31 = -ad * e + b * f;
            m.M32 = ad * f + b * e;
            m.M33 = a * c;
            m.M34 = 0f;

            m.M41 = m.M42 = m.M43 = 0f;
            m.M44 = 1f;

            return m;
        }

        public static Matrix4 CreateFromQuaternion(Quaternion quaternion)
        {
            Matrix4 matrix;

            double xx = quaternion.X * quaternion.X;
            double yy = quaternion.Y * quaternion.Y;
            double zz = quaternion.Z * quaternion.Z;
            double xy = quaternion.X * quaternion.Y;
            double zw = quaternion.Z * quaternion.W;
            double zx = quaternion.Z * quaternion.X;
            double yw = quaternion.Y * quaternion.W;
            double yz = quaternion.Y * quaternion.Z;
            double xw = quaternion.X * quaternion.W;

            matrix.M11 = 1f - (2f * (yy + zz));
            matrix.M12 = 2f * (xy + zw);
            matrix.M13 = 2f * (zx - yw);
            matrix.M14 = 0f;

            matrix.M21 = 2f * (xy - zw);
            matrix.M22 = 1f - (2f * (zz + xx));
            matrix.M23 = 2f * (yz + xw);
            matrix.M24 = 0f;

            matrix.M31 = 2f * (zx + yw);
            matrix.M32 = 2f * (yz - xw);
            matrix.M33 = 1f - (2f * (yy + xx));
            matrix.M34 = 0f;

            matrix.M41 = matrix.M42 = matrix.M43 = 0f;
            matrix.M44 = 1f;

            return matrix;
        }

        public static Matrix4 CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
        {
            Matrix4 matrix;

            Vector3 z = Vector3.Normalize(cameraPosition - cameraTarget);
            Vector3 x = Vector3.Normalize(cameraUpVector.Cross(z));
            Vector3 y = z.Cross(x);

            matrix.M11 = x.X;
            matrix.M12 = y.X;
            matrix.M13 = z.X;
            matrix.M14 = 0f;

            matrix.M21 = x.Y;
            matrix.M22 = y.Y;
            matrix.M23 = z.Y;
            matrix.M24 = 0f;

            matrix.M31 = x.Z;
            matrix.M32 = y.Z;
            matrix.M33 = z.Z;
            matrix.M34 = 0f;

            matrix.M41 = -x.Dot(cameraPosition);
            matrix.M42 = -y.Dot(cameraPosition);
            matrix.M43 = -z.Dot(cameraPosition);
            matrix.M44 = 1f;

            return matrix;
        }

        public static Matrix4 CreateRotationX(double radians)
        {
            Matrix4 matrix;

            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);

            matrix.M11 = 1f;
            matrix.M12 = 0f;
            matrix.M13 = 0f;
            matrix.M14 = 0f;

            matrix.M21 = 0f;
            matrix.M22 = cos;
            matrix.M23 = sin;
            matrix.M24 = 0f;

            matrix.M31 = 0f;
            matrix.M32 = -sin;
            matrix.M33 = cos;
            matrix.M34 = 0f;

            matrix.M41 = 0f;
            matrix.M42 = 0f;
            matrix.M43 = 0f;
            matrix.M44 = 1f;

            return matrix;
        }

        public static Matrix4 CreateRotationY(double radians)
        {
            Matrix4 matrix;

            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);

            matrix.M11 = cos;
            matrix.M12 = 0f;
            matrix.M13 = -sin;
            matrix.M14 = 0f;

            matrix.M21 = 0f;
            matrix.M22 = 1f;
            matrix.M23 = 0f;
            matrix.M24 = 0f;

            matrix.M31 = sin;
            matrix.M32 = 0f;
            matrix.M33 = cos;
            matrix.M34 = 0f;

            matrix.M41 = 0f;
            matrix.M42 = 0f;
            matrix.M43 = 0f;
            matrix.M44 = 1f;

            return matrix;
        }

        public static Matrix4 CreateRotationZ(double radians)
        {
            Matrix4 matrix;

            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);

            matrix.M11 = cos;
            matrix.M12 = sin;
            matrix.M13 = 0f;
            matrix.M14 = 0f;

            matrix.M21 = -sin;
            matrix.M22 = cos;
            matrix.M23 = 0f;
            matrix.M24 = 0f;

            matrix.M31 = 0f;
            matrix.M32 = 0f;
            matrix.M33 = 1f;
            matrix.M34 = 0f;

            matrix.M41 = 0f;
            matrix.M42 = 0f;
            matrix.M43 = 0f;
            matrix.M44 = 1f;

            return matrix;
        }

        public static Matrix4 CreateScale(Vector3 scale)
        {
            Matrix4 matrix;

            matrix.M11 = scale.X;
            matrix.M12 = 0f;
            matrix.M13 = 0f;
            matrix.M14 = 0f;

            matrix.M21 = 0f;
            matrix.M22 = scale.Y;
            matrix.M23 = 0f;
            matrix.M24 = 0f;

            matrix.M31 = 0f;
            matrix.M32 = 0f;
            matrix.M33 = scale.Z;
            matrix.M34 = 0f;

            matrix.M41 = 0f;
            matrix.M42 = 0f;
            matrix.M43 = 0f;
            matrix.M44 = 1f;

            return matrix;
        }

        public static Matrix4 CreateTranslation(Vector3 position)
        {
            Matrix4 matrix;

            matrix.M11 = 1f;
            matrix.M12 = 0f;
            matrix.M13 = 0f;
            matrix.M14 = 0f;

            matrix.M21 = 0f;
            matrix.M22 = 1f;
            matrix.M23 = 0f;
            matrix.M24 = 0f;

            matrix.M31 = 0f;
            matrix.M32 = 0f;
            matrix.M33 = 1f;
            matrix.M34 = 0f;

            matrix.M41 = position.X;
            matrix.M42 = position.Y;
            matrix.M43 = position.Z;
            matrix.M44 = 1f;

            return matrix;
        }

        public static Matrix4 CreateWorld(Vector3 position, Vector3 forward, Vector3 up)
        {
            Matrix4 result;

            // Normalize forward vector
            forward.Normalize();

            // Calculate right vector
            Vector3 right = forward.Cross(up);
            right.Normalize();

            // Recalculate up vector
            up = right.Cross(forward);
            up.Normalize();

            result.M11 = right.X;
            result.M12 = right.Y;
            result.M13 = right.Z;
            result.M14 = 0.0f;

            result.M21 = up.X;
            result.M22 = up.Y;
            result.M23 = up.Z;
            result.M24 = 0.0f;

            result.M31 = -forward.X;
            result.M32 = -forward.Y;
            result.M33 = -forward.Z;
            result.M34 = 0.0f;

            result.M41 = position.X;
            result.M42 = position.Y;
            result.M43 = position.Z;
            result.M44 = 1.0f;

            return result;
        }

        public static Matrix4 operator/(Matrix4 matrix1, Matrix4 matrix2)
        {
            Matrix4 matrix;

            matrix.M11 = matrix1.M11 / matrix2.M11;
            matrix.M12 = matrix1.M12 / matrix2.M12;
            matrix.M13 = matrix1.M13 / matrix2.M13;
            matrix.M14 = matrix1.M14 / matrix2.M14;

            matrix.M21 = matrix1.M21 / matrix2.M21;
            matrix.M22 = matrix1.M22 / matrix2.M22;
            matrix.M23 = matrix1.M23 / matrix2.M23;
            matrix.M24 = matrix1.M24 / matrix2.M24;

            matrix.M31 = matrix1.M31 / matrix2.M31;
            matrix.M32 = matrix1.M32 / matrix2.M32;
            matrix.M33 = matrix1.M33 / matrix2.M33;
            matrix.M34 = matrix1.M34 / matrix2.M34;

            matrix.M41 = matrix1.M41 / matrix2.M41;
            matrix.M42 = matrix1.M42 / matrix2.M42;
            matrix.M43 = matrix1.M43 / matrix2.M43;
            matrix.M44 = matrix1.M44 / matrix2.M44;

            return matrix;
        }

        public static Matrix4 operator/(Matrix4 matrix1, double divider)
        {
            Matrix4 matrix;

            double oodivider = 1f / divider;
            matrix.M11 = matrix1.M11 * oodivider;
            matrix.M12 = matrix1.M12 * oodivider;
            matrix.M13 = matrix1.M13 * oodivider;
            matrix.M14 = matrix1.M14 * oodivider;

            matrix.M21 = matrix1.M21 * oodivider;
            matrix.M22 = matrix1.M22 * oodivider;
            matrix.M23 = matrix1.M23 * oodivider;
            matrix.M24 = matrix1.M24 * oodivider;

            matrix.M31 = matrix1.M31 * oodivider;
            matrix.M32 = matrix1.M32 * oodivider;
            matrix.M33 = matrix1.M33 * oodivider;
            matrix.M34 = matrix1.M34 * oodivider;

            matrix.M41 = matrix1.M41 * oodivider;
            matrix.M42 = matrix1.M42 * oodivider;
            matrix.M43 = matrix1.M43 * oodivider;
            matrix.M44 = matrix1.M44 * oodivider;

            return matrix;
        }

        public static Matrix4 Lerp(Matrix4 matrix1, Matrix4 matrix2, double amount)
        {
            Matrix4 matrix;

            matrix.M11 = matrix1.M11 + ((matrix2.M11 - matrix1.M11) * amount);
            matrix.M12 = matrix1.M12 + ((matrix2.M12 - matrix1.M12) * amount);
            matrix.M13 = matrix1.M13 + ((matrix2.M13 - matrix1.M13) * amount);
            matrix.M14 = matrix1.M14 + ((matrix2.M14 - matrix1.M14) * amount);

            matrix.M21 = matrix1.M21 + ((matrix2.M21 - matrix1.M21) * amount);
            matrix.M22 = matrix1.M22 + ((matrix2.M22 - matrix1.M22) * amount);
            matrix.M23 = matrix1.M23 + ((matrix2.M23 - matrix1.M23) * amount);
            matrix.M24 = matrix1.M24 + ((matrix2.M24 - matrix1.M24) * amount);

            matrix.M31 = matrix1.M31 + ((matrix2.M31 - matrix1.M31) * amount);
            matrix.M32 = matrix1.M32 + ((matrix2.M32 - matrix1.M32) * amount);
            matrix.M33 = matrix1.M33 + ((matrix2.M33 - matrix1.M33) * amount);
            matrix.M34 = matrix1.M34 + ((matrix2.M34 - matrix1.M34) * amount);

            matrix.M41 = matrix1.M41 + ((matrix2.M41 - matrix1.M41) * amount);
            matrix.M42 = matrix1.M42 + ((matrix2.M42 - matrix1.M42) * amount);
            matrix.M43 = matrix1.M43 + ((matrix2.M43 - matrix1.M43) * amount);
            matrix.M44 = matrix1.M44 + ((matrix2.M44 - matrix1.M44) * amount);

            return matrix;
        }

        public static Matrix4 operator*(Matrix4 matrix1, Matrix4 matrix2)
        {
            return new Matrix4(
                matrix1.M11 * matrix2.M11 + matrix1.M12 * matrix2.M21 + matrix1.M13 * matrix2.M31 + matrix1.M14 * matrix2.M41,
                matrix1.M11 * matrix2.M12 + matrix1.M12 * matrix2.M22 + matrix1.M13 * matrix2.M32 + matrix1.M14 * matrix2.M42,
                matrix1.M11 * matrix2.M13 + matrix1.M12 * matrix2.M23 + matrix1.M13 * matrix2.M33 + matrix1.M14 * matrix2.M43,
                matrix1.M11 * matrix2.M14 + matrix1.M12 * matrix2.M24 + matrix1.M13 * matrix2.M34 + matrix1.M14 * matrix2.M44,

                matrix1.M21 * matrix2.M11 + matrix1.M22 * matrix2.M21 + matrix1.M23 * matrix2.M31 + matrix1.M24 * matrix2.M41,
                matrix1.M21 * matrix2.M12 + matrix1.M22 * matrix2.M22 + matrix1.M23 * matrix2.M32 + matrix1.M24 * matrix2.M42,
                matrix1.M21 * matrix2.M13 + matrix1.M22 * matrix2.M23 + matrix1.M23 * matrix2.M33 + matrix1.M24 * matrix2.M43,
                matrix1.M21 * matrix2.M14 + matrix1.M22 * matrix2.M24 + matrix1.M23 * matrix2.M34 + matrix1.M24 * matrix2.M44,

                matrix1.M31 * matrix2.M11 + matrix1.M32 * matrix2.M21 + matrix1.M33 * matrix2.M31 + matrix1.M34 * matrix2.M41,
                matrix1.M31 * matrix2.M12 + matrix1.M32 * matrix2.M22 + matrix1.M33 * matrix2.M32 + matrix1.M34 * matrix2.M42,
                matrix1.M31 * matrix2.M13 + matrix1.M32 * matrix2.M23 + matrix1.M33 * matrix2.M33 + matrix1.M34 * matrix2.M43,
                matrix1.M31 * matrix2.M14 + matrix1.M32 * matrix2.M24 + matrix1.M33 * matrix2.M34 + matrix1.M34 * matrix2.M44,

                matrix1.M41 * matrix2.M11 + matrix1.M42 * matrix2.M21 + matrix1.M43 * matrix2.M31 + matrix1.M44 * matrix2.M41,
                matrix1.M41 * matrix2.M12 + matrix1.M42 * matrix2.M22 + matrix1.M43 * matrix2.M32 + matrix1.M44 * matrix2.M42,
                matrix1.M41 * matrix2.M13 + matrix1.M42 * matrix2.M23 + matrix1.M43 * matrix2.M33 + matrix1.M44 * matrix2.M43,
                matrix1.M41 * matrix2.M14 + matrix1.M42 * matrix2.M24 + matrix1.M43 * matrix2.M34 + matrix1.M44 * matrix2.M44
            );
        }

        public static Matrix4 operator*(Matrix4 matrix1, double scaleFactor)
        {
            Matrix4 matrix;
            matrix.M11 = matrix1.M11 * scaleFactor;
            matrix.M12 = matrix1.M12 * scaleFactor;
            matrix.M13 = matrix1.M13 * scaleFactor;
            matrix.M14 = matrix1.M14 * scaleFactor;

            matrix.M21 = matrix1.M21 * scaleFactor;
            matrix.M22 = matrix1.M22 * scaleFactor;
            matrix.M23 = matrix1.M23 * scaleFactor;
            matrix.M24 = matrix1.M24 * scaleFactor;

            matrix.M31 = matrix1.M31 * scaleFactor;
            matrix.M32 = matrix1.M32 * scaleFactor;
            matrix.M33 = matrix1.M33 * scaleFactor;
            matrix.M34 = matrix1.M34 * scaleFactor;

            matrix.M41 = matrix1.M41 * scaleFactor;
            matrix.M42 = matrix1.M42 * scaleFactor;
            matrix.M43 = matrix1.M43 * scaleFactor;
            matrix.M44 = matrix1.M44 * scaleFactor;
            return matrix;
        }

        public static Matrix4 operator-(Matrix4 matrix)
        {
            Matrix4 result = new Matrix4();
            result.M11 = -matrix.M11;
            result.M12 = -matrix.M12;
            result.M13 = -matrix.M13;
            result.M14 = -matrix.M14;

            result.M21 = -matrix.M21;
            result.M22 = -matrix.M22;
            result.M23 = -matrix.M23;
            result.M24 = -matrix.M24;

            result.M31 = -matrix.M31;
            result.M32 = -matrix.M32;
            result.M33 = -matrix.M33;
            result.M34 = -matrix.M34;

            result.M41 = -matrix.M41;
            result.M42 = -matrix.M42;
            result.M43 = -matrix.M43;
            result.M44 = -matrix.M44;
            return result;
        }

        public static Matrix4 operator-(Matrix4 matrix1, Matrix4 matrix2)
        {
            Matrix4 matrix = new Matrix4();
            matrix.M11 = matrix1.M11 - matrix2.M11;
            matrix.M12 = matrix1.M12 - matrix2.M12;
            matrix.M13 = matrix1.M13 - matrix2.M13;
            matrix.M14 = matrix1.M14 - matrix2.M14;

            matrix.M21 = matrix1.M21 - matrix2.M21;
            matrix.M22 = matrix1.M22 - matrix2.M22;
            matrix.M23 = matrix1.M23 - matrix2.M23;
            matrix.M24 = matrix1.M24 - matrix2.M24;

            matrix.M31 = matrix1.M31 - matrix2.M31;
            matrix.M32 = matrix1.M32 - matrix2.M32;
            matrix.M33 = matrix1.M33 - matrix2.M33;
            matrix.M34 = matrix1.M34 - matrix2.M34;

            matrix.M41 = matrix1.M41 - matrix2.M41;
            matrix.M42 = matrix1.M42 - matrix2.M42;
            matrix.M43 = matrix1.M43 - matrix2.M43;
            matrix.M44 = matrix1.M44 - matrix2.M44;
            return matrix;
        }

        public static Matrix4 Transform(Matrix4 value, Quaternion rotation)
        {
            Matrix4 matrix;

            double x2 = rotation.X + rotation.X;
            double y2 = rotation.Y + rotation.Y;
            double z2 = rotation.Z + rotation.Z;

            double a = (1f - rotation.Y * y2) - rotation.Z * z2;
            double b = rotation.X * y2 - rotation.W * z2;
            double c = rotation.X * z2 + rotation.W * y2;
            double d = rotation.X * y2 + rotation.W * z2;
            double e = (1f - rotation.X * x2) - rotation.Z * z2;
            double f = rotation.Y * z2 - rotation.W * x2;
            double g = rotation.X * z2 - rotation.W * y2;
            double h = rotation.Y * z2 + rotation.W * x2;
            double i = (1f - rotation.X * x2) - rotation.Y * y2;

            matrix.M11 = ((value.M11 * a) + (value.M12 * b)) + (value.M13 * c);
            matrix.M12 = ((value.M11 * d) + (value.M12 * e)) + (value.M13 * f);
            matrix.M13 = ((value.M11 * g) + (value.M12 * h)) + (value.M13 * i);
            matrix.M14 = value.M14;

            matrix.M21 = ((value.M21 * a) + (value.M22 * b)) + (value.M23 * c);
            matrix.M22 = ((value.M21 * d) + (value.M22 * e)) + (value.M23 * f);
            matrix.M23 = ((value.M21 * g) + (value.M22 * h)) + (value.M23 * i);
            matrix.M24 = value.M24;

            matrix.M31 = ((value.M31 * a) + (value.M32 * b)) + (value.M33 * c);
            matrix.M32 = ((value.M31 * d) + (value.M32 * e)) + (value.M33 * f);
            matrix.M33 = ((value.M31 * g) + (value.M32 * h)) + (value.M33 * i);
            matrix.M34 = value.M34;

            matrix.M41 = ((value.M41 * a) + (value.M42 * b)) + (value.M43 * c);
            matrix.M42 = ((value.M41 * d) + (value.M42 * e)) + (value.M43 * f);
            matrix.M43 = ((value.M41 * g) + (value.M42 * h)) + (value.M43 * i);
            matrix.M44 = value.M44;

            return matrix;
        }

        public static Matrix4 Transpose(Matrix4 matrix)
        {
            Matrix4 result;

            result.M11 = matrix.M11;
            result.M12 = matrix.M21;
            result.M13 = matrix.M31;
            result.M14 = matrix.M41;

            result.M21 = matrix.M12;
            result.M22 = matrix.M22;
            result.M23 = matrix.M32;
            result.M24 = matrix.M42;

            result.M31 = matrix.M13;
            result.M32 = matrix.M23;
            result.M33 = matrix.M33;
            result.M34 = matrix.M43;

            result.M41 = matrix.M14;
            result.M42 = matrix.M24;
            result.M43 = matrix.M34;
            result.M44 = matrix.M44;

            return result;
        }

        public static Matrix4 Inverse3x3(Matrix4 matrix)
        {
            if (matrix.Determinant3x3 == 0f)
                throw new ArgumentException("Singular matrix inverse not possible");

            return (Adjoint3x3(matrix) / matrix.Determinant3x3);
        }

        public static Matrix4 Adjoint3x3(Matrix4 matrix)
        {
            Matrix4 adjointMatrix = new Matrix4();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                    adjointMatrix[i, j] = (Math.Pow(-1, i + j) * (Minor(matrix, i, j).Determinant3x3));
            }

            adjointMatrix = Transpose(adjointMatrix);
            return adjointMatrix;
        }

        public static Matrix4 Inverse(Matrix4 matrix)
        {
            if (matrix.Determinant == 0f)
                throw new ArgumentException("Singular matrix inverse not possible");

            return (Adjoint(matrix) / matrix.Determinant);
        }

        public static Matrix4 Adjoint(Matrix4 matrix)
        {
            Matrix4 adjointMatrix = new Matrix4();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                    adjointMatrix[i, j] = (Math.Pow(-1, i + j) * ((Minor(matrix, i, j)).Determinant));
            }

            adjointMatrix = Transpose(adjointMatrix);
            return adjointMatrix;
        }

        public static Matrix4 Minor(Matrix4 matrix, int row, int col)
        {
            Matrix4 minor = new Matrix4();
            int m = 0, n = 0;

            for (int i = 0; i < 4; i++)
            {
                if (i == row)
                    continue;
                n = 0;
                for (int j = 0; j < 4; j++)
                {
                    if (j == col)
                        continue;
                    minor[m, n] = matrix[i, j];
                    n++;
                }
                m++;
            }

            return minor;
        }

        #endregion Static Methods

        #region Overrides

        public override bool Equals(object obj)
        {
            return (obj is Matrix4) ? this == (Matrix4)obj : false;
        }

        public bool Equals(Matrix4 other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return
                M11.GetHashCode() ^ M12.GetHashCode() ^ M13.GetHashCode() ^ M14.GetHashCode() ^
                M21.GetHashCode() ^ M22.GetHashCode() ^ M23.GetHashCode() ^ M24.GetHashCode() ^
                M31.GetHashCode() ^ M32.GetHashCode() ^ M33.GetHashCode() ^ M34.GetHashCode() ^
                M41.GetHashCode() ^ M42.GetHashCode() ^ M43.GetHashCode() ^ M44.GetHashCode();
        }

        /// <summary>
        /// Get a formatted string representation of the vector
        /// </summary>
        /// <returns>A string representation of the vector</returns>
        public override string ToString()
        {
            return string.Format(EnUsCulture,
                "|{0}, {1}, {2}, {3}|\n|{4}, {5}, {6}, {7}|\n|{8}, {9}, {10}, {11}|\n|{12}, {13}, {14}, {15}|",
                M11, M12, M13, M14, M21, M22, M23, M24, M31, M32, M33, M34, M41, M42, M43, M44);
        }

        #endregion Overrides

        #region Operators

        public static bool operator ==(Matrix4 left, Matrix4 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Matrix4 left, Matrix4 right)
        {
            return !left.Equals(right);
        }

        public double this[int row, int column]
        {
            get
            {
                switch (row)
                {
                    case 0:
                        switch (column)
                        {
                            case 0:
                                return M11;
                            case 1:
                                return M12;
                            case 2:
                                return M13;
                            case 3:
                                return M14;
                            default:
                                throw new IndexOutOfRangeException("Matrix4 row and column values must be from 0-3");
                        }
                    case 1:
                        switch (column)
                        {
                            case 0:
                                return M21;
                            case 1:
                                return M22;
                            case 2:
                                return M23;
                            case 3:
                                return M24;
                            default:
                                throw new IndexOutOfRangeException("Matrix4 row and column values must be from 0-3");
                        }
                    case 2:
                        switch (column)
                        {
                            case 0:
                                return M31;
                            case 1:
                                return M32;
                            case 2:
                                return M33;
                            case 3:
                                return M34;
                            default:
                                throw new IndexOutOfRangeException("Matrix4 row and column values must be from 0-3");
                        }
                    case 3:
                        switch (column)
                        {
                            case 0:
                                return M41;
                            case 1:
                                return M42;
                            case 2:
                                return M43;
                            case 3:
                                return M44;
                            default:
                                throw new IndexOutOfRangeException("Matrix4 row and column values must be from 0-3");
                        }
                    default:
                        throw new IndexOutOfRangeException("Matrix4 row and column values must be from 0-3");
                }
            }
            set
            {
                switch (row)
                {
                    case 0:
                        switch (column)
                        {
                            case 0:
                                M11 = value; return;
                            case 1:
                                M12 = value; return;
                            case 2:
                                M13 = value; return;
                            case 3:
                                M14 = value; return;
                            default:
                                throw new IndexOutOfRangeException("Matrix4 row and column values must be from 0-3");
                        }
                    case 1:
                        switch (column)
                        {
                            case 0:
                                M21 = value; return;
                            case 1:
                                M22 = value; return;
                            case 2:
                                M23 = value; return;
                            case 3:
                                M24 = value; return;
                            default:
                                throw new IndexOutOfRangeException("Matrix4 row and column values must be from 0-3");
                        }
                    case 2:
                        switch (column)
                        {
                            case 0:
                                M31 = value; return;
                            case 1:
                                M32 = value; return;
                            case 2:
                                M33 = value; return;
                            case 3:
                                M34 = value; return;
                            default:
                                throw new IndexOutOfRangeException("Matrix4 row and column values must be from 0-3");
                        }
                    case 3:
                        switch (column)
                        {
                            case 0:
                                M41 = value; return;
                            case 1:
                                M42 = value; return;
                            case 2:
                                M43 = value; return;
                            case 3:
                                M44 = value; return;
                            default:
                                throw new IndexOutOfRangeException("Matrix4 row and column values must be from 0-3");
                        }
                    default:
                        throw new IndexOutOfRangeException("Matrix4 row and column values must be from 0-3");
                }
            }
        }

        #endregion Operators

        /// <summary>A 4x4 matrix containing all zeroes</summary>
        public static readonly Matrix4 Zero = new Matrix4();

        /// <summary>A 4x4 identity matrix</summary>
        public static readonly Matrix4 Identity = new Matrix4(
            1f, 0f, 0f, 0f,
            0f, 1f, 0f, 0f,
            0f, 0f, 1f, 0f,
            0f, 0f, 0f, 1f);

        private readonly static CultureInfo EnUsCulture = new CultureInfo("en-us");
    }
}
