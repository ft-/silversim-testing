// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;

namespace SilverSim.Scene.Types.Object.Mesh
{
    public static partial class PrimMesh
    {
        static readonly Vector3 START_VECTOR_BOX;

        static readonly Vector3 TRAPEZOID_P0;
        static readonly Vector3 TRAPEZOID_P1;
        static readonly Vector3 TRAPEZOID_P2;
        static readonly Vector3 TRAPEZOID_P3;

        static PrimMesh()
        {
            START_VECTOR_BOX = new Vector3(-1, -1, 0).Normalize();

            /* the hole shape inside a sphere is a trapezoid but not a tri-angle.
             * So, it is called trapezoid here.
             */
            TRAPEZOID_P0 = Vector3.UnitX;
            TRAPEZOID_P1 = TRAPEZOID_P0.Rotate2D_XY(60 / 180 * Math.PI);
            TRAPEZOID_P2 = TRAPEZOID_P0.Rotate2D_XY(120 / 180 * Math.PI);
            TRAPEZOID_P3 = -Vector3.UnitX;
        }

        internal static Mesh ShapeToMesh(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            Mesh mesh;
            switch (shape.ShapeType)
            {
                case PrimitiveShapeType.Box:
                case PrimitiveShapeType.Cylinder:
                case PrimitiveShapeType.Prism:
                    mesh = shape.BoxCylPrismToMesh();
                    break;

                case PrimitiveShapeType.Ring:
                case PrimitiveShapeType.Torus:
                case PrimitiveShapeType.Tube:
                case PrimitiveShapeType.Sphere:
                    mesh = shape.AdvancedToMesh();
                    break;

                default:
                    throw new NotImplementedException();
            }

            return mesh;
        }

    }
}
