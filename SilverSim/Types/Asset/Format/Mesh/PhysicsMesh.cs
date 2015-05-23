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

using SilverSim.StructuredData.LLSD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Types.Asset.Format.Mesh
{
    public class PhysicsMesh : LOD
    {
        public PhysicsMesh(AssetData asset)
        {
            if (asset.Type != AssetType.Mesh)
            {
                throw new ArgumentException();
            }
            Map meshmap;
            int start;
            try
            {
                meshmap = (Map)LLSD_Binary.Deserialize(asset.InputStream);
                start = (int)asset.InputStream.Position;
            }
            catch
            {
                throw new NotAMeshFormat();
            }

            Map physicsMap;
            if (meshmap.ContainsKey("physics_shape"))
            {
                physicsMap = (Map)meshmap["physics_shape"];
            }
            else if(meshmap.ContainsKey("physics_mesh"))
            {
                physicsMap = (Map)meshmap["physics_mesh"];
            }
            else if (meshmap.ContainsKey("medium_lod"))
            {
                physicsMap = (Map)meshmap["high_lod"];
            }
            else
            {
                throw new NotAMeshFormat();
            }

            int physOffset = physicsMap["offset"].AsInt + start;
            int physSize = physicsMap["size"].AsInt;

            if(physOffset < start || physSize <= 0)
            {
                throw new NotAMeshFormat();
            }

            Load(asset.Data, physOffset, physSize);
        }
    }
}
