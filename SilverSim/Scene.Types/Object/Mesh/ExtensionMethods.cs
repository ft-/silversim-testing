// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types.Asset.Format.Mesh;
using SilverSim.Types.Primitive;

namespace SilverSim.Scene.Types.Object.Mesh
{
    public static class ExtensionMethods
    {
        public static MeshLOD ToMesh(this ObjectPart part, AssetServiceInterface assetService)
        {
            return part.Shape.DecodedParams.ToMesh(assetService);
        }

        public static MeshLOD ToMesh(this ObjectPart.PrimitiveShape shape, AssetServiceInterface assetService)
        {
            return shape.DecodedParams.ToMesh(assetService);
        }

        public static MeshLOD ToMesh(this ObjectPart.PrimitiveShape.Decoded shape, AssetServiceInterface assetService)
        {
            MeshLOD mesh;
            switch(shape.ShapeType)
            {
                case PrimitiveShapeType.Sculpt:
                    switch(shape.SculptType & PrimitiveSculptType.TypeMask)
                    {
                        case PrimitiveSculptType.Mesh:
                            LLMesh llMesh = new LLMesh(assetService[shape.SculptMap]);
                            mesh = llMesh.GetLOD(LLMesh.LodLevel.LOD3);
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

            return mesh;
        }
    }
}
