﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.StructuredData.Llsd;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types.Asset.Format.Mesh
{
    public class PhysicsMesh : MeshLOD
    {
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
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
