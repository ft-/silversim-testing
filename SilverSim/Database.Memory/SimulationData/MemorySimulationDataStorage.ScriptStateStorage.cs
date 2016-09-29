// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.Memory.SimulationData
{
    public partial class MemorySimulationDataStorage : ISimulationDataScriptStateStorageInterface
    {
        readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, byte[]>> m_ScriptStateData = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, byte[]>>(delegate () { return new RwLockedDictionary<string, byte[]>(); });

        string GenScriptStateKey(UUID primID, UUID itemID)
        {
            return primID.ToString() + ":" + itemID.ToString();
        }

        bool ISimulationDataScriptStateStorageInterface.TryGetValue(UUID regionID, UUID primID, UUID itemID, out byte[] state)
        {
            RwLockedDictionary<string, byte[]> states;
            state = null;
            return m_ScriptStateData.TryGetValue(regionID, out states) && states.TryGetValue(GenScriptStateKey(primID, itemID), out state);
        }

        /* setting value to null will delete the entry */
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        byte[] ISimulationDataScriptStateStorageInterface.this[UUID regionID, UUID primID, UUID itemID] 
        {
            get
            {
                byte[] state;
                if(!ScriptStates.TryGetValue(regionID, primID, itemID, out state))
                {
                    throw new KeyNotFoundException();
                }

                return state;
            }
            set
            {
                m_ScriptStateData[regionID][GenScriptStateKey(primID, itemID)] = value;
            }
        }

        bool ISimulationDataScriptStateStorageInterface.Remove(UUID regionID, UUID primID, UUID itemID)
        {
            RwLockedDictionary<string, byte[]> states;
            return m_ScriptStateData.TryGetValue(regionID, out states) && states.Remove(GenScriptStateKey(primID, itemID));
        }

        void RemoveAllScriptStatesInRegion(UUID regionID)
        {
            m_ScriptStateData.Remove(regionID);
        }
    }
}
