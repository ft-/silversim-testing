// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scene.Types.Object.Mesh
{
    public static class ExtensionMethods
    {
        public static Mesh ToMesh(this ObjectPart part, AssetServiceInterface assetService)
        {
            return part.Shape.DecodedParams.ToMesh(assetService);
        }

        public static Mesh ToMesh(this ObjectPart.PrimitiveShape shape, AssetServiceInterface assetService)
        {
            return shape.DecodedParams.ToMesh(assetService);
        }

        public static Mesh ToMesh(this ObjectPart.PrimitiveShape.Decoded shape, AssetServiceInterface assetService, bool usePhysicsMesh = false)
        {
            Mesh mesh;
            switch(shape.ShapeType)
            {
                case PrimitiveShapeType.Sculpt:
                    switch(shape.SculptType & PrimitiveSculptType.TypeMask)
                    {
                        case PrimitiveSculptType.Mesh:
                            mesh = assetService[shape.SculptMap].LLMeshToMesh(shape, usePhysicsMesh);
                            break;

                        default:
                            mesh = assetService[shape.SculptMap].SculptMeshToMesh(shape);
                            break;
                    }
                    break;

                default:
                    mesh = shape.ShapeToMesh();
                    break;
            }

            /* clean up de-generate triangles and duplicate vertices */
            mesh.Optimize();

            return mesh;
        }
    }
}
