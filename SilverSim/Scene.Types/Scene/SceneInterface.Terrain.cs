﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Scene.Types.Agent;
using SilverSim.Viewer.Messages.Generic;
using SilverSim.Viewer.Messages.LayerData;
using SilverSim.Viewer.Messages.Region;
using SilverSim.Viewer.Messages;
using System.Threading;
using ThreadedClasses;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        public TerrainController Terrain;

        public class TerrainController
        {
            private const int BASE_REGION_SIZE = 256;
            private const double DEFAULT_TERRAIN_HEIGHT = 21;

            readonly SceneInterface m_Scene;
            readonly ReaderWriterLock m_TerrainRwLock = new ReaderWriterLock();

            readonly LayerPatch[,] m_TerrainPatches;

            public readonly RwLockedList<ITerrainListener> TerrainListeners = new RwLockedList<ITerrainListener>();

            public TerrainController(SceneInterface scene)
            {
                uint x;
                uint y;

                uint xPatches = scene.RegionData.Size.X / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;
                uint yPatches = scene.RegionData.Size.Y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;

                m_Scene = scene;
                m_TerrainPatches = new LayerPatch[yPatches, xPatches];

                for (y = 0; y < yPatches; ++y)
                {
                    for (x = 0; x < xPatches; ++x)
                    {
                        m_TerrainPatches[y, x] = new LayerPatch(22);
                        m_TerrainPatches[y, x].X = x;
                        m_TerrainPatches[y, x].Y = y;
                    }
                }
                Patch = new PatchesAccessor(m_TerrainPatches, xPatches, yPatches);
            }

            #region Update of Terrain Data
            /*
            public IList<TerrainPatch> GetTerrainDistanceSorted(Vector3 v)
            {
                SortedList<int, TerrainPatch> sorted = new SortedList<int, TerrainPatch>();
                uint x;
                uint y;

                if(v.X < 0)
                {
                    x = 0;
                }
                else if(v.X >= SizeX)
                {
                    x = SizeX - 1;
                }
                else
                {
                    x = (uint)v.X / TERRAIN_PATCH_SIZE;
                }

                if (v.Y < 0)
                {
                    y = 0;
                }
                else if(v.Y >= SizeY)
                {
                    y = SizeY - 1;
                }
                else
                {
                    y = (uint)v.Y / TERRAIN_PATCH_SIZE;
                }

                int distance;

                for(uint py = 0; py < SizeY / TERRAIN_PATCH_SIZE; ++py)
                {
                    for(uint px = 0; px < SizeX / TERRAIN_PATCH_SIZE; ++px)
                    {
                        distance = ((int)px - (int)x) * ((int)px - (int)x) + ((int)py - (int)y) * ((int)py - (int)y);
                        sorted.Add(distance, new TerrainPatch(px, py, m_Map[py * m_PatchCountX + px]));
                    }
                }

                return sorted.Values;
            }
 */
            private List<LayerData> CompileTerrainData(IAgent agent, bool force)
            {
                m_TerrainRwLock.AcquireReaderLock(-1);
                try
                {
                    int y;
                    int x;
                    List<LayerData> mlist = new List<LayerData>();
                    List<LayerPatch> dirtyPatches = new List<LayerPatch>();
                    RwLockedDictionary<uint, uint> agentSceneSerials = agent.TransmittedTerrainSerials[m_Scene.ID];
                    
                    for (y = 0; y < m_Scene.RegionData.Size.Y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++y)
                    {
                        for (x = 0; x < m_Scene.RegionData.Size.X / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++x)
                        {
                            LayerPatch patch = m_TerrainPatches[y, x];
                            uint serial;
                            if(agentSceneSerials.TryGetValue(patch.ExtendedPatchID, out serial))
                            {
                                if (serial != patch.Serial)
                                {
                                    agentSceneSerials[patch.ExtendedPatchID] = serial;
                                    dirtyPatches.Add(m_TerrainPatches[y, x]);
                                }
                            }
                            else
                            {
                                dirtyPatches.Add(m_TerrainPatches[y, x]);
                            }
                        }
                    }
                    LayerData.LayerDataType layerType = LayerData.LayerDataType.Land;

                    if (BASE_REGION_SIZE < m_Scene.RegionData.Size.X || BASE_REGION_SIZE < m_Scene.RegionData.Size.Y)
                    {
                        layerType = LayerData.LayerDataType.LandExtended;
                    }
                    int offset = 0;
                    while (offset < dirtyPatches.Count)
                    {
                        int remaining = dirtyPatches.Count - offset;
                        int actualused = 0;
                        mlist.Add(LayerCompressor.ToLayerMessage(dirtyPatches, layerType, offset, remaining, out actualused));
                        offset += actualused;
                    }
                    return mlist;
                }
                finally
                {
                    m_TerrainRwLock.ReleaseReaderLock();
                }
            }

            public void UpdateTerrainDataToSingleClient(IAgent agent, bool forceUpdate)
            {
                List<LayerData> mlist = CompileTerrainData(agent, forceUpdate);
                foreach (LayerData m in mlist)
                {
                    agent.SendMessageAlways(m, m_Scene.ID);
                }
            }

            public void UpdateTerrainDataToClients()
            {
                foreach (IAgent agent in m_Scene.Agents)
                {
                    List<LayerData> mlist = CompileTerrainData(agent, false);
                    foreach (LayerData m in mlist)
                    {
                        agent.SendMessageAlways(m, m_Scene.ID);
                    }
                }
            }
            public void UpdateTerrainListeners(LayerPatch layerpatch)
            {
                layerpatch = new LayerPatch(layerpatch);
                foreach (ITerrainListener listener in TerrainListeners)
                {
                    listener.TerrainUpdate(layerpatch);
                }
            }
            #endregion

            public Vector3 Normal(int posX, int posY)
            {
                // Clamp to valid position
                posX = posX.Clamp(0, (int)m_Scene.RegionData.Size.X);
                posY = posY.Clamp(0, (int)m_Scene.RegionData.Size.Y);

                /* Find neighboring points so we can calculate the resulting plane */
                Vector3 p0 = new Vector3(posX, posY, this[(uint)posX, (uint)posY]);
                Vector3 p1 = new Vector3(posX + 1, posY, 0);
                Vector3 p2 = new Vector3(posX, posY + 1, 0);

                p1.Z = this[(posX + 1) >= m_Scene.RegionData.Size.X ? 
                            (uint)posX :
                            (uint)posX + 1, 
                            (uint)posY];

                p2.Z = this[(uint)posX,
                            (posY + 1.0) >= m_Scene.RegionData.Size.Y ?
                            (uint)posY :
                            (uint)posY + 1];

                /* Calculate normalized vectors from p0 to p1 and p0 to p2 */
                Vector3 v0 = p1 - p0;
                Vector3 v1 = p2 - p0;

                /* Calculate the cross product (the slope normal). */
                return v0.Cross(v1).Normalize();
            }

            #region Properties
            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            public double this[uint x, uint y]
            {
                get
                {
                    if (x >= m_Scene.RegionData.Size.X || y >= m_Scene.RegionData.Size.Y)
                    {
                        throw new KeyNotFoundException();
                    }
                    return m_TerrainPatches[x / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES].Data[y % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, x % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES];
                }
                set
                {
                    LayerPatch lp = null;
                    if (x >= m_Scene.RegionData.Size.X || y >= m_Scene.RegionData.Size.Y)
                    {
                        throw new KeyNotFoundException();
                    }

                    m_TerrainRwLock.AcquireWriterLock(-1);
                    try
                    {
                        lp = m_TerrainPatches[x / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES]; 
                        lp.Data[y % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, x % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES] = (float)value;
                    }
#if DEBUG
                    catch(Exception e)
                    {
                        m_Log.Debug(string.Format("Terrain Change at {0},{1} failed", x, y), e);
                        throw;
                    }
#endif
                    finally
                    {
                        m_TerrainRwLock.ReleaseWriterLock();
                    }
                    UpdateTerrainDataToClients();
                    if(lp != null)
                    {
                        foreach (ITerrainListener listener in TerrainListeners)
                        {
                            listener.TerrainUpdate(new LayerPatch(lp));
                        }
                    }
                }
            }

            public LayerPatch AdjustTerrain(uint x, uint y, double change)
            {
                if (x >= m_Scene.RegionData.Size.X || y >= m_Scene.RegionData.Size.Y)
                {
                    throw new KeyNotFoundException();
                }

                m_TerrainRwLock.AcquireWriterLock(-1);
                try
                {
                    LayerPatch lp = m_TerrainPatches[x / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES];
                    lp.Data[y % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, x % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES] += (float)change;
                    return lp;
                }
#if DEBUG
                catch (Exception e)
                {
                    m_Log.Debug(string.Format("Terrain Change at {0},{1} failed", x, y), e);
                    throw;
                }
#endif
                finally
                {
                    m_TerrainRwLock.ReleaseWriterLock();
                }
            }

            public LayerPatch BlendTerrain(uint x, uint y, double newval, double mix /* 0. orig only , 1. new only */)
            {
                if (x >= m_Scene.RegionData.Size.X || y >= m_Scene.RegionData.Size.Y)
                {
                    throw new KeyNotFoundException();
                }

                m_TerrainRwLock.AcquireWriterLock(-1);
                try
                {
                    LayerPatch lp = m_TerrainPatches[x / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES];
                    lp.Data[y % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, x % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES] =
                        (float)(lp.Data[y % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, x % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES] * (1 - mix)) +
                        (float)(newval * mix);
                    return lp;
                }
#if DEBUG
                catch (Exception e)
                {
                    m_Log.Debug(string.Format("Terrain Change at {0},{1} failed", x, y), e);
                    throw;
                }
#endif
                finally
                {
                    m_TerrainRwLock.ReleaseWriterLock();
                }
            }

            public double this[Vector3 pos]
            {
                get
                {
                    int x = (int)pos.X.Clamp(0, m_Scene.RegionData.Size.X - 1);
                    int y = (int)pos.Y.Clamp(0, m_Scene.RegionData.Size.Y - 1);
                    return this[(uint)x, (uint)y];
                }
                set
                {
                    if (pos.X < 0 || pos.Y < 0)
                    {
                        throw new KeyNotFoundException();
                    }

                    uint x = (uint)pos.X;
                    uint y = (uint)pos.Y;
                    this[x, y] = (float)value;
                }
            }
            #endregion

            #region Access Terrain Data
            public readonly PatchesAccessor Patch;

            public List<LayerPatch> AllPatches
            {
                get
                {
                    int xPatches = (int)m_Scene.RegionData.Size.X / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;
                    int yPatches = (int)m_Scene.RegionData.Size.Y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;

                    List<LayerPatch> patches = new List<LayerPatch>();
                    for (int y = 0; y < yPatches; ++y)
                    {
                        for (int x = 0; x < xPatches; ++x)
                        {
                            patches.Add(new LayerPatch(m_TerrainPatches[y, x]));
                        }
                    }
                    return patches;
                }

                set
                {
                    foreach(LayerPatch p in value)
                    {
                        Patch.Update(p);
                        foreach (ITerrainListener listener in TerrainListeners)
                        {
                            listener.TerrainUpdate(new LayerPatch(p));
                        }
                    }
                    UpdateTerrainDataToClients();
                }
            }

            public class PatchesAccessor
            {
                readonly LayerPatch[,] m_TerrainPatches;
                readonly uint m_NumXPatches;
                readonly uint m_NumYPatches;
                public PatchesAccessor(LayerPatch[,] terrainPatches, uint xPatches, uint yPatches)
                {
                    m_TerrainPatches = terrainPatches;
                    m_NumXPatches = xPatches;
                    m_NumYPatches = yPatches;
                }

                [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
                public LayerPatch this[uint x, uint y]
                {
                    get
                    {
                        if(x >= m_NumXPatches || y >= m_NumYPatches)
                        {
                            throw new KeyNotFoundException();
                        }
                        return new LayerPatch(m_TerrainPatches[y, x]);
                    }
                }

                public void Update(LayerPatch p)
                {
                    if (p.X >= m_NumXPatches || p.Y >= m_NumYPatches)
                    {
                        throw new KeyNotFoundException();
                    }
                    m_TerrainPatches[p.Y, p.X].Update(p);
                }

                public void UpdateWithSerial(LayerPatch p)
                {
                    if (p.X >= m_NumXPatches || p.Y >= m_NumYPatches)
                    {
                        throw new KeyNotFoundException();
                    }
                    m_TerrainPatches[p.Y, p.X].UpdateWithSerial(p);
                }
            }
            #endregion
        }
    }
}
