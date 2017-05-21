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

using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;

namespace SilverSim.Scene.Types.Object.Mesh
{
    public static partial class PrimMesh
    {
        /*
                            paramList.Add(ProfileCurve & 0xF0); => hole_shape
                            paramList.Add(new Vector3(PathBegin / 50000f, 1 - PathEnd / 50000f, 0)); => cut
                            paramList.Add(ProfileHollow / 50000f); => hollow
                            paramList.Add(new Vector3(PathTwistBegin / 100f, PathTwist / 100f, 0)); => twist
                            paramList.Add(new Vector3(ProfileBegin / 50000f, 1 - ProfileEnd / 50000f, 0)); => dimple
         * 
         * additionally, pathscale, revolutions and skew avail
         */

        #region Calculate Sphere 2D Path
        private static Vector3 CalcTrapezoidInSpherePoint(double angle)
        {
            Vector3 c_p1;
            Vector3 c_p2;
            Vector3 c_p3;

            if (angle < Math.PI / 3)
            {
                c_p1 = TRAPEZOID_P0;
                c_p2 = TRAPEZOID_P1;
            }
            else if(angle < Math.PI * 2 / 3)
            {
                c_p1 = TRAPEZOID_P1;
                c_p2 = TRAPEZOID_P2;
            }
            {
                c_p1 = TRAPEZOID_P2;
                c_p2 = TRAPEZOID_P3;
            }

            double above_div_common;
            double x_above_div;
            double below_div;
            double y_above_div;

            c_p3 = new Vector3(Math.Cos(angle), Math.Sin(angle), 0);

            /* According to Cramer, we can calculate from two lines the intersection point.
             * The original algorithm has four points (two per line) but our fourth is always the Zero Vector.
             * So, point 4 is eliminated.
             */
            above_div_common = -(c_p2.X * c_p1.Y - c_p1.X * c_p2.Y);
            x_above_div = c_p3.X * above_div_common;
            below_div = (-c_p3.Y) * (c_p2.X - c_p1.X) - (c_p2.Y - c_p1.Y) * (-c_p3.X);

            y_above_div = c_p3.Y * above_div_common;
            return new Vector3(x_above_div / below_div, y_above_div / below_div, 0);
        }

        private static Vector3 CalcSquareInSpherePoint(double angle)
        {
            /*                      p0 (0.5, 0.0)
             *                      /\
             *              side 3 /  \ side 1
             * (-0.5, -0.5)    p2 /____\  p1 (0.5, -0.5)
             */
            Vector3 c_p1;
            Vector3 c_p2;
            Vector3 c_p3;

            if (angle < Math.PI / 2)
            {
                c_p1 = Vector3.UnitX;
                c_p2 = Vector3.UnitY;
            }
            {
                c_p1 = -Vector3.UnitX;
                c_p2 = Vector3.UnitY;
            }

            double above_div_common;
            double x_above_div;
            double below_div;
            double y_above_div;

            c_p3 = new Vector3(Math.Cos(angle), Math.Sin(angle), 0);

            /* According to Cramer, we can calculate from two lines the intersection point.
             * The original algorithm has four points (two per line) but our fourth is always the Zero Vector.
             * So, point 4 is eliminated.
             */
            above_div_common = -(c_p2.X * c_p1.Y - c_p1.X * c_p2.Y);
            x_above_div = c_p3.X * above_div_common;
            below_div = (-c_p3.Y) * (c_p2.X - c_p1.X) - (c_p2.Y - c_p1.Y) * (-c_p3.X);

            y_above_div = c_p3.Y * above_div_common;
            return new Vector3(x_above_div / below_div, y_above_div / below_div, 0);
        }

        private static PathDetails CalcSpherePath(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            /* calculate a half-sphere here 
             * starting from UnitX to -UnitX
             */
            var Path = new PathDetails();

            double startangle = Math.PI * shape.ProfileBegin;
            double endangle = Math.PI * (1f - shape.ProfileEnd);
            double stepangle = (endangle - startangle) / 60;

            if (shape.IsHollow)
            {
                /* we calculate two points */
                Vector3 startPoint = Vector3.UnitX * 0.5;
                for (; startangle < endangle; startangle += stepangle)
                {
                    Vector3 outerDirectionalVec = startPoint.Rotate2D_XY(startangle);
                    Vector3 innerDirectionalVec;

                    switch (shape.HoleShape)
                    {
                        case PrimitiveProfileHollowShape.Triangle:
                            /* Even though, the option is called Triangle. It is actually a Trapezoid. */
                            innerDirectionalVec = CalcTrapezoidInSpherePoint(startangle) * (shape.ProfileHollow * 0.5);
                            break;

                        case PrimitiveProfileHollowShape.Same:
                        case PrimitiveProfileHollowShape.Circle:
                            /* circle is simple as we are calculating with such objects */
                            innerDirectionalVec = CalcTrianglePoint(startangle) * shape.ProfileHollow;
                            break;

                        case PrimitiveProfileHollowShape.Square:
                            innerDirectionalVec = CalcSquareInSpherePoint(startangle) * (shape.ProfileHollow * 0.5);
                            break;

                        default:
                            throw new NotImplementedException();
                    }

                    outerDirectionalVec.Z = outerDirectionalVec.X;
                    innerDirectionalVec.Z = innerDirectionalVec.X;
                    outerDirectionalVec.X = 0;
                    innerDirectionalVec.X = 0;

                    /* inner path is reversed */
                    Path.Vertices.Add(outerDirectionalVec);
                    Path.Vertices.Insert(0, innerDirectionalVec);

                }
                /* no center point here, even though we can have path cut */
                Path.IsOpenHollow = shape.IsOpen;
            }
            else
            {
                /* no hollow, so it becomes simple */
                Vector3 startPoint = Vector3.UnitX * 0.5;
                for (; startangle < endangle; startangle += stepangle)
                {
                    Vector3 directionalVec = startPoint.Rotate2D_XY(startangle);
                    directionalVec.Z = directionalVec.X;
                    directionalVec.X = 0;
                    Path.Vertices.Add(directionalVec);
                }
                if (shape.IsOpen)
                {
                    Path.Vertices.Add(new Vector3(0, 0, 0));
                }
            }

            return Path;

        }
        #endregion
    }
}
