// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Viewer.Messages.LayerData;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using SilverSim.Scene.Types.SceneEnvironment;
using SilverSim.Scene.Types.WindLight;
using SilverSim.Scripting.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Types.Asset;

namespace SilverSim.Main.Common
{
    public static class SceneLoadingExtensionMethods
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LOAD SCENE");

        private struct SceneLoadingParams
        {
            public SceneInterface Scene;
            public SimulationDataStorageInterface SimulationDataStorage;
        }

        public static void LoadSceneAsync(this SceneInterface scene, SimulationDataStorageInterface simulationDataStorage)
        {
            lock (scene.m_LoaderThreadLock)
            {
                if (scene.m_LoaderThread == null && !scene.IsSceneEnabled)
                {
                    SceneLoadingParams loadparams = new SceneLoadingParams();
                    loadparams.Scene = scene;
                    loadparams.SimulationDataStorage = simulationDataStorage;
                    scene.m_LoaderThread = new Thread(LoadSceneThread);
                    scene.m_LoaderThread.Start(loadparams);
                }
            }
        }

        /** <summary>only for testing code</summary> */
        public static void LoadSceneSync(this SceneInterface scene, SimulationDataStorageInterface simulationDataStorage)
        {
            m_Log.Error("Do not use LoadSceneSync in production software");
            lock (scene.m_LoaderThreadLock)
            {
                if (scene.m_LoaderThread == null && !scene.IsSceneEnabled)
                {
                    SceneLoadingParams loadparams = new SceneLoadingParams();
                    loadparams.Scene = scene;
                    loadparams.SimulationDataStorage = simulationDataStorage;
                    scene.m_LoaderThread = new Thread(LoadSceneThread);
                    /* we put a thread in there for ensuring correct sequence but we do not start it */
                    LoadSceneMain(loadparams);
                }
            }
        }

        static void LoadSceneThread(object o)
        {
            SceneLoadingParams loadparams = (SceneLoadingParams)o;
            Thread.CurrentThread.Name = "Scene Loading Thread for " + loadparams.Scene.Name + " (" + loadparams.Scene.ID.ToString() + ")";
            LoadSceneMain(loadparams);
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        static void LoadSceneMain(SceneLoadingParams loadparams)
        {
            List<UUID> parcels;
            try
            {
                lock (loadparams.Scene.m_LoaderThreadLock)
                {
                    try
                    {
                        EnvironmentSettings settings;
                        if (loadparams.SimulationDataStorage.EnvironmentSettings.TryGetValue(loadparams.Scene.ID, out settings))
                        {
                            loadparams.Scene.EnvironmentSettings = settings;
                            m_Log.InfoFormat("Loaded environment settings for {0} ({1})", loadparams.Scene.Name, loadparams.Scene.ID);
                        }
                    }
                    catch(Exception e)
                    {
                        m_Log.ErrorFormat("Failed to load environment settings for {0} ({1}): {2}: {3}\n{4}",
                            loadparams.Scene.Name, loadparams.Scene.ID, e.GetType().FullName, e.Message, e.StackTrace);
                        loadparams.Scene.EnvironmentSettings = null;
                    }
                }

                lock (loadparams.Scene.m_LoaderThreadLock)
                {
                    RegionSettings settings;
                    bool storeSettings = false;
                    if(!loadparams.SimulationDataStorage.RegionSettings.TryGetValue(loadparams.Scene.ID, out settings))
                    {
                        settings = new RegionSettings();
                        m_Log.InfoFormat("Initializing with region settings defaults for {0} ({1})", loadparams.Scene.Name, loadparams.Scene.ID);
                        storeSettings = true;
                    }
                    else
                    {
                        m_Log.InfoFormat("Loaded region settings for {0} ({1})", loadparams.Scene.Name, loadparams.Scene.ID);
                    }
                    loadparams.Scene.RegionSettings.BlockTerraform = settings.BlockTerraform;
                    loadparams.Scene.RegionSettings.BlockFly = settings.BlockFly;
                    loadparams.Scene.RegionSettings.AllowDamage = settings.AllowDamage;
                    loadparams.Scene.RegionSettings.RestrictPushing = settings.RestrictPushing;
                    loadparams.Scene.RegionSettings.AllowLandResell = settings.AllowLandResell;
                    loadparams.Scene.RegionSettings.AllowLandJoinDivide = settings.AllowLandJoinDivide;
                    loadparams.Scene.RegionSettings.BlockShowInSearch = settings.BlockShowInSearch;
                    loadparams.Scene.RegionSettings.AgentLimit = settings.AgentLimit;
                    loadparams.Scene.RegionSettings.ObjectBonus = settings.ObjectBonus;
                    loadparams.Scene.RegionSettings.DisableScripts = settings.DisableScripts;
                    loadparams.Scene.RegionSettings.DisableCollisions = settings.DisableCollisions;
                    loadparams.Scene.RegionSettings.BlockFlyOver = settings.BlockFlyOver;
                    loadparams.Scene.RegionSettings.Sandbox = settings.Sandbox;
                    loadparams.Scene.RegionSettings.TerrainTexture1 = settings.TerrainTexture1;
                    loadparams.Scene.RegionSettings.TerrainTexture2 = settings.TerrainTexture2;
                    loadparams.Scene.RegionSettings.TerrainTexture3 = settings.TerrainTexture3;
                    loadparams.Scene.RegionSettings.TerrainTexture4 = settings.TerrainTexture4;
                    loadparams.Scene.RegionSettings.TelehubObject = settings.TelehubObject;
                    loadparams.Scene.RegionSettings.Elevation1NW = settings.Elevation1NW;
                    loadparams.Scene.RegionSettings.Elevation2NW = settings.Elevation2NW;
                    loadparams.Scene.RegionSettings.Elevation1NE = settings.Elevation1NE;
                    loadparams.Scene.RegionSettings.Elevation2NE = settings.Elevation2NE;
                    loadparams.Scene.RegionSettings.Elevation1SE = settings.Elevation1SE;
                    loadparams.Scene.RegionSettings.Elevation2SE = settings.Elevation2SE;
                    loadparams.Scene.RegionSettings.Elevation1SW = settings.Elevation1SW;
                    loadparams.Scene.RegionSettings.Elevation2SW = settings.Elevation2SW;
                    loadparams.Scene.RegionSettings.WaterHeight = settings.WaterHeight;
                    loadparams.Scene.RegionSettings.TerrainRaiseLimit = settings.TerrainRaiseLimit;
                    loadparams.Scene.RegionSettings.TerrainLowerLimit = settings.TerrainLowerLimit;

                    if(storeSettings)
                    {
                        loadparams.SimulationDataStorage.RegionSettings[loadparams.Scene.ID] = settings;
                    }
                }

                lock(loadparams.Scene.m_LoaderThreadLock)
                {
                    List<Vector3> spawns = loadparams.SimulationDataStorage.Spawnpoints[loadparams.Scene.ID];
                    loadparams.Scene.SpawnPoints = spawns;
                }

                lock(loadparams.Scene.m_LoaderThreadLock)
                {
                    EnvironmentController.WindlightSkyData skyData;
                    EnvironmentController.WindlightWaterData waterData;
                    if(loadparams.SimulationDataStorage.LightShare.TryGetValue(loadparams.Scene.ID, out skyData, out waterData))
                    {
                        loadparams.Scene.Environment.SkyData = skyData;
                        loadparams.Scene.Environment.WaterData = waterData;
                        m_Log.InfoFormat("Loaded LightShare settings for {0} ({1})", loadparams.Scene.Name, loadparams.Scene.ID);
                    }
                }

                lock (loadparams.Scene.m_LoaderThreadLock)
                {
                    parcels = loadparams.SimulationDataStorage.Parcels.ParcelsInRegion(loadparams.Scene.ID);
                }
                if (parcels.Count == 1)
                {
                    m_Log.InfoFormat("Loading {0} parcel for {1} ({2})", parcels.Count, loadparams.Scene.Name, loadparams.Scene.ID);
                }
                else
                {
                    m_Log.InfoFormat("Loading {0} parcels for {1} ({2})", parcels.Count, loadparams.Scene.Name, loadparams.Scene.ID);
                }
                if (parcels.Count != 0)
                {
                    foreach (UUID parcelid in parcels)
                    {
                        try
                        {
                            lock (loadparams.Scene.m_LoaderThreadLock)
                            {
                                ParcelInfo pi = loadparams.SimulationDataStorage.Parcels[loadparams.Scene.ID, parcelid];
                                loadparams.Scene.AddParcelNoTrigger(pi);
                            }
                        }
                        catch (Exception e)
                        {
                            m_Log.WarnFormat("Loading parcel {0} for {3} ({4}) failed: {2}: {1}\n{5}", parcelid, e.Message, e.GetType().FullName, loadparams.Scene.Name, loadparams.Scene.ID, e.StackTrace);
                        }

                    }
                }

                if (parcels.Count == 0)
                {
                    ParcelInfo pi = new ParcelInfo((int)loadparams.Scene.SizeX / 4, (int)loadparams.Scene.SizeY / 4);
                    pi.AABBMin = new Vector3(0, 0, 0);
                    pi.AABBMax = new Vector3(loadparams.Scene.SizeX - 1, loadparams.Scene.SizeY - 1, 0);
                    pi.ActualArea = (int)(loadparams.Scene.SizeX * loadparams.Scene.SizeY);
                    pi.Area = (int)(loadparams.Scene.SizeX * loadparams.Scene.SizeY);
                    pi.AuctionID = 0;
                    pi.LocalID = 1;
                    pi.ID = UUID.Random;
                    pi.Name = "Your Parcel";
                    pi.Owner = loadparams.Scene.Owner;
                    pi.Flags = ParcelFlags.None; /* we keep all flags disabled initially */
                    pi.BillableArea = (int)(loadparams.Scene.SizeX * loadparams.Scene.SizeY);
                    pi.LandBitmap.SetAllBits();
                    pi.LandingType = TeleportLandingType.Anywhere;
                    pi.LandingPosition = new Vector3(128, 128, 23);
                    pi.LandingLookAt = new Vector3(1, 0, 0);
                    pi.ClaimDate = new Date();
                    pi.Status = ParcelStatus.Leased;
                    loadparams.SimulationDataStorage.Parcels.Store(loadparams.Scene.ID, pi);
                    loadparams.Scene.AddParcel(pi);
                    m_Log.InfoFormat("Auto-generated default parcel for {1} ({2})", parcels.Count, loadparams.Scene.Name, loadparams.Scene.ID);
                }
                else if (parcels.Count == 1)
                {
                    m_Log.InfoFormat("Loaded {0} parcel for {1} ({2})", parcels.Count, loadparams.Scene.Name, loadparams.Scene.ID);
                }
                else
                {
                    m_Log.InfoFormat("Loaded {0} parcels for {1} ({2})", parcels.Count, loadparams.Scene.Name, loadparams.Scene.ID);
                }

                List<UUID> objects;
                lock (loadparams.Scene.m_LoaderThreadLock)
                {
                    objects = loadparams.SimulationDataStorage.Objects.ObjectsInRegion(loadparams.Scene.ID);
                }
                if (objects.Count == 1)
                {
                    m_Log.InfoFormat("Loading {0} object for {1} ({2})", objects.Count, loadparams.Scene.Name, loadparams.Scene.ID);
                }
                else
                {
                    m_Log.InfoFormat("Loading {0} objects for {1} ({2})", objects.Count, loadparams.Scene.Name, loadparams.Scene.ID);
                }
                if (objects.Count != 0)
                {
                    List<ObjectGroup> objGroups = loadparams.SimulationDataStorage.Objects[loadparams.Scene.ID];
                    m_Log.InfoFormat("Adding objects to {0} ({1})", loadparams.Scene.Name, loadparams.Scene.ID);
                    foreach (ObjectGroup grp in objGroups)
                    {
                        try
                        {
                            lock (loadparams.Scene.m_LoaderThreadLock)
                            {
                                loadparams.Scene.Add(grp);
                            }
                        }
                        catch (Exception e)
                        {
                            m_Log.WarnFormat("Loading object {0} for {3} ({4}) failed: {2}: {1}\n{5}", grp.ID, e.Message, e.GetType().FullName, loadparams.Scene.Name, loadparams.Scene.ID, e.StackTrace);
                        }
                    }
                }
                if (objects.Count == 1)
                {
                    m_Log.InfoFormat("Loaded {0} object for {1} ({2})", objects.Count, loadparams.Scene.Name, loadparams.Scene.ID);
                }
                else
                {
                    m_Log.InfoFormat("Loaded {0} objects for {1} ({2})", objects.Count, loadparams.Scene.Name, loadparams.Scene.ID);
                }

                m_Log.InfoFormat("Loading terrain for {0} ({1})", loadparams.Scene.Name, loadparams.Scene.ID);
                List<LayerPatch> patches;
                lock (loadparams.Scene.m_LoaderThreadLock)
                {
                    patches = loadparams.SimulationDataStorage.Terrains[loadparams.Scene.ID];
                }

                byte[,] valid = new byte[loadparams.Scene.SizeX / 16, loadparams.Scene.SizeY / 16];

                foreach (LayerPatch p in patches)
                {
                    if (p.X < loadparams.Scene.SizeX / 16 && p.Y < loadparams.Scene.SizeY / 16)
                    {
                        valid[p.X, p.Y] = 1;
                        loadparams.Scene.Terrain.Patch.UpdateWithSerial(p);
                    }
                }

                if (patches.Count == 1)
                {
                    m_Log.InfoFormat("Loaded {0} terrain segment for {1} ({2})", patches.Count, loadparams.Scene.Name, loadparams.Scene.ID);
                }
                else
                {
                    m_Log.InfoFormat("Loaded {0} terrain segments for {1} ({2})", patches.Count, loadparams.Scene.Name, loadparams.Scene.ID);
                }

                /* now we enable storing new data, scripts have to be started after executing this line */
                loadparams.Scene.StartStorage();

                {
                    uint px, py;
                    int count = 0;
                    for (py = 0; py < loadparams.Scene.SizeY / 16; ++py)
                    {
                        for (px = 0; px < loadparams.Scene.SizeX / 16; ++px)
                        {
                            if (valid[px, py] == 0)
                            {
                                LayerPatch p = loadparams.Scene.Terrain.Patch[px, py];
                                loadparams.Scene.Terrain.Patch.Update(p);
                                ++count;
                            }
                        }
                    }
                    if (count == 1)
                    {
                        m_Log.InfoFormat("Stored {0} missing terrain segment for {1} ({2})", count, loadparams.Scene.Name, loadparams.Scene.ID);
                    }
                    else if (count > 0)
                    {
                        m_Log.InfoFormat("Stored {0} missing terrain segments for {1} ({2})", count, loadparams.Scene.Name, loadparams.Scene.ID);
                    }
                }

                loadparams.Scene.UpdateEnvironmentSettings();

                m_Log.InfoFormat("Starting scripts for {0} ({1})", loadparams.Scene.Name, loadparams.Scene.ID);
                int scriptcount = 0;
                foreach(ObjectPart part in loadparams.Scene.Primitives)
                {
                    foreach(ObjectPartInventoryItem item in part.Inventory.Values)
                    {
                        if (item.AssetType == AssetType.LSLText)
                        {
                            AssetData assetData;
                            if (loadparams.Scene.AssetService.TryGetValue(item.AssetID, out assetData))
                            {
                                item.ScriptInstance = ScriptLoader.Load(part, item, item.Owner, assetData);
                                if (++scriptcount % 50 == 0)
                                {
                                    m_Log.InfoFormat("Started {2} scripts for {0} ({1})", loadparams.Scene.Name, loadparams.Scene.ID, scriptcount);
                                }
                            }
                        }
                    }
                }

                if (scriptcount == 1)
                {
                    m_Log.InfoFormat("Started 1 script for {0} ({1})", loadparams.Scene.Name, loadparams.Scene.ID);
                }
                else if(scriptcount % 50 != 0)
                {
                    m_Log.InfoFormat("Started {2} scripts for {0} ({1})", loadparams.Scene.Name, loadparams.Scene.ID, scriptcount);
                }
                m_Log.InfoFormat("All scripts started for {0} ({1})", loadparams.Scene.Name, loadparams.Scene.ID);

                loadparams.Scene.IsKeyframedMotionEnabled = true;

                loadparams.Scene.LoginControl.Ready(SceneInterface.ReadyFlags.SceneObjects);
            }
            finally
            {
                lock (loadparams.Scene.m_LoaderThreadLock)
                {
                    loadparams.Scene.m_LoaderThread = null;
                }
            }
        }
    }
}
