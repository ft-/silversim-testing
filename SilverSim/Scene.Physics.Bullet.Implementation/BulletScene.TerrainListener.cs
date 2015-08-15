// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using BulletSharp;
using SilverSim.LL.Messages.LayerData;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.Scene.Physics.Bullet.Implementation
{
    public partial class BulletScene
    {
        readonly Dictionary<uint, uint> m_LastKnownTerrainSerials = new Dictionary<uint, uint>();
        readonly BlockingQueue<LayerPatch> m_TerrainUpdateQueue = new BlockingQueue<LayerPatch>();
        readonly IndexedMesh m_TerrainIndexedMesh = new IndexedMesh();
        BvhTriangleMeshShape m_TerrainMeshShape;
        TriangleIndexVertexArray m_TerrainMeshIndexArray;

        void InitializeTerrainMesh()
        {
            for (int y = 0; y < m_Scene.RegionData.Size.Y; ++y)
            {
                for (int x = 0; x < m_Scene.RegionData.Size.X; ++x)
                {
                    Vector3 v = new Vector3(x, y, 21);

                    m_TerrainIndexedMesh.Vertices.Add(v);
                }
            }

            List<LayerPatch> initialTerrain = m_Scene.Terrain.AllPatches;
            foreach(LayerPatch lp in initialTerrain)
            {
                for(int y = 0; y < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++y)
                {
                    for(int x = 0; x < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++x)
                    {
                        int vx = x + (int)lp.X * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;
                        int vy = y + (int)lp.Y * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;
                        int lineWidth = ((int)m_Scene.RegionData.Size.X + 1);
                        int vidx = vy * lineWidth + vx;
                        m_TerrainIndexedMesh.Vertices[vidx] = new Vector3(vx, vy, lp[x, y]);
                        if(vy == m_Scene.RegionData.Size.Y - 1)
                        {
                            m_TerrainIndexedMesh.Vertices[vidx + lineWidth] = new Vector3(vx, vy + 1, lp[x, y]);
                        }
                        if(vx == m_Scene.RegionData.Size.X - 1)
                        {
                            m_TerrainIndexedMesh.Vertices[vidx + 1] = new Vector3(vx + 1, vy, lp[x, y]);
                        }
                        if (vx == m_Scene.RegionData.Size.X - 1 && vy == m_Scene.RegionData.Size.Y - 1)
                        {
                            m_TerrainIndexedMesh.Vertices[vidx + lineWidth + 1] = new Vector3(vx + 1, vy + 1, lp[x, y]);
                        }
                    }
                }
            }

            /* build triangles */
            for (int y = 0; y < m_Scene.RegionData.Size.Y; ++y)
            {
                for (int x = 0; x < m_Scene.RegionData.Size.X; ++x)
                {
                    int lineWidth = ((int)m_Scene.RegionData.Size.X + 1);
                    int vidx = y * lineWidth + x;

                    /* Face 0, 0+lineWidth, 1 */
                    m_TerrainIndexedMesh.TriangleIndices.Add(vidx);
                    m_TerrainIndexedMesh.TriangleIndices.Add(vidx + lineWidth);
                    m_TerrainIndexedMesh.TriangleIndices.Add(vidx + 1);

                    /* Face 0+lineWidth, 1+lineWidth, 1 */
                    m_TerrainIndexedMesh.TriangleIndices.Add(vidx + lineWidth);
                    m_TerrainIndexedMesh.TriangleIndices.Add(vidx + lineWidth + 1);
                    m_TerrainIndexedMesh.TriangleIndices.Add(vidx + 1);
                }
            }

            m_TerrainMeshIndexArray = new TriangleIndexVertexArray();
            m_TerrainMeshIndexArray.AddIndexedMesh(m_TerrainIndexedMesh);
            m_TerrainMeshShape = new BvhTriangleMeshShape(m_TerrainMeshIndexArray, false, true);
        }

        public void TerrainUpdate(LayerPatch layerpatch)
        {
            uint serialno;
            lock (m_LastKnownTerrainSerials)
            {
                if (m_LastKnownTerrainSerials.TryGetValue(layerpatch.ExtendedPatchID, out serialno))
                {
                    int diff = (int)(layerpatch.Serial - serialno);
                    if (diff > 0)
                    {
                        m_LastKnownTerrainSerials[layerpatch.ExtendedPatchID] = layerpatch.Serial;
                        m_TerrainUpdateQueue.Enqueue(layerpatch);
                    }
                }
            }
        }

        void BulletTerrainUpdateThread()
        {
            Thread.CurrentThread.Name = "Bullet:Terrain:Update (" + m_Scene.RegionData.ID + ") Thread";
            while (!m_StopBulletThreads)
            {
                LayerPatch lp;
                try
                {
                    lp = m_TerrainUpdateQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                for (int y = 0; y < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++y)
                {
                    for (int x = 0; x < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++x)
                    {
                        int vx = x + (int)lp.X * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;
                        int vy = y + (int)lp.Y * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;
                        int lineWidth = ((int)m_Scene.RegionData.Size.X + 1);
                        int vidx = vy * lineWidth + vx;
                        m_TerrainIndexedMesh.Vertices[vidx] = new Vector3(vx, vy, lp[x, y]);
                        if (vy == m_Scene.RegionData.Size.Y - 1)
                        {
                            m_TerrainIndexedMesh.Vertices[vidx + lineWidth] = new Vector3(vx, vy + 1, lp[x, y]);
                        }
                        if (vx == m_Scene.RegionData.Size.X - 1)
                        {
                            m_TerrainIndexedMesh.Vertices[vidx + 1] = new Vector3(vx + 1, vy, lp[x, y]);
                        }
                        if (vx == m_Scene.RegionData.Size.X - 1 && vy == m_Scene.RegionData.Size.Y - 1)
                        {
                            m_TerrainIndexedMesh.Vertices[vidx + lineWidth + 1] = new Vector3(vx + 1, vy + 1, lp[x, y]);
                        }
                    }
                }
            }
        }

    }
}
