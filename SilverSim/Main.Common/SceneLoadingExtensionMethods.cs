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
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.SceneEnvironment;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scene.Types.WindLight;
using SilverSim.Scripting.Common;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Parcel;
using SilverSim.Viewer.Messages.LayerData;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Main.Common
{
    public static class SceneLoadingExtensionMethods
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LOAD SCENE");

        private struct SceneLoadingParams
        {
            public SceneList Scenes;
            public SceneInterface Scene;
            public SimulationDataStorageInterface SimulationDataStorage;
        }

        public static void LoadScene(this SceneInterface scene, SimulationDataStorageInterface simulationDataStorage, SceneList scenes)
        {
            lock (scene.m_LoaderThreadLock)
            {
                if (scene.m_LoaderThread == null && !scene.IsSceneEnabled)
                {
                    var loadparams = new SceneLoadingParams()
                    {
                        Scenes = scenes,
                        Scene = scene,
                        SimulationDataStorage = simulationDataStorage
                    };
                    scene.m_LoaderThread = ThreadManager.CreateThread(LoadSceneThread);
                    scene.m_LoaderThread.Start(loadparams);
                }
            }
        }

        /** <summary>only for testing code</summary> */
        public static void LoadSceneSync(this SceneInterface scene, SimulationDataStorageInterface simulationDataStorage, SceneList scenes)
        {
            m_Log.Error("Do not use LoadSceneSync in production software");
            lock (scene.m_LoaderThreadLock)
            {
                if (scene.m_LoaderThread == null && !scene.IsSceneEnabled)
                {
                    var loadparams = new SceneLoadingParams()
                    {
                        Scenes = scenes,
                        Scene = scene,
                        SimulationDataStorage = simulationDataStorage
                    };
                    scene.m_LoaderThread = ThreadManager.CreateThread(LoadSceneThread);
                    /* we put a thread in there for ensuring correct sequence but we do not start it */
                    LoadSceneMain(loadparams);
                }
            }
        }

        private static void LoadSceneThread(object o)
        {
            var loadparams = (SceneLoadingParams)o;
            Thread.CurrentThread.Name = "Scene Loading Thread for " + loadparams.Scene.Name + " (" + loadparams.Scene.ID.ToString() + ")";
            LoadSceneMain(loadparams);
        }

        private static void LoadSceneMain(SceneLoadingParams loadparams)
        {
            List<UUID> parcels;
            try
            {
                lock (loadparams.Scene.m_LoaderThreadLock)
                {
                    loadparams.Scene.UpdateRunState(SceneInterface.RunState.Starting, SceneInterface.RunState.None);
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
                    loadparams.Scene.SpawnPoints = loadparams.SimulationDataStorage.Spawnpoints[loadparams.Scene.ID];
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

                lock(loadparams.Scene.m_LoaderThreadLock)
                {
                    byte[] serializedData;
                    if(loadparams.SimulationDataStorage.EnvironmentController.TryGetValue(loadparams.Scene.ID, out serializedData))
                    {
                        loadparams.Scene.Environment.Serialization = serializedData;
                        m_Log.InfoFormat("Loaded environment controller settings for {0} ({1})", loadparams.Scene.Name, loadparams.Scene.ID);
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
                    var pi = new ParcelInfo((int)loadparams.Scene.SizeX / 4, (int)loadparams.Scene.SizeY / 4)
                    {
                        AABBMin = new Vector3(0, 0, 0),
                        AABBMax = new Vector3(loadparams.Scene.SizeX - 1, loadparams.Scene.SizeY - 1, 0),
                        ActualArea = (int)(loadparams.Scene.SizeX * loadparams.Scene.SizeY),
                        Area = (int)(loadparams.Scene.SizeX * loadparams.Scene.SizeY),
                        AuctionID = 0,
                        LocalID = 1,
                        ID = UUID.Random,
                        Name = "Your Parcel",
                        Owner = loadparams.Scene.Owner,
                        Flags = ParcelFlags.None, /* we keep all flags disabled initially */
                        BillableArea = (int)(loadparams.Scene.SizeX * loadparams.Scene.SizeY),
                        LandingType = TeleportLandingType.Anywhere,
                        LandingPosition = new Vector3(128, 128, 23),
                        LandingLookAt = new Vector3(1, 0, 0),
                        ClaimDate = new Date(),
                        Status = ParcelStatus.Leased
                    };
                    pi.LandBitmap.SetAllBits();
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

                var valid = new byte[loadparams.Scene.SizeX / 16, loadparams.Scene.SizeY / 16];

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
                    int count = 0;
                    for (uint py = 0; py < loadparams.Scene.SizeY / 16; ++py)
                    {
                        for (uint px = 0; px < loadparams.Scene.SizeX / 16; ++px)
                        {
                            if (valid[px, py] == 0)
                            {
                                LayerPatch p = loadparams.Scene.Terrain.Patch[px, py];
                                loadparams.Scene.Terrain.Patch.MarkDirty(px, py);
                                ++count;
                            }
                        }
                    }
                    if (count == 1)
                    {
                        m_Log.InfoFormat("Stored {0} missing terrain segment for {1} ({2})", count, loadparams.Scene.Name, loadparams.Scene.ID);
                        loadparams.Scene.Terrain.Flush();
                    }
                    else if (count > 0)
                    {
                        m_Log.InfoFormat("Stored {0} missing terrain segments for {1} ({2})", count, loadparams.Scene.Name, loadparams.Scene.ID);
                        loadparams.Scene.Terrain.Flush();
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
                                byte[] serializedState;
                                try
                                {
                                    if (loadparams.SimulationDataStorage.ScriptStates.TryGetValue(loadparams.Scene.ID, part.ID, item.ID, out serializedState))
                                    {
                                        item.ScriptInstance = ScriptLoader.Load(part, item, item.Owner, assetData, null, serializedState);
                                        item.ScriptInstance.PostEvent(new ChangedEvent(ChangedEvent.ChangedFlags.RegionStart));
                                    }
                                    else
                                    {
                                        item.ScriptInstance = ScriptLoader.Load(part, item, item.Owner, assetData, null);
                                        item.ScriptInstance.IsResetRequired = true;
                                    }

                                    if (item.ScriptInstance.IsResetRequired)
                                    {
                                        item.ScriptInstance.IsResetRequired = false;
                                        item.ScriptInstance.IsRunning = true;
                                        item.ScriptInstance.Reset();
                                    }
                                    if (++scriptcount % 50 == 0)
                                    {
                                        m_Log.InfoFormat("Started {2} scripts for {0} ({1})", loadparams.Scene.Name, loadparams.Scene.ID, scriptcount);
                                    }
                                }
                                catch(Exception e)
                                {
#if DEBUG
                                    m_Log.ErrorFormat("Loading script {0} (asset {1}) for {2} ({3}) in {4} ({5}) failed: {6}: {7}\n{8}", item.Name, item.AssetID, part.Name, part.ID, part.ObjectGroup.Name, part.ObjectGroup.ID, e.GetType().FullName, e.Message, e.StackTrace);
#else
                                    m_Log.ErrorFormat("Loading script {0} (asset {1}) for {2} ({3}) in {4} ({5}) failed: {6}", item.Name, item.AssetID, part.Name, part.ID, part.ObjectGroup.Name, part.ObjectGroup.ID, e.Message);
#endif
                                }
                            }
                            else
                            {
                                m_Log.ErrorFormat("Script {0} (asset {1}) is missing for {2} ({3}) in {4} ({5})", item.Name, item.AssetID, part.Name, part.ID, part.ObjectGroup.Name, part.ObjectGroup.ID);
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
                loadparams.Scene.UpdateRunState(SceneInterface.RunState.Started, SceneInterface.RunState.Starting);
            }
            catch (Exception e)
            {
                m_Log.ErrorFormat("Loading error for {0} ({1}): Exception {2}: {3}\nat {4}",
                    loadparams.Scene.Name,
                    loadparams.Scene.ID,
                    e.GetType().FullName,
                    e.Message,
                    e.StackTrace);
                loadparams.Scenes.Remove(loadparams.Scene);
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
