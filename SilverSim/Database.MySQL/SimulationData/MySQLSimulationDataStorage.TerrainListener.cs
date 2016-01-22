// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.Types;
using SilverSim.Viewer.Messages.LayerData;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Database.MySQL.SimulationData
{
    public partial class MySQLSimulationDataStorage
    {
        public class MySQLTerrainListener : TerrainListener
        {
            readonly string m_ConnectionString;
            readonly UUID m_RegionID;

            public MySQLTerrainListener(string connectionString, UUID regionID)
            {
                m_ConnectionString = connectionString;
                m_RegionID = regionID;
            }

            protected override void StorageTerrainThread()
            {
                Thread.CurrentThread.Name = "Storage Terrain Thread: " + m_RegionID.ToString();

                C5.TreeDictionary<uint, uint> knownSerialNumbers = new C5.TreeDictionary<uint, uint>();
                string replaceIntoTerrain = string.Empty;
                List<string> updateRequests = new List<string>();

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
                        Dictionary<string, object> data = new Dictionary<string, object>();
                        data["RegionID"] = m_RegionID;
                        data["PatchID"] = req.ExtendedPatchID;
                        data["TerrainData"] = req.Serialization;
                        if (replaceIntoTerrain.Length == 0)
                        {
                            replaceIntoTerrain = "REPLACE INTO terrains (" + MySQLUtilities.GenerateFieldNames(data) + ") VALUES ";
                        }
                        updateRequests.Add("(" + MySQLUtilities.GenerateValues(data) + ")");
                        knownSerialNumbers[req.ExtendedPatchID] = serialNumber;
                    }

                    if((m_StorageTerrainRequestQueue.Count == 0 && updateRequests.Count > 0) || updateRequests.Count >= 256)
                    {
                        string elems = string.Join(",", updateRequests);
                        try
                        {
                            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                            {
                                conn.Open();
                                using (MySqlCommand cmd = new MySqlCommand(replaceIntoTerrain + elems, conn))
                                {
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            updateRequests.Clear();
                        }
                        catch(Exception e)
                        {
                            m_Log.Error("Terrain store failed", e);
                        }
                    }
                }
            }
        }

        public override TerrainListener GetTerrainListener(UUID regionID)
        {
            return new MySQLTerrainListener(m_ConnectionString, regionID);
        }
    }
}
