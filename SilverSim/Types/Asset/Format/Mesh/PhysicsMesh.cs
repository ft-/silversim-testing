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

using SilverSim.Types.StructuredData.Llsd;

namespace SilverSim.Types.Asset.Format.Mesh
{
    public class PhysicsMesh : MeshLOD
    {
        public PhysicsMesh(AssetData asset)
        {
            if (asset.Type != AssetType.Mesh)
            {
                throw new NotAMeshFormatException();
            }
            Map meshmap;
            int start;
            try
            {
                meshmap = (Map)LlsdBinary.Deserialize(asset.InputStream);
                start = (int)asset.InputStream.Position;
            }
            catch
            {
                throw new NotAMeshFormatException();
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
            /* prefer lowest LOD */
            else if (meshmap.ContainsKey("lowest_lod"))
            {
                physicsMap = (Map)meshmap["lowest_lod"];
            }
            else if (meshmap.ContainsKey("low_lod"))
            {
                physicsMap = (Map)meshmap["low_lod"];
            }
            else if (meshmap.ContainsKey("medium_lod"))
            {
                physicsMap = (Map)meshmap["medium_lod"];
            }
            else if (meshmap.ContainsKey("high_lod"))
            {
                physicsMap = (Map)meshmap["high_lod"];
            }
            else
            {
                throw new NotAMeshFormatException();
            }

            int physOffset = physicsMap["offset"].AsInt + start;
            int physSize = physicsMap["size"].AsInt;

            if(physOffset < start || physSize <= 0)
            {
                throw new NotAMeshFormatException();
            }

            Load(asset.Data, physOffset, physSize);
        }
    }
}
