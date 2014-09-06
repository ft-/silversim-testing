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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Scene.Types.Agent;
using SilverSim.LL.Messages.Generic;
using SilverSim.LL.Messages.LayerData;
using SilverSim.LL.Messages.Region;
using SilverSim.LL.Messages;
using System.Threading;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        public TerrainController Terrain;

        public class TerrainController : IDisposable
        {
            private const int BASE_REGION_SIZE = 256;

            private SceneInterface m_Scene;
            private ReaderWriterLock m_TerrainRwLock = new ReaderWriterLock();

            private LayerPatch[,] m_TerrainPatches;
            private bool[,] m_TerrainPatchesDirty;

            public TerrainController(SceneInterface scene)
            {
                int x;
                int y;

                int xPatches = (int)scene.RegionData.Size.X / LayerCompressor.LAYER_PATCH_SIM_WIDTH;
                int yPatches = (int)scene.RegionData.Size.Y / LayerCompressor.LAYER_PATCH_SIM_WIDTH;

                m_Scene = scene;
                m_TerrainPatches = new LayerPatch[yPatches, xPatches];
                m_TerrainPatchesDirty = new bool[yPatches, xPatches];

                for (y = 0; y < yPatches; ++y)
                {
                    for (x = 0; x < xPatches; ++x)
                    {
                        m_TerrainPatches[y, x] = new LayerPatch();
                        m_TerrainPatches[y,x].X = x * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;
                        m_TerrainPatches[y,x].Y = y * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;
                    }
                }
            }

            public void Dispose()
            {
                m_Scene = null;
            }

            #region Update of Terrain Data
            private List<LayerData> CompileTerrainData(bool force)
            {
                m_TerrainRwLock.AcquireReaderLock(-1);
                try
                {
                    int y;
                    int x;
                    List<LayerData> mlist = new List<LayerData>();
                    List<LayerPatch> dirtyPatches = new List<LayerPatch>();

                    for (y = 0; y < m_Scene.RegionData.Size.Y / LayerCompressor.LAYER_PATCH_SIM_WIDTH; ++y)
                    {
                        for (x = 0; x < m_Scene.RegionData.Size.X / LayerCompressor.LAYER_PATCH_SIM_WIDTH; ++x)
                        {
                            dirtyPatches.Add(new LayerPatch(m_TerrainPatches[y, x]));
                        }
                    }
                    LayerPatch[] patches = new LayerPatch[dirtyPatches.Count];
                    dirtyPatches.CopyTo(patches);

                    if (BASE_REGION_SIZE == m_Scene.RegionData.Size.X && BASE_REGION_SIZE == m_Scene.RegionData.Size.Y)
                    {
                        mlist.Add(LayerCompressor.ToLayerMessage(patches, LayerData.LayerDataType.Land));
                    }
                    else
                    {
                        int offset = 0;
                        while (offset < patches.Length)
                        {
                            if (patches.Length - offset > LayerCompressor.MAX_PATCHES_PER_MESSAGE)
                            {
                                mlist.Add(LayerCompressor.ToLayerMessage(patches, LayerData.LayerDataType.LandExtended, offset, LayerCompressor.MAX_PATCHES_PER_MESSAGE));
                                offset += LayerCompressor.MAX_PATCHES_PER_MESSAGE;
                            }
                            else
                            {
                                mlist.Add(LayerCompressor.ToLayerMessage(patches, LayerData.LayerDataType.LandExtended, offset, patches.Length - offset));
                                offset = patches.Length;
                            }
                        }
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
                List<LayerData> mlist = CompileTerrainData(forceUpdate);
                foreach (LayerData m in mlist)
                {
                    agent.SendMessageAlways(m, m_Scene.ID);
                }
            }

            private void UpdateTerrainDataToClients()
            {
                List<LayerData> mlist = CompileTerrainData(false);
                foreach (LayerData m in mlist)
                {
                    SendToAllClients(m);
                }
            }
            #endregion

            private void SendToAllClients(Message m)
            {
                foreach (IAgent agent in m_Scene.Agents)
                {
                    agent.SendMessageAlways(m, m_Scene.ID);
                }
            }

            #region Properties
            public double this[uint x, uint y]
            {
                get
                {
                    if (x >= m_Scene.RegionData.Size.X || y >= m_Scene.RegionData.Size.Y)
                    {
                        throw new KeyNotFoundException();
                    }
                    x /= LayerCompressor.LAYER_PATCH_ENTRY_WIDTH;
                    y /= LayerCompressor.LAYER_PATCH_ENTRY_WIDTH;
                    return m_TerrainPatches[y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, x / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES].Data[y, x];
                }
                set
                {
                    if (x >= m_Scene.RegionData.Size.X || y >= m_Scene.RegionData.Size.Y)
                    {
                        throw new KeyNotFoundException();
                    }

                    x /= LayerCompressor.LAYER_PATCH_ENTRY_WIDTH;
                    y /= LayerCompressor.LAYER_PATCH_ENTRY_WIDTH;
                    m_TerrainRwLock.AcquireWriterLock(-1);
                    try
                    {
                        m_TerrainPatches[y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, x / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES].Data[y, x] = (float)value;
                        m_TerrainPatchesDirty[y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, x / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES] = true;
                    }
                    finally
                    {
                        m_TerrainRwLock.ReleaseWriterLock();
                    }
                    UpdateTerrainDataToClients();
                }
            }

            public double this[Vector3 pos]
            {
                get
                {
                    if (pos.X < 0 || pos.Y < 0)
                    {
                        throw new KeyNotFoundException();
                    }

                    uint x = (uint)pos.X;
                    uint y = (uint)pos.Y;
                    return this[x, y];
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
        }
    }
}
