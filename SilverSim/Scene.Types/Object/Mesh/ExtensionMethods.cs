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
            ObjectPart.PrimitiveShape.Decoded shape = part.Shape.DecodedParams;

            return part.Shape.DecodedParams.ToMesh(assetService);
        }

        public static Mesh ToMesh(this ObjectPart.PrimitiveShape shape, AssetServiceInterface assetService)
        {
            return shape.DecodedParams.ToMesh(assetService);
        }

        public static Mesh ToMesh(this ObjectPart.PrimitiveShape.Decoded shape, AssetServiceInterface assetService)
        {
            Mesh mesh;
            switch(shape.ShapeType)
            {
                case PrimitiveShapeType.Sculpt:
                    switch(shape.SculptType & PrimitiveSculptType.TypeMask)
                    {
                        case PrimitiveSculptType.Mesh:
                            mesh = assetService[shape.SculptMap].LLMeshToMesh(shape);
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
