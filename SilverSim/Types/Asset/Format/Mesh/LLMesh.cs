﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.StructuredData.Llsd;
using System;
using System.IO;

namespace SilverSim.Types.Asset.Format.Mesh
{
    public class LLMesh
    {
        readonly Map m_MeshData;
        readonly int m_EndOfHeader;
        byte[] m_AssetData;

        public LLMesh(AssetData asset)
        {
            m_AssetData = asset.Data;
            if (asset.Type != AssetType.Mesh)
            {
                throw new NotAMeshFormatException();
            }
            try
            {
                using (Stream s = asset.InputStream)
                {
                    m_MeshData = (Map)LlsdBinary.Deserialize(s);
                    m_EndOfHeader = (int)s.Position;
                }
            }
            catch
            {
                throw new NotAMeshFormatException();
            }
        }

        public enum LodLevel
        {
            LOD0,
            LOD1,
            LOD2,
            LOD3,
            Physics
        }

        public bool HasConvexPhysics()
        {
            return m_MeshData.ContainsKey("physics_convex");
        }

        public PhysicsConvexShape GetConvexPhysics()
        {
            Map lodMap;
            IValue iv;
            if (!m_MeshData.TryGetValue("physics_convex", out iv))
            {
                throw new NoSuchMeshDataException();
            }

            lodMap = iv as Map;
            if (null == lodMap)
            {
                throw new NoSuchMeshDataException();
            }

            int physOffset = lodMap["offset"].AsInt + m_EndOfHeader;
            int physSize = lodMap["size"].AsInt;

            if (physOffset < m_EndOfHeader || physSize <= 0)
            {
                throw new NotAMeshFormatException();
            }

            return new PhysicsConvexShape(m_AssetData, physOffset, physSize);
        }

        public bool HasLOD(LodLevel level)
        {
            switch(level)
            {
                case LodLevel.Physics:
                    return m_MeshData.ContainsKey("physics_mesh");

                case LodLevel.LOD0:
                    return m_MeshData.ContainsKey("lowest_lod");

                case LodLevel.LOD1:
                    return m_MeshData.ContainsKey("low_lod");

                case LodLevel.LOD2:
                    return m_MeshData.ContainsKey("medium_lod");

                case LodLevel.LOD3:
                    return m_MeshData.ContainsKey("high_lod");

                default:
                    throw new ArgumentOutOfRangeException("level");
            }
        }

        public MeshLOD GetLOD(LodLevel level)
        {
            string lodName;
            switch(level)
            {
                case LodLevel.Physics:
                    lodName = "physics_mesh";
                    break;

                case LodLevel.LOD0:
                    lodName = "lowest_lod";
                    break;

                case LodLevel.LOD1:
                    lodName = "low_lod";
                    break;

                case LodLevel.LOD2:
                    lodName = "medium_lod";
                    break;

                case LodLevel.LOD3:
                    lodName = "high_lod";
                    break;

                default:
                    throw new ArgumentOutOfRangeException("level");
            }

            Map lodMap;
            IValue iv;
            if(!m_MeshData.TryGetValue(lodName, out iv))
            {
                throw new NoSuchMeshDataException();
            }

            lodMap = iv as Map;
            if(null == lodMap)
            {
                throw new NoSuchMeshDataException();
            }

            int physOffset = lodMap["offset"].AsInt + m_EndOfHeader;
            int physSize = lodMap["size"].AsInt;

            if (physOffset < m_EndOfHeader || physSize <= 0)
            {
                throw new NotAMeshFormatException();
            }

            return new MeshLOD(m_AssetData, physOffset, physSize);
        }
    }
}
