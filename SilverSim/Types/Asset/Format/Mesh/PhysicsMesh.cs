// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
            else if (meshmap.ContainsKey("medium_lod"))
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
