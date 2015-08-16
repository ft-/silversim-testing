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
        CollisionObject m_TerrainObject;
        int m_TerrainVertexCount;
        int m_TerrainLineWidth;
        bool m_TerrainFilled = false;

        void InitializeTerrainMesh()
        {
            m_Log.Info("Initializing terrain");
            m_TerrainLineWidth = ((int)m_Scene.RegionData.Size.X + 1);
            int triangleidxcount = (int)((m_Scene.RegionData.Size.X) * (m_Scene.RegionData.Size.Y) * 2 * 3);
            m_TerrainVertexCount = (int)((m_Scene.RegionData.Size.X + 1) * (m_Scene.RegionData.Size.Y + 1));
            triangleidxcount += (int)m_Scene.RegionData.Size.X * 2 * 3 + (int)m_Scene.RegionData.Size.Y * 2 * 3;
            triangleidxcount *= 2;
            m_TerrainIndexedMesh.Allocate(m_TerrainVertexCount * 2, 1, triangleidxcount, 4);

            #region Build Triangles
            /* build triangles */
            int tridx = 0;
            for (int y = 0; y < m_Scene.RegionData.Size.Y; ++y)
            {
                for (int x = 0; x < m_Scene.RegionData.Size.X; ++x)
                {
                    int vidx = y * m_TerrainLineWidth + x;

                    try
                    {
                        /* Face 0, 0+lineWidth, 1 */
                        m_TerrainIndexedMesh.TriangleIndices[tridx++] = vidx;
                        m_TerrainIndexedMesh.TriangleIndices[tridx++] = vidx + m_TerrainLineWidth;
                        m_TerrainIndexedMesh.TriangleIndices[tridx++] = vidx + 1;

                        /* Face 0+lineWidth, 1+lineWidth, 1 */
                        m_TerrainIndexedMesh.TriangleIndices[tridx++] = vidx + m_TerrainLineWidth;
                        m_TerrainIndexedMesh.TriangleIndices[tridx++] = vidx + m_TerrainLineWidth + 1;
                        m_TerrainIndexedMesh.TriangleIndices[tridx++] = vidx + 1;

                        /* Lower Face 0, 0+lineWidth, 1 */
                        m_TerrainIndexedMesh.TriangleIndices[tridx++] = m_TerrainVertexCount + vidx;
                        m_TerrainIndexedMesh.TriangleIndices[tridx++] = m_TerrainVertexCount + vidx + m_TerrainLineWidth;
                        m_TerrainIndexedMesh.TriangleIndices[tridx++] = m_TerrainVertexCount + vidx + 1;

                        /* Lower Face 0+lineWidth, 1+lineWidth, 1 */
                        m_TerrainIndexedMesh.TriangleIndices[tridx++] = m_TerrainVertexCount + vidx + m_TerrainLineWidth;
                        m_TerrainIndexedMesh.TriangleIndices[tridx++] = m_TerrainVertexCount + vidx + m_TerrainLineWidth + 1;
                        m_TerrainIndexedMesh.TriangleIndices[tridx++] = m_TerrainVertexCount + vidx + 1;

                    }
                    catch(Exception e)
                    {
                        m_Log.FatalFormat("Failed to initialize Terrain Mesh {0}", e.Message);
                        throw;
                    }
                }
            }

            /* Extrusion */
            for (int y = 0; y < m_Scene.RegionData.Size.Y; ++y)
            {
                try
                {
                    int vidx = y * m_TerrainLineWidth;
                    /* Side face y left */
                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = vidx;
                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = vidx + m_TerrainLineWidth;
                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = m_TerrainVertexCount + vidx;

                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = vidx + m_TerrainLineWidth;
                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = m_TerrainVertexCount + vidx + m_TerrainLineWidth;
                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = m_TerrainVertexCount + vidx;

                    /* Side face y right */
                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = vidx + m_TerrainLineWidth - 1;
                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = vidx + 2 * m_TerrainLineWidth - 1;
                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = m_TerrainVertexCount + vidx + m_TerrainLineWidth - 1;

                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = vidx + m_TerrainLineWidth - 1;
                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = m_TerrainVertexCount + vidx + 2 * m_TerrainLineWidth - 1;
                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = m_TerrainVertexCount + vidx + m_TerrainLineWidth - 1;

                }
                catch (Exception e)
                {
                    m_Log.FatalFormat("Failed to initialize Terrain Mesh {0}", e.Message);
                    throw;
                }
            }

            int lowBorderStart = m_TerrainVertexCount - m_TerrainLineWidth;
            for (int x = 0; x < m_Scene.RegionData.Size.X; ++x)
            {
                try
                {
                    /* Side face x top */
                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = x;
                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = x + 1;
                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = m_TerrainVertexCount + x;

                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = m_TerrainVertexCount + x + 1;
                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = x + 1;
                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = m_TerrainVertexCount + x;

                    /* Side face x bottom */
                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = lowBorderStart + x;
                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = lowBorderStart + x + 1;
                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = lowBorderStart + m_TerrainVertexCount + x;

                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = lowBorderStart + m_TerrainVertexCount + x + 1;
                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = lowBorderStart + x + 1;
                    m_TerrainIndexedMesh.TriangleIndices[tridx++] = lowBorderStart + m_TerrainVertexCount + x;
                }
                catch (Exception e)
                {
                    m_Log.FatalFormat("Failed to initialize Terrain Mesh {0}", e.Message);
                    throw;
                }
            }

            if (tridx != m_TerrainIndexedMesh.TriangleIndices.Count)
            {
                throw new InvalidOperationException("Terrain not initialized correctly");
            }
            #endregion

            m_TerrainMeshIndexArray = new TriangleIndexVertexArray();
            m_TerrainMeshIndexArray.AddIndexedMesh(m_TerrainIndexedMesh);
            m_TerrainMeshShape = new BvhTriangleMeshShape(m_TerrainMeshIndexArray, false, true);
            m_TerrainObject = new CollisionObject();
            m_TerrainObject.CollisionShape = m_TerrainMeshShape;
            m_Log.Info("Initializing terrain done");
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

            #region Fill Terrain Data
            m_Log.Info("Fill terrain");

            List<LayerPatch> initialTerrain = m_Scene.Terrain.AllPatches;
            foreach (LayerPatch lp in initialTerrain)
            {
                for (int y = 0; y < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++y)
                {
                    for (int x = 0; x < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++x)
                    {
                        int vx = x + (int)lp.X * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;
                        int vy = y + (int)lp.Y * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;
                        int vidx = vy * m_TerrainLineWidth + vx;
                        m_TerrainIndexedMesh.Vertices[vidx] = new Vector3(vx, vy, lp[x, y]);
                        m_TerrainIndexedMesh.Vertices[m_TerrainVertexCount + vidx] = new Vector3(vx, vy, lp[x, y] - 1);
                        if (vy == m_Scene.RegionData.Size.Y - 1)
                        {
                            m_TerrainIndexedMesh.Vertices[vidx + m_TerrainLineWidth] = new Vector3(vx, vy + 1, lp[x, y]);
                            m_TerrainIndexedMesh.Vertices[m_TerrainVertexCount + vidx + m_TerrainLineWidth] = new Vector3(vx, vy + 1, lp[x, y] - 1);
                        }
                        if (vx == m_Scene.RegionData.Size.X - 1)
                        {
                            m_TerrainIndexedMesh.Vertices[vidx + 1] = new Vector3(vx + 1, vy, lp[x, y]);
                            m_TerrainIndexedMesh.Vertices[m_TerrainVertexCount + vidx + 1] = new Vector3(vx + 1, vy, lp[x, y] - 1);
                        }
                        if (vx == m_Scene.RegionData.Size.X - 1 && vy == m_Scene.RegionData.Size.Y - 1)
                        {
                            m_TerrainIndexedMesh.Vertices[vidx + m_TerrainLineWidth + 1] = new Vector3(vx + 1, vy + 1, lp[x, y]);
                            m_TerrainIndexedMesh.Vertices[m_TerrainVertexCount + vidx + m_TerrainLineWidth + 1] = new Vector3(vx + 1, vy + 1, lp[x, y] - 1);
                        }
                    }
                }
            }
            m_DynamicsWorld.AddCollisionObject(m_TerrainObject);
            m_TerrainFilled = true;
            m_Log.Info("Fill terrain completed");
            m_Scene.LoginControl.Ready(Types.Scene.SceneInterface.ReadyFlags.PhysicsTerrain);
            EnablePhysicsInternally();
            #endregion

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
                        m_TerrainIndexedMesh.Vertices[m_TerrainVertexCount + vidx] = new Vector3(vx, vy, lp[x, y] - 0.5);
                        if (vy == m_Scene.RegionData.Size.Y - 1)
                        {
                            m_TerrainIndexedMesh.Vertices[vidx + lineWidth] = new Vector3(vx, vy + 1, lp[x, y]);
                            m_TerrainIndexedMesh.Vertices[m_TerrainVertexCount + vidx + lineWidth] = new Vector3(vx, vy + 1, lp[x, y] - 0.5);
                        }
                        if (vx == m_Scene.RegionData.Size.X - 1)
                        {
                            m_TerrainIndexedMesh.Vertices[vidx + 1] = new Vector3(vx + 1, vy, lp[x, y]);
                            m_TerrainIndexedMesh.Vertices[m_TerrainVertexCount + vidx + 1] = new Vector3(vx + 1, vy, lp[x, y] - 0.5);
                        }
                        if (vx == m_Scene.RegionData.Size.X - 1 && vy == m_Scene.RegionData.Size.Y - 1)
                        {
                            m_TerrainIndexedMesh.Vertices[vidx + lineWidth + 1] = new Vector3(vx + 1, vy + 1, lp[x, y]);
                            m_TerrainIndexedMesh.Vertices[m_TerrainVertexCount + vidx + lineWidth + 1] = new Vector3(vx + 1, vy + 1, lp[x, y] - 0.5);
                        }
                    }
                }
            }
        }

    }
}
