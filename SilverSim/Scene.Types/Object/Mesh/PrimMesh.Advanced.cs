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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scene.Types.Object.Mesh
{
    public static partial class PrimMesh
    {
        static List<Vector3> ExtrudeAdvanced(this PathDetails path, ObjectPart.PrimitiveShape.Decoded shape, double twistBegin, double twistEnd, double cut)
        {
            List<Vector3> extrusionPath = new List<Vector3>();
            double twist = twistBegin.Lerp(twistEnd, cut);
            Vector3 topSize = new Vector3();
            Vector3 shear = new Vector3();

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

            #region top_shear
            if (shape.TopShear.X < 0f)
            {
                shear.X = 1.0.Clamp(1f + shape.TopShear.X, 1f - cut);
            }
            else
            {
                shear.X = 1.0.Clamp(1f - shape.TopShear.X, cut);
            }
            if (shape.TopShear.Y < 0f)
            {
                shear.Y = 1.0.Clamp(1f + shape.TopShear.Y, 1f - cut);
            }
            else
            {
                shear.Y = 1.0.Clamp(1f - shape.TopShear.Y, 1f - cut);
            }
            #endregion

            /* generate extrusions */
            foreach (Vector3 vertex in path.Vertices)
            {
                Vector3 outvertex = vertex;
                outvertex.X *= topSize.X;
                outvertex.Y *= topSize.Y;
                outvertex += shape.TopShear;
                outvertex = outvertex.Rotate2D(twist);
                outvertex.Z = cut - 0.5;
                /* TODO: implement vertex displacement accordingly */
                outvertex += Vector3.UnitX.Rotate2D(twist) * 0.5;
                extrusionPath.Add(outvertex);
            }
            return extrusionPath;
        }

        #region 2D Path calculation
        static PathDetails CalcTorusPath(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            PathDetails path = shape.CalcCylinderPath();
            int i;
            for(i = 0; i < path.Vertices.Count; ++i)
            {
                /* rotate vertices */
                Vector3 v = path.Vertices[i];
                path.Vertices[i] = new Vector3(0, v.Y, v.X);
            }

            return path;
        }

        static PathDetails CalcRingPath(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            PathDetails path = shape.CalcPrismPath();
            int i;
            for (i = 0; i < path.Vertices.Count; ++i)
            {
                /* rotate vertices */
                Vector3 v = path.Vertices[i];
                path.Vertices[i] = new Vector3(0, v.Y, v.X);
            }

            return path;
        }

        static PathDetails CalcPipePath(this ObjectPart.PrimitiveShape.Decoded shape)
        {
            PathDetails path = shape.CalcBoxPath();
            int i;
            for (i = 0; i < path.Vertices.Count; ++i)
            {
                /* rotate vertices */
                Vector3 v = path.Vertices[i];
                path.Vertices[i] = new Vector3(0, v.Y, v.X);
            }

            return path;
        }
        #endregion
    }
}
