// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Viewer.Messages.LayerData;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace SilverSim.Scene.Implementation.Basic
{
    partial class BasicScene : ITerrainListener
    {
        readonly BlockingQueue<LayerPatch> m_TerrainStoreQueue = new BlockingQueue<LayerPatch>();
        readonly RwLockedDictionary<uint, uint> m_LastStoredTerrainSerial = new RwLockedDictionary<uint, uint>();

        public void TerrainUpdate(LayerPatch layerpath)
        {
            m_TerrainStoreQueue.Enqueue(layerpath);
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void StoreTerrainProcess()
        {
            Thread.CurrentThread.Name = "Terrain:Store for " + ID.ToString();
            while(!m_StopBasicSceneThreads)
            {
                LayerPatch lp;
                uint lastserial;
                try
                {
                    lp = m_TerrainStoreQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                if (m_LastStoredTerrainSerial.TryGetValue(lp.ExtendedPatchID, out lastserial))
                {
                    int age = (int)lp.Serial - (int)lastserial;
                    if(age < 0)
                    {
                        continue;
                    }
                }
                try
                {
#if DEBUG
                    m_Log.DebugFormat("Storing terrain segment {0},{1} for region {2} ({3})", lp.X, lp.Y, Name, ID);
#endif
                    m_SimulationDataStorage.Terrains[ID, lp.ExtendedPatchID] = lp;
                    m_LastStoredTerrainSerial[lp.ExtendedPatchID] = lp.Serial;
                }
                catch (Exception e)
                {
                    m_Log.WarnFormat("Failed to store terrain segment {0},{1} for region {3} ({4}): Reason: {2}", lp.X, lp.Y, e.Message, Name, ID);
                }
            }
        }
    }
}
