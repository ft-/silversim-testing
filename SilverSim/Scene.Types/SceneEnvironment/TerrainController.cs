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

using log4net;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Viewer.Messages.LayerData;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Scene.Types.SceneEnvironment
{
    public class TerrainController
    {
        private static readonly ILog m_Log = LogManager.GetLogger("TERRAIN CONTROLLER");

        private const int BASE_REGION_SIZE = 256;
        private const double DEFAULT_TERRAIN_HEIGHT = 21;

        private readonly SceneInterface m_Scene;
        private readonly ReaderWriterLock m_TerrainRwLock = new ReaderWriterLock();

        private readonly LayerPatch[,] m_TerrainPatches;

        public readonly RwLockedList<ITerrainListener> TerrainListeners = new RwLockedList<ITerrainListener>();

        private float m_LowerLimit;
        private float m_RaiseLimit;

        public float LowerLimit
        {
            get
            {
                return m_TerrainRwLock.AcquireReaderLock(() => m_LowerLimit);
            }
            set
            {
                m_TerrainRwLock.AcquireWriterLock(() => m_LowerLimit = value);
            }
        }

        public float RaiseLimit
        {
            get
            {
                return m_TerrainRwLock.AcquireReaderLock(() => m_RaiseLimit);
            }
            set
            {
                m_TerrainRwLock.AcquireWriterLock(() => m_RaiseLimit = value);
            }
        }

        public TerrainController(SceneInterface scene)
        {
            uint x;
            uint y;

            uint xPatches = scene.SizeX / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;
            uint yPatches = scene.SizeY / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;

            LowerLimit = (float)scene.RegionSettings.TerrainLowerLimit;
            RaiseLimit = (float)scene.RegionSettings.TerrainRaiseLimit;

            m_Scene = scene;
            m_TerrainPatches = new LayerPatch[yPatches, xPatches];

            for (y = 0; y < yPatches; ++y)
            {
                for (x = 0; x < xPatches; ++x)
                {
                    m_TerrainPatches[y, x] = new LayerPatch(22)
                    {
                        X = x,
                        Y = y
                    };
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
        private List<LayerData> CompileTerrainData(IAgent agent) => m_TerrainRwLock.AcquireReaderLock(() =>
        {
            int y;
            int x;
            var mlist = new List<LayerData>();
            var dirtyPatches = new List<LayerPatch>();
            RwLockedDictionary<uint, uint> agentSceneSerials = agent.TransmittedTerrainSerials[m_Scene.ID];

            for (y = 0; y < m_Scene.SizeY / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++y)
            {
                for (x = 0; x < m_Scene.SizeX / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++x)
                {
                    LayerPatch patch = m_TerrainPatches[y, x];
                    uint serial;
                    if (agentSceneSerials.TryGetValue(patch.ExtendedPatchID, out serial))
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
            var layerType = LayerData.LayerDataType.Land;

            if (BASE_REGION_SIZE < m_Scene.SizeX || BASE_REGION_SIZE < m_Scene.SizeY)
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
        });

        public void UpdateTerrainDataToSingleClient(IAgent agent)
        {
            foreach (LayerData m in CompileTerrainData(agent))
            {
                agent.SendMessageAlways(m, m_Scene.ID);
            }
        }

        public void UpdateTerrainDataToClients()
        {
            foreach (IAgent agent in m_Scene.Agents)
            {
                foreach (LayerData m in CompileTerrainData(agent))
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

        public Vector3 SurfaceNormal(double posX, double posY)
        {
            // Clamp to valid position
            var iposX = (uint)posX.Clamp(0, m_Scene.SizeX);
            var iposY = (uint)posY.Clamp(0, m_Scene.SizeY);

            uint iposX_plus_1 = iposX + 1;
            uint iposY_plus_1 = iposY + 1;

            double t00 = this[iposX, iposY];
            double zx = iposX_plus_1 >= m_Scene.SizeX ? 0 : this[iposX_plus_1, iposY] - t00;
            double zy = iposY_plus_1 >= m_Scene.SizeY ? 0 : this[iposX, iposY_plus_1] - t00;

            /* Calculate the cross product (the surface normal). */
            return new Vector3(
                -zx,
                zx - zy,
                1);
        }

        public Vector3 SurfaceSlope(double posX, double posY)
        {
            Vector3 vsn = SurfaceNormal(posX, posY).Normalize();

            /* Put the x,y coordinates of the slope normal into the plane equation to get
             * the height of that point on the plane.  
             * The resulting vector provides the slope.
             * 
             * Info from old lslwiki data:
             * The slope of the ground is direction the land is 'laying'. It is always orthogonal to llGroundNormal.
             * Example: A 'cliff' would have a slope of approximately <0, 0, -1> (pointing down).
             */
            Vector3 vsl = vsn;
            vsl.Z = ((vsn.X * vsn.X) + (vsn.Y * vsn.Y)) / (-1 * vsn.Z);

            return vsl;
        }

        public Vector3 SurfaceContour(double posX, double posY)
        {
            /* Info from old lslwiki data:
             * The ground contour is the direction in which there is no change in height.
             * Example: Imagine taking a horizontal "slice" of the sim's land at the height of the ground being sampled. The ground contour would point in the direction of the outline created.             * 
             */
            Vector3 v = SurfaceSlope(posX, posY);
            return new Vector3(-v.Y, v.X, 0);
        }

        #region Properties
        public double this[uint x, uint y]
        {
            get
            {
                if (x >= m_Scene.SizeX || y >= m_Scene.SizeY)
                {
                    throw new KeyNotFoundException();
                }
                return m_TerrainPatches[x / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES].Data[y % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, x % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES];
            }
            set
            {
                LayerPatch lp = null;
                if (x >= m_Scene.SizeX || y >= m_Scene.SizeY)
                {
                    throw new KeyNotFoundException();
                }

                m_TerrainRwLock.AcquireWriterLock(() =>
                {
                    lp = m_TerrainPatches[x / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES];
                    lp.Data[y % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, x % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES] = (float)value;
                    lp.IncrementSerial();
                });
                if (lp != null)
                {
                    lp.Dirty = true;
                }
            }
        }

        public void Flush()
        {
            var updatedPatches = new List<LayerPatch>();
            m_TerrainRwLock.AcquireReaderLock(-1);
            try
            {
                int x;
                int y;

                for (y = 0; y < m_Scene.SizeY / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++y)
                {
                    for (x = 0; x < m_Scene.SizeX / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++x)
                    {
                        LayerPatch patch = m_TerrainPatches[y, x];
                        if(patch.Dirty)
                        {
                            updatedPatches.Add(patch);
                            patch.Dirty = false;
                        }
                    }
                }
            }
            finally
            {
                m_TerrainRwLock.ReleaseReaderLock();
            }
            UpdateTerrainDataToClients();
            foreach (LayerPatch lp in updatedPatches)
            {
                foreach (ITerrainListener listener in TerrainListeners)
                {
                    listener.TerrainUpdate(new LayerPatch(lp));
                }
            }
        }

        public LayerPatch AdjustTerrain(uint x, uint y, double change)
        {
            if (x >= m_Scene.SizeX || y >= m_Scene.SizeY)
            {
                throw new KeyNotFoundException();
            }

            return m_TerrainRwLock.AcquireWriterLock(() =>
            {
                LayerPatch lp = m_TerrainPatches[x / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES];
                float val = lp.Data[y % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, x % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES] + (float)change;
                if (val < LowerLimit)
                {
                    val = LowerLimit;
                }
                else if (val > RaiseLimit)
                {
                    val = RaiseLimit;
                }
                lp.Data[y % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, x % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES] = val;
                return lp;
            });
        }

        public LayerPatch BlendTerrain(uint x, uint y, double newval, double mix /* 0. orig only , 1. new only */)
        {
            if (x >= m_Scene.SizeX || y >= m_Scene.SizeY)
            {
                throw new KeyNotFoundException();
            }

            return m_TerrainRwLock.AcquireWriterLock(() =>
            {
                var lp = m_TerrainPatches[x / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES];
                float val =
                    (float)(lp.Data[y % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, x % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES] * (1 - mix)) +
                    (float)(newval * mix);
                if (val < LowerLimit)
                {
                    val = LowerLimit;
                }
                else if (val > RaiseLimit)
                {
                    val = RaiseLimit;
                }
                lp.Data[y % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, x % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES] = val;
                return lp;
            });
        }

        public double this[Vector3 pos]
        {
            get
            {
                var x = (int)pos.X.Clamp(0, m_Scene.SizeX - 1);
                var y = (int)pos.Y.Clamp(0, m_Scene.SizeY - 1);
                return this[(uint)x, (uint)y];
            }
            set
            {
                if (pos.X < 0 || pos.Y < 0)
                {
                    throw new KeyNotFoundException();
                }

                var x = (uint)pos.X;
                var y = (uint)pos.Y;
                if (value < LowerLimit)
                {
                    this[x, y] = LowerLimit;
                }
                else if (value > RaiseLimit)
                {
                    this[x, y] = RaiseLimit;
                }
                else
                {
                    this[x, y] = (float)value;
                }
            }
        }
        #endregion

        #region Access Terrain Data
        public readonly PatchesAccessor Patch;

        public List<LayerPatch> AllPatches
        {
            get
            {
                int xPatches = (int)m_Scene.SizeX / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;
                int yPatches = (int)m_Scene.SizeY / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;

                var patches = new List<LayerPatch>();
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
                foreach (LayerPatch p in value)
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
            private readonly LayerPatch[,] m_TerrainPatches;
            private readonly uint m_NumXPatches;
            private readonly uint m_NumYPatches;

            public PatchesAccessor(LayerPatch[,] terrainPatches, uint xPatches, uint yPatches)
            {
                m_TerrainPatches = terrainPatches;
                m_NumXPatches = xPatches;
                m_NumYPatches = yPatches;
            }

            public LayerPatch this[uint x, uint y]
            {
                get
                {
                    if (x >= m_NumXPatches || y >= m_NumYPatches)
                    {
                        throw new KeyNotFoundException();
                    }
                    return new LayerPatch(m_TerrainPatches[y, x]);
                }
            }

            public void MarkDirty(uint x, uint y)
            {
                if (x >= m_NumXPatches || y >= m_NumYPatches)
                {
                    throw new KeyNotFoundException();
                }
                m_TerrainPatches[y, x].Dirty = true;
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
