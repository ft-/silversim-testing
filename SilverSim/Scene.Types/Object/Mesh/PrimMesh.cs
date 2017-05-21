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
using SilverSim.Types.Asset.Format.Mesh;
using SilverSim.Types.Primitive;
using System;

namespace SilverSim.Scene.Types.Object.Mesh
{
    public static partial class PrimMesh
    {
        private static readonly Vector3 START_VECTOR_BOX;

        private static readonly Vector3 TRAPEZOID_P0;
        private static readonly Vector3 TRAPEZOID_P1;
        private static readonly Vector3 TRAPEZOID_P2;
        private static readonly Vector3 TRAPEZOID_P3;

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

        internal static MeshLOD ShapeToMesh(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            MeshLOD mesh;
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
