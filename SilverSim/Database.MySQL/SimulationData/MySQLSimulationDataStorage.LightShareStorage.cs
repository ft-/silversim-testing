// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Types;
using System.Collections.Generic;
using EnvController = SilverSim.Scene.Types.SceneEnvironment.EnvironmentController;

namespace SilverSim.Database.MySQL.SimulationData
{
    public partial class MySQLSimulationDataStorage : ISimulationDataLightShareStorageInterface
    {
        bool ISimulationDataLightShareStorageInterface.TryGetValue(UUID regionID, out EnvController.WindlightSkyData skyData, out EnvController.WindlightWaterData waterData)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM lightshare WHERE RegionID LIKE ?regionid", conn))
                {
                    cmd.Parameters.AddWithValue("?regionid", regionID.ToString());
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if(!reader.Read())
                        {
                            skyData = EnvController.WindlightSkyData.Defaults;
                            waterData = EnvController.WindlightWaterData.Defaults;
                            return false;
                        }

                        skyData = new EnvController.WindlightSkyData();
                        skyData.Ambient = reader.GetWLVector4("Ambient");
                        skyData.CloudColor = reader.GetWLVector4("CloudColor");
                        skyData.CloudCoverage = reader.GetDouble("CloudCoverage");
                        skyData.BlueDensity = reader.GetWLVector4("BlueDensity");
                        skyData.CloudDetailXYDensity = reader.GetVector3("CloudDetailXYDensity");
                        skyData.CloudScale = reader.GetDouble("CloudScale");
                        skyData.CloudScroll = reader.GetWLVector2("CloudScroll");
                        skyData.CloudScrollXLock = reader.GetBool("CloudScrollXLock");
                        skyData.CloudScrollYLock = reader.GetBool("CloudScrollYLock");
                        skyData.CloudXYDensity = reader.GetVector3("CloudXYDensity");
                        skyData.DensityMultiplier = reader.GetDouble("DensityMultiplier");
                        skyData.DistanceMultiplier = reader.GetDouble("DistanceMultiplier");
                        skyData.DrawClassicClouds = reader.GetBool("DrawClassicClouds");
                        skyData.EastAngle = reader.GetDouble("EastAngle");
                        skyData.HazeDensity = reader.GetDouble("HazeDensity");
                        skyData.HazeHorizon = reader.GetDouble("HazeHorizon");
                        skyData.Horizon = reader.GetWLVector4("Horizon");
                        skyData.MaxAltitude = reader.GetInt32("MaxAltitude");
                        skyData.SceneGamma = reader.GetDouble("SceneGamma");
                        skyData.SunGlowFocus = reader.GetDouble("SunGlowFocus");
                        skyData.SunGlowSize = reader.GetDouble("SunGlowSize");
                        skyData.SunMoonColor = reader.GetWLVector4("SunMoonColor");
                        skyData.SunMoonPosition = reader.GetDouble("SunMoonPosition");

                        waterData = new EnvController.WindlightWaterData();
                        waterData.BigWaveDirection = reader.GetWLVector2("BigWaveDirection");
                        waterData.LittleWaveDirection = reader.GetWLVector2("LittleWaveDirection");
                        waterData.BlurMultiplier = reader.GetDouble("BlurMultiplier");
                        waterData.FresnelScale = reader.GetDouble("FresnelScale");
                        waterData.FresnelOffset = reader.GetDouble("FresnelOffset");
                        waterData.NormalMapTexture = reader.GetUUID("NormalMapTexture");
                        waterData.ReflectionWaveletScale = reader.GetVector3("ReflectionWaveletScale");
                        waterData.RefractScaleAbove = reader.GetDouble("RefractScaleAbove");
                        waterData.RefractScaleBelow = reader.GetDouble("RefractScaleBelow");
                        waterData.UnderwaterFogModifier = reader.GetDouble("UnderwaterFogModifier");
                        waterData.Color = reader.GetColor("WaterColor");
                        waterData.FogDensityExponent = reader.GetDouble("FogDensityExponent");
                        return true;
                    }
                }
            }
        }

        void ISimulationDataLightShareStorageInterface.Store(UUID regionID, EnvController.WindlightSkyData skyData, EnvController.WindlightWaterData waterData)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();

                Dictionary<string, object> data = new Dictionary<string, object>();
                data["RegionID"] = regionID;
                data["Ambient"] = skyData.Ambient;
                data["CloudColor"] = skyData.CloudColor;
                data["CloudCoverage"] = skyData.CloudCoverage;
                data["BlueDensity"] = skyData.BlueDensity;
                data["CloudDetailXYDensity"] = skyData.CloudDetailXYDensity;
                data["CloudScale"] = skyData.CloudScale;
                data["CloudScroll"] = skyData.CloudScroll;
                data["CloudScrollXLock"] = skyData.CloudScrollXLock;
                data["CloudScrollYLock"] = skyData.CloudScrollYLock;
                data["CloudXYDensity"] = skyData.CloudXYDensity;
                data["DensityMultiplier"] = skyData.DensityMultiplier;
                data["DistanceMultiplier"] = skyData.DistanceMultiplier;
                data["DrawClassicClouds"] = skyData.DrawClassicClouds;
                data["EastAngle"] = skyData.EastAngle;
                data["HazeDensity"] = skyData.HazeDensity;
                data["HazeHorizon"] = skyData.HazeHorizon;
                data["Horizon"] = skyData.Horizon;
                data["MaxAltitude"] = skyData.MaxAltitude;
                data["SceneGamma"] = skyData.SceneGamma;
                data["StarBrightness"] = skyData.StarBrightness;
                data["SunGlowFocus"] = skyData.SunGlowFocus;
                data["SunGlowSize"] = skyData.SunGlowSize;
                data["SunMoonColor"] = skyData.SunMoonColor;
                data["SunMoonPosition"] = skyData.SunMoonPosition;

                data["BigWaveDirection"] = waterData.BigWaveDirection;
                data["LittleWaveDirection"] = waterData.LittleWaveDirection;
                data["BlurMultiplier"] = waterData.BlurMultiplier;
                data["FresnelScale"] = waterData.FresnelScale;
                data["FresnelOffset"] = waterData.FresnelOffset;
                data["NormalMapTexture"] = waterData.NormalMapTexture.ToString();
                data["ReflectionWaveletScale"] = waterData.ReflectionWaveletScale;
                data["RefractScaleAbove"] = waterData.RefractScaleAbove;
                data["RefractScaleBelow"] = waterData.RefractScaleBelow;
                data["UnderwaterFogModifier"] = waterData.UnderwaterFogModifier;
                data["WaterColor"] = waterData.Color;
                data["FogDensityExponent"] = waterData.FogDensityExponent;

                conn.ReplaceInto("lightshare", data);
            }
        }

        bool ISimulationDataLightShareStorageInterface.Remove(UUID regionID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM lightshare WHERE RegionID LIKE ?regionid", conn))
                {
                    cmd.Parameters.AddParameter("?regionid", regionID);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
