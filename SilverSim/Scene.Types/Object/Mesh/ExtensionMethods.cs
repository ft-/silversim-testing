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

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types.Asset.Format.Mesh;
using SilverSim.Types.Primitive;

namespace SilverSim.Scene.Types.Object.Mesh
{
    public static class ExtensionMethods
    {
        public static MeshLOD ToMesh(this ObjectPart part, AssetServiceInterface assetService) =>
            part.Shape.DecodedParams.ToMesh(assetService);

        public static MeshLOD ToMesh(this ObjectPart.PrimitiveShape shape, AssetServiceInterface assetService) =>
            shape.DecodedParams.ToMesh(assetService);

        public static MeshLOD ToMesh(this ObjectPart.PrimitiveShape.Decoded shape, AssetServiceInterface assetService)
        {
            MeshLOD mesh;
            switch(shape.ShapeType)
            {
                case PrimitiveShapeType.Sculpt:
                    switch(shape.SculptType & PrimitiveSculptType.TypeMask)
                    {
                        case PrimitiveSculptType.Mesh:
                            var llMesh = new LLMesh(assetService[shape.SculptMap]);
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
