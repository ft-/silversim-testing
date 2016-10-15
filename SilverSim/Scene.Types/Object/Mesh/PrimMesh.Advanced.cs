// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset.Format.Mesh;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Object.Mesh
{
    public static partial class PrimMesh
    {
        #region extrude advanced
        static List<Vector3> ExtrudeAdvanced(this PathDetails path, ObjectPart.PrimitiveShape.Decoded shape, double twistBegin, double twistEnd, double cut)
        {
            List<Vector3> extrusionPath = new List<Vector3>();
            double twist;
            double angle = 0.0.Lerp(shape.Revolutions * 2 * Math.PI, cut);
            Vector3 topSize;
            Vector3 shear;
            Vector3 taper = new Vector3();
            double radiusOffset;

            CalcTopSizeAndShear(shape, twistBegin, twistEnd, cut, out topSize, out shear, out twist);
            shear.Z = shear.X;
            shear.X = 0;

            #region taper
            taper.Z = shape.Taper.X < 0f ?
                0.0.Lerp(shape.Taper.X, 1f - cut) :
                0.0.Lerp(shape.Taper.X, cut);

            taper.Y = shape.Taper.Y < 0f ?
                0.0.Lerp(shape.Taper.Y, 1f - cut) :
                0.0.Lerp(shape.Taper.Y, cut);
            #endregion

            #region radius offset
            radiusOffset = shape.RadiusOffset < 0f ?
                0.0.Lerp(shape.RadiusOffset, 1f - cut) :
                0.0.Lerp(shape.RadiusOffset, cut);
            #endregion

            /* generate extrusions */
            double pathscale = (1 - shape.PathScale.Y) * 0.5;
            foreach (Vector3 vertex in path.Vertices)
            {
                Vector3 outvertex = vertex;
                //outvertex.Z *= topSize.X;
                //outvertex.Y *= topSize.Y;
                outvertex = outvertex.Rotate2D_YZ(twist);
                outvertex += shear;

                //outvertex.Z *= shape.PathScale.X;
                /*outvertex.Y *= taper.Y;
                outvertex.Z *= taper.Z;
                outvertex.Z *= (1 / (shape.Skew * shape.Revolutions));
                outvertex.Z += shape.Skew * shape.Revolutions * cut;*/

                outvertex.Y = outvertex.Y * pathscale + 0.5 - pathscale * 0.5;

                outvertex = outvertex.Rotate2D_XY(-angle);
                outvertex.Z += outvertex.X * shape.TopShear.X;
                outvertex.Y += outvertex.X * shape.TopShear.Y;
                //outvertex.X *= radiusOffset;
                //outvertex.Y *= radiusOffset;

                extrusionPath.Add(outvertex);
            }
            return extrusionPath;
        }
        #endregion

        #region advanced mesh generator
        static MeshLOD AdvancedToMesh(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            PathDetails path;
            switch (shape.ShapeType)
            {
                case PrimitiveShapeType.Ring:
                    path = CalcPrismPath(shape);
                    break;

                case PrimitiveShapeType.Torus:
                    path = CalcCylinderPath(shape);
                    break;

                case PrimitiveShapeType.Tube:
                    path = CalcBoxPath(shape);
                    break;

                case PrimitiveShapeType.Sphere:
                    path = CalcSpherePath(shape);
                    break;

                default:
                    throw new NotImplementedException();
            }

            double cut = shape.PathBegin;
            double cutBegin = cut;
            double cutEnd = shape.PathEnd;
            double cutStep = (cutEnd - cut) / 36f / shape.Revolutions;
            double twistBegin = shape.TwistBegin * Math.PI * 2;
            double twistEnd = shape.TwistEnd * Math.PI * 2;

            MeshLOD mesh = new MeshLOD();
            if (shape.ShapeType == PrimitiveShapeType.Sphere)
            {
                for (; cut < cutEnd; cut += cutStep)
                {
                    mesh.Vertices.AddRange(path.ExtrudeSphere(shape, twistBegin, twistEnd, cut));
                }
                mesh.Vertices.AddRange(path.ExtrudeSphere(shape, twistBegin, twistEnd, cutEnd));
            }
            else
            {
                for (; cut < cutEnd; cut += cutStep)
                {
                    mesh.Vertices.AddRange(path.ExtrudeAdvanced(shape, twistBegin, twistEnd, cut));
                }
                mesh.Vertices.AddRange(path.ExtrudeAdvanced(shape, twistBegin, twistEnd, cutEnd));
            }

            shape.BuildTriangles(mesh, path, cutBegin, cutEnd);

            return mesh;
        }
        #endregion
    }
}
