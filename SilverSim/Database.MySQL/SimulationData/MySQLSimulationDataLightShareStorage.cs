// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.SceneEnvironment;
using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.SimulationData
{
    public class MySQLSimulationDataLightShareStorage : SimulationDataLightShareStorageInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL LIGHTSHARE SETTINGS SERVICE");
        readonly string m_ConnectionString;

        public MySQLSimulationDataLightShareStorage(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public void ProcessMigrations()
        {
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "lightshare", Migrations, m_Log);
        }

        public override bool TryGetValue(UUID regionID, out EnvironmentController.WindlightSkyData skyData, out EnvironmentController.WindlightWaterData waterData)
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
                            skyData = EnvironmentController.WindlightSkyData.Defaults;
                            waterData = EnvironmentController.WindlightWaterData.Defaults;
                            return false;
                        }

                        skyData = new EnvironmentController.WindlightSkyData();
                        skyData.Ambient = reader.GetWLVector4("Ambient");
                        skyData.CloudColor = reader.GetWLVector4("CloudColor");
                        skyData.CloudCoverage = reader.GetDouble("CloudCoverage");
                        skyData.BlueDensity = reader.GetWLVector4("BlueDensity");
                        skyData.CloudDetailXYDensity.X = reader.GetDouble("CloudDetailXYDensityX");
                        skyData.CloudDetailXYDensity.Y = reader.GetDouble("CloudDetailXYDensityY");
                        skyData.CloudScale = reader.GetDouble("CloudScale");
                        skyData.CloudScrollX = reader.GetDouble("CloudScrollX");
                        skyData.CloudScrollXLock = MySQLUtilities.GetBool(reader, "CloudScrollXLock");
                        skyData.CloudScrollY = reader.GetDouble("CloudScrollY");
                        skyData.CloudScrollYLock = MySQLUtilities.GetBool(reader, "CloudScrollYLock");
                        skyData.CloudXYDensity.X = reader.GetDouble("CloudXYDensityX");
                        skyData.CloudXYDensity.Y = reader.GetDouble("CloudXYDensityY");
                        skyData.DensityMultiplier = reader.GetDouble("DensityMultiplier");
                        skyData.DistanceMultiplier = reader.GetDouble("DistanceMultiplier");
                        skyData.DrawClassicClouds = MySQLUtilities.GetBool(reader, "DrawClassicClouds");
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

                        waterData = new EnvironmentController.WindlightWaterData();
                        waterData.BigWaveDirection.X = reader.GetDouble("BigWaveDirectionX");
                        waterData.BigWaveDirection.Y = reader.GetDouble("BigWaveDirectionY");
                        waterData.LittleWaveDirection.X = reader.GetDouble("LittleWaveDirectionX");
                        waterData.LittleWaveDirection.Y = reader.GetDouble("LittleWaveDirectionY");
                        waterData.BlurMultiplier = reader.GetDouble("BlurMultiplier");
                        waterData.FresnelScale = reader.GetDouble("FresnelScale");
                        waterData.FresnelOffset = reader.GetDouble("FresnelOffset");
                        waterData.NormalMapTexture = reader.GetUUID("NormalMapTexture");
                        waterData.ReflectionWaveletScale.X = reader.GetDouble("ReflectionWaveletScaleX");
                        waterData.ReflectionWaveletScale.Y = reader.GetDouble("ReflectionWaveletScaleY");
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

        public override void Store(UUID regionID, EnvironmentController.WindlightSkyData skyData, EnvironmentController.WindlightWaterData waterData)
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
                data["CloudDetailXYDensityX"] = skyData.CloudDetailXYDensity.X;
                data["CloudDetailXYDensityY"] = skyData.CloudDetailXYDensity.Y;
                data["CloudScale"] = skyData.CloudScale;
                data["CloudScrollX"] = skyData.CloudScrollX;
                data["CloudScrollXLock"] = skyData.CloudScrollXLock;
                data["CloudScrollY"] = skyData.CloudScrollY;
                data["CloudScrollYLock"] = skyData.CloudScrollYLock;
                data["CloudXYDensityX"] = skyData.CloudXYDensity.X;
                data["CloudXYDensityY"] = skyData.CloudXYDensity.Y;
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

                data["BigWaveDirectionX"] = waterData.BigWaveDirection.X;
                data["BigWaveDirectionY"] = waterData.BigWaveDirection.Y;
                data["LittleWaveDirectionX"] = waterData.LittleWaveDirection.X;
                data["LittleWaveDirectionY"] = waterData.LittleWaveDirection.Y;
                data["BlurMultiplier"] = waterData.BlurMultiplier;
                data["FresnelScale"] = waterData.FresnelScale;
                data["FresnelOffset"] = waterData.FresnelOffset;
                data["NormalMapTexture"] = waterData.NormalMapTexture.ToString();
                data["ReflectionWaveletScaleX"] = waterData.ReflectionWaveletScale.X;
                data["ReflectionWaveletScaleY"] = waterData.ReflectionWaveletScale.Y;
                data["RefractScaleAbove"] = waterData.RefractScaleAbove;
                data["RefractScaleBelow"] = waterData.RefractScaleBelow;
                data["UnderwaterFogModifier"] = waterData.UnderwaterFogModifier;
                data["WaterColor"] = waterData.Color;
                data["FogDensityExponent"] = waterData.FogDensityExponent;

                conn.ReplaceInto("lightshare", data);
            }
        }

        public override bool Remove(UUID regionID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM lightshare WHERE RegionID LIKE ?regionid", conn))
                {
                    cmd.Parameters.AddWithValue("?regionid", regionID.ToString());
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        private static readonly string[] Migrations = new string[]{
            "CREATE TABLE %tablename% (" +
                "RegionID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "AmbientRed DOUBLE NOT NULL, AmbientGreen DOUBLE NOT NULL, AmbientBlue DOUBLE NOT NULL, AmbientValue DOUBLE NOT NULL," +
                "CloudColorRed DOUBLE NOT NULL, CloudColorGreen DOUBLE NOT NULL, CloudColorBlue DOUBLE NOT NULL, CloudColorValue DOUBLE NOT NULL," +
                "CloudCoverage DOUBLE NOT NULL," +
                "CloudDetailXYDensityX DOUBLE NOT NULL, CloudDetailXYDensityY DOUBLE NOT NULL," +
                "CloudScale DOUBLE NOT NULL," +
                "CloudScrollX DOUBLE NOT NULL, CloudScrollXLock INT(1) UNSIGNED NOT NULL," +
                "CloudScrollY DOUBLE NOT NULL, CloudScrollYLock INT(1) UNSIGNED NOT NULL," +
                "CloudXYDensityX DOUBLE NOT NULL, CloudXYDensityY DOUBLE NOT NULL," +
                "DensityMultiplier DOUBLE NOT NULL," +
                "DistanceMultiplier DOUBLE NOT NULL, DrawClassicClouds INT(1) UNSIGNED NOT NULL," +
                "EastAngle DOUBLE NOT NULL," +
                "HazeDensity DOUBLE NOT NULL, HazeHorizon DOUBLE NOT NULL," +
                "HorizonRed DOUBLE NOT NULL, HorizonGreen DOUBLE NOT NULL, HorizonBlue DOUBLE NOT NULL, HorizonValue DOUBLE NOT NULL," +
                "MaxAltitude INT(11) NOT NULL, SceneGamma DOUBLE NOT NULL," +
                "StarBrightness DOUBLE NOT NULL, " +
                "SunGlowFocus DOUBLE NOT NULL, SunGlowSize DOUBLE NOT NULL," +
                "SunMoonColorRed DOUBLE NOT NULL, SunMoonColorGreen DOUBLE NOT NULL, SunMoonColorBlue DOUBLE NOT NULL, SunMoonColorValue DOUBLE NOT NULL," +
                "SunMoonPosition DOUBLE NOT NULL," +
                "BigWaveDirectionX DOUBLE NOT NULL, BigWaveDirectionY DOUBLE NOT NULL," +
                "LittleWaveDirectionX DOUBLE NOT NULL, LittleWaveDirectionY DOUBLE NOT NULL," +
                "BlurMultiplier DOUBLE NOT NULL, FresnelScale DOUBLE NOT NULL," +
                "FresnelOffset DOUBLE NOT NULL, NormalMapTexture CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "ReflectionWaveletScaleX DOUBLE NOT NULL, ReflectionWaveletScaleY DOUBLE NOT NULL," +
                "RefractScaleAbove DOUBLE NOT NULL, RefractScaleBelow DOUBLE NOT NULL," +
                "UnderwaterFogModifier DOUBLE NOT NULL, " +
                "WaterColorRed DOUBLE NOT NULL, WaterColorGreen DOUBLE NOT NULL, WaterColorBlue DOUBLE NOT NULL," +
                "FogDensityExponent DOUBLE NOT NULL," +
                "PRIMARY KEY(RegionID))",
            "ALTER TABLE %tablename% ADD COLUMN (BlueDensityRed DOUBLE NOT NULL, " +
                            "BlueDensityGreen DOUBLE NOT NULL," +
                            "BlueDensityBlue DOUBLE NOT NULL," +
                            "BlueDensityValue DOUBLE NOT NULL),",
        };
    }
}
