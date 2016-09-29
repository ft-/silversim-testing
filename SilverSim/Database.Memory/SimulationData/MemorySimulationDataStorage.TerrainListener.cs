// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Viewer.Messages.LayerData;
using System.Threading;

namespace SilverSim.Database.Memory.SimulationData
{
    public partial class MemorySimulationDataStorage
    {
        public class MemoryTerrainListener : TerrainListener
        {
            readonly UUID m_RegionID;
            readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, byte[]>> m_Data;

            public MemoryTerrainListener(RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, byte[]>> data, UUID regionID)
            {
                m_Data = data;
                m_RegionID = regionID;
            }

            protected override void StorageTerrainThread()
            {
                Thread.CurrentThread.Name = "Storage Terrain Thread: " + m_RegionID.ToString();

                C5.TreeDictionary<uint, uint> knownSerialNumbers = new C5.TreeDictionary<uint, uint>();

                while (!m_StopStorageThread || m_StorageTerrainRequestQueue.Count != 0)
                {
                    LayerPatch req;
                    try
                    {
                        req = m_StorageTerrainRequestQueue.Dequeue(1000);
                    }
                    catch
                    {
                        continue;
                    }

                    uint serialNumber = req.Serial;

                    if (!knownSerialNumbers.Contains(req.ExtendedPatchID) || knownSerialNumbers[req.ExtendedPatchID] != req.Serial)
                    {
                        m_Data[m_RegionID][req.ExtendedPatchID] = req.Serialization;
                        knownSerialNumbers[req.ExtendedPatchID] = serialNumber;
                    }
                }
            }
        }

        public override TerrainListener GetTerrainListener(UUID regionID)
        {
            return new MemoryTerrainListener(m_TerrainData, regionID);
        }
    }
}
