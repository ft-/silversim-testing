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

using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        #region extrude sphere
        static List<Vector3> ExtrudeSphere(this PathDetails path, ObjectPart.PrimitiveShape.Decoded shape, double twistBegin, double twistEnd, double cut)
        {
            List<Vector3> extrusionPath = new List<Vector3>();
            double twist = twistBegin.Lerp(twistEnd, cut);
            double angle = 0.0.Lerp(shape.Revolutions * 2 * Math.PI, cut);
            Vector3 topSize = new Vector3();
            Vector3 taper = new Vector3();
            double radiusOffset;

            #region cut
            if (shape.PathScale.X < 0f)
            {
                topSize.X = 1.0.Clamp(1f + shape.PathScale.X, 1f - cut);
            }
            else
            {
                topSize.X = 1.0.Clamp(1f - shape.PathScale.X, cut);
            }
            if (shape.PathScale.Y < 0f)
            {
                topSize.Y = 1.0.Clamp(1f + shape.PathScale.Y, 1f - cut);
            }
            else
            {
                topSize.Y = 1.0.Clamp(1f - shape.PathScale.Y, cut);
            }
            #endregion

            #region taper
            if (shape.Taper.X < 0f)
            {
                taper.X = 1.0.Clamp(1f + shape.Taper.X, 1f - cut);
            }
            else
            {
                taper.X = 1.0.Clamp(1f - shape.Taper.X, cut);
            }
            if (shape.Taper.Y < 0f)
            {
                taper.Y = 1.0.Clamp(1f + shape.Taper.Y, 1f - cut);
            }
            else
            {
                taper.Y = 1.0.Clamp(1f - shape.Taper.Y, cut);
            }
            #endregion

            #region radius offset
            if (shape.RadiusOffset < 0f)
            {
                radiusOffset = 1.0.Clamp(1f + shape.RadiusOffset, 1f - cut);
            }
            else
            {
                radiusOffset = 1.0.Clamp(1f - shape.RadiusOffset, cut);
            }
            #endregion

            /* generate extrusions */
            foreach (Vector3 vertex in path.Vertices)
            {
                Vector3 outvertex = vertex;
                outvertex.X *= topSize.X;
                outvertex.Y = 1.0.Clamp(outvertex.Y, shape.PathScale.Y);
                outvertex.Y *= topSize.Y;
                outvertex += shape.TopShear;
                outvertex.X *= shape.PathScale.X;
                outvertex.Y *= taper.Y;
                outvertex.Z *= taper.Z;
                outvertex = outvertex.Rotate2D_YZ(twist);
                outvertex.Z *= (1 / (shape.Skew * shape.Revolutions));
                outvertex.Z += shape.Skew * shape.Revolutions * cut;
                outvertex += outvertex.Rotate2D_XY(angle);
                outvertex.X *= radiusOffset;
                outvertex.Y *= radiusOffset;

                extrusionPath.Add(outvertex);
            }
            return extrusionPath;
        }
        #endregion

        #region Calculate Sphere 2D Path
        static Vector3 CalcTrigonInSpherePoint(double angle)
        {
            Vector3 c_p1;
            Vector3 c_p2;
            Vector3 c_p3;

            if (angle < Math.PI / 3)
            {
                c_p1 = TRIGON_P0;
                c_p2 = TRIGON_P1;
            }
            else if(angle < Math.PI * 2 / 3)
            {
                c_p1 = TRIGON_P1;
                c_p2 = TRIGON_P2;
            }
            {
                c_p1 = TRIGON_P2;
                c_p2 = TRIGON_P3;
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

        static Vector3 CalcSquareInSpherePoint(double angle)
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

        static PathDetails CalcSpherePath(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            /* calculate a half-sphere here 
             * starting from UnitX to -UnitX
             */
            PathDetails Path = new PathDetails();

            double startangle = Math.PI * shape.ProfileBegin;
            double endangle = Math.PI * shape.ProfileEnd;
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
                            /* Even though, the option is called Triangle. It is actually a Trigon. */
                            innerDirectionalVec = CalcTrigonInSpherePoint(startangle) * (shape.ProfileHollow * 0.5);
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
