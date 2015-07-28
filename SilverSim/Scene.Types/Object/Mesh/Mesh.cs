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
    public sealed class Mesh
    {
        public struct Triangle
        {
            public int PrimFaceIndex;

            public int VectorIndex0;
            public int VectorIndex1;
            public int VectorIndex2;

            public int NormalIndex0;
            public int NormalIndex1;
            public int NormalIndex2;

            public int UVIndex0;
            public int UVIndex1;
            public int UVIndex2;
        }

        public List<Vector3> Vertices = new List<Vector3>();
        public List<Vector3> Normals = new List<Vector3>();
        public List<Triangle> Triangles = new List<Triangle>();

        public Mesh()
        {

        }
    }
}
