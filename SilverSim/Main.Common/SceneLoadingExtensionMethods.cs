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

        public static void LoadSceneThread(object o)
        {
            SceneLoadingParams loadparams = (SceneLoadingParams)o;
            Thread.CurrentThread.Name = "Scene Loading Thread for " + loadparams.Scene.Name + " (" + loadparams.Scene.ID.ToString() + ")";
            List<UUID> parcels;
            try
            {
                lock (loadparams.Scene.m_LoaderThreadLock)
                {
                    try
                    {
                        loadparams.Scene.EnvironmentSettings = loadparams.SimulationDataStorage.EnvironmentSettings[loadparams.Scene.ID];
                    }
                    catch
                    {
                        loadparams.Scene.EnvironmentSettings = null;
                    }
                }

                lock (loadparams.Scene.m_LoaderThreadLock)
                {
                    parcels = loadparams.SimulationDataStorage.Parcels.ParcelsInRegion(loadparams.Scene.ID);
                }
                if (parcels.Count == 1)
                {
                    m_Log.InfoFormat("Loading {0} parcel for {1} ({2})", parcels.Count, loadparams.Scene.RegionData.Name, loadparams.Scene.ID);
                }
                else
                {
                    m_Log.InfoFormat("Loading {0} parcels for {1} ({2})", parcels.Count, loadparams.Scene.RegionData.Name, loadparams.Scene.ID);
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
                                loadparams.Scene.AddParcel(pi);
                            }
                        }
                        catch (Exception e)
                        {
                            m_Log.WarnFormat("Loading parcel {0} for {3} ({4}) failed: {2}: {1}", parcelid, e.Message, e.GetType().FullName, loadparams.Scene.RegionData.Name, loadparams.Scene.ID);
                        }

                    }
                }

                if (parcels.Count == 0)
                {
                    ParcelInfo pi = new ParcelInfo((int)loadparams.Scene.RegionData.Size.X / 4, (int)loadparams.Scene.RegionData.Size.Y / 4);
                    pi.AABBMin = new Vector3(0, 0, 0);
                    pi.AABBMax = new Vector3(loadparams.Scene.RegionData.Size.X - 1, loadparams.Scene.RegionData.Size.Y - 1, 0);
                    pi.ActualArea = (int)(loadparams.Scene.RegionData.Size.X * loadparams.Scene.RegionData.Size.Y);
                    pi.Area = (int)(loadparams.Scene.RegionData.Size.X * loadparams.Scene.RegionData.Size.Y);
                    pi.AuctionID = 0;
                    pi.LocalID = 1;
                    pi.ID = UUID.Random;
                    pi.Name = "Your Parcel";
                    pi.Owner = loadparams.Scene.RegionData.Owner;
                    pi.Flags = ParcelFlags.None; /* we keep all flags disabled initially */
                    pi.BillableArea = (int)(loadparams.Scene.RegionData.Size.X * loadparams.Scene.RegionData.Size.Y);
                    pi.LandBitmap.SetAllBits();
                    pi.LandingPosition = new Vector3(128, 128, 23);
                    pi.LandingLookAt = new Vector3(1, 0, 0);
                    pi.ClaimDate = new Date();
                    pi.Status = ParcelStatus.Leased;
                    loadparams.SimulationDataStorage.Parcels.Store(loadparams.Scene.ID, pi);
                    loadparams.Scene.AddParcel(pi);
                    m_Log.InfoFormat("Auto-generated default parcel for {1} ({2})", parcels.Count, loadparams.Scene.RegionData.Name, loadparams.Scene.ID);
                }
                else if (parcels.Count == 1)
                {
                    m_Log.InfoFormat("Loaded {0} parcel for {1} ({2})", parcels.Count, loadparams.Scene.RegionData.Name, loadparams.Scene.ID);
                }
                else
                {
                    m_Log.InfoFormat("Loaded {0} parcels for {1} ({2})", parcels.Count, loadparams.Scene.RegionData.Name, loadparams.Scene.ID);
                }

                List<UUID> objects;
                lock (loadparams.Scene.m_LoaderThreadLock)
                {
                    objects = loadparams.SimulationDataStorage.Objects.ObjectsInRegion(loadparams.Scene.ID);
                }
                if (objects.Count == 1)
                {
                    m_Log.InfoFormat("Loading {0} object for {1} ({2})", objects.Count, loadparams.Scene.RegionData.Name, loadparams.Scene.ID);
                }
                else
                {
                    m_Log.InfoFormat("Loading {0} objects for {1} ({2})", objects.Count, loadparams.Scene.RegionData.Name, loadparams.Scene.ID);
                }
                if (objects.Count != 0)
                {
                    List<ObjectGroup> objGroups = loadparams.SimulationDataStorage.Objects[loadparams.Scene.ID];
                    m_Log.InfoFormat("Adding objects to {0} ({1})", loadparams.Scene.RegionData.Name, loadparams.Scene.ID);
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
                            m_Log.WarnFormat("Loading object {0} for {3} ({4}) failed: {2}: {1}\n{5}", grp.ID, e.Message, e.GetType().FullName, loadparams.Scene.RegionData.Name, loadparams.Scene.ID, e.StackTrace);
                        }
                    }
                }
                if (objects.Count == 1)
                {
                    m_Log.InfoFormat("Loaded {0} object for {1} ({2})", objects.Count, loadparams.Scene.RegionData.Name, loadparams.Scene.ID);
                }
                else
                {
                    m_Log.InfoFormat("Loaded {0} objects for {1} ({2})", objects.Count, loadparams.Scene.RegionData.Name, loadparams.Scene.ID);
                }

                m_Log.InfoFormat("Loading terrain for {0} ({1})", loadparams.Scene.RegionData.Name, loadparams.Scene.ID);
                List<LayerPatch> patches;
                lock (loadparams.Scene.m_LoaderThreadLock)
                {
                    patches = loadparams.SimulationDataStorage.Terrains[loadparams.Scene.ID];
                }

                byte[,] valid = new byte[loadparams.Scene.RegionData.Size.X / 16, loadparams.Scene.RegionData.Size.Y / 16];

                foreach (LayerPatch p in patches)
                {
                    if (p.X < loadparams.Scene.RegionData.Size.X / 16 && p.Y < loadparams.Scene.RegionData.Size.Y / 16)
                    {
                        valid[p.X, p.Y] = 1;
                        loadparams.Scene.Terrain.Patch.UpdateWithSerial(p);
                    }
                }

                if (patches.Count == 1)
                {
                    m_Log.InfoFormat("Loaded {0} terrain segment for {1} ({2})", patches.Count, loadparams.Scene.RegionData.Name, loadparams.Scene.ID);
                }
                else
                {
                    m_Log.InfoFormat("Loaded {0} terrain segments for {1} ({2})", patches.Count, loadparams.Scene.RegionData.Name, loadparams.Scene.ID);
                }

                {
                    uint px, py;
                    int count = 0;
                    for (py = 0; py < loadparams.Scene.RegionData.Size.Y / 16; ++py)
                    {
                        for (px = 0; px < loadparams.Scene.RegionData.Size.X / 16; ++px)
                        {
                            if (valid[px, py] == 0)
                            {
                                LayerPatch p = loadparams.Scene.Terrain.Patch[px, py];
                                loadparams.SimulationDataStorage.Terrains[loadparams.Scene.ID, p.ExtendedPatchID] = p;
                                ++count;
                            }
                        }
                    }
                    if (count == 1)
                    {
                        m_Log.InfoFormat("Stored {0} missing terrain segment for {1} ({2})", count, loadparams.Scene.RegionData.Name, loadparams.Scene.ID);
                    }
                    else if (count > 0)
                    {
                        m_Log.InfoFormat("Stored {0} missing terrain segments for {1} ({2})", count, loadparams.Scene.RegionData.Name, loadparams.Scene.ID);
                    }
                }

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
