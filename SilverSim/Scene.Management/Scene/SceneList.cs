﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System;
using System.Diagnostics.CodeAnalysis;
using ThreadedClasses;

namespace SilverSim.Scene.Management.Scene
{
    public class SceneList : RwLockedDoubleDictionary<UUID, ulong, SceneInterface>
    {
        private static readonly ILog m_Log = LogManager.GetLogger("SCENE MANAGER");
        public event Action<SceneInterface> OnRegionAdd;
        public event Action<SceneInterface> OnRegionRemove;
        readonly RwLockedBiDiMappingDictionary<UUID, string> m_RegionNames = new RwLockedBiDiMappingDictionary<UUID, string>();

        public SceneList()
        {
        }

        public void RemoveAll()
        {
            foreach(UUID id in m_RegionNames.Keys1)
            {
                Remove(base[id]);
            }
        }

        public SceneInterface this[GridVector gv]
        {
            get
            {
                return this[gv.RegionHandle];
            }
        }

        public SceneInterface this[string name]
        {
            get
            {
                return ((RwLockedDoubleDictionary<UUID, ulong, SceneInterface>)this)[m_RegionNames[name.ToLower()]];
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public void Add(SceneInterface scene)
        {
            m_RegionNames.Add(scene.ID, scene.Name.ToLower());
            try
            {
                Add(scene.ID, scene.GridPosition.RegionHandle, scene);
            }
            catch
            {
                m_RegionNames.Remove(scene.ID);
                throw;
            }
            m_Log.InfoFormat("Adding region {0} at {1},{2}", scene.Name, scene.GridPosition.X / 256, scene.GridPosition.Y / 256);
            if (OnRegionAdd != null)
            {
                var ev = OnRegionAdd; /* events are not exactly thread-safe, so copy the reference first */
                if (null != ev)
                {
                    Action<SceneInterface>[] invocationList = (Action<SceneInterface>[])ev.GetInvocationList();
                    foreach (Action<SceneInterface> del in invocationList)
                    {
                        try
                        {
                            del(scene);
                        }
                        catch (Exception e)
                        {
                            m_Log.DebugFormat("Exception {0}:{1} at {2}", e.GetType().Name, e.Message, e.StackTrace);
                        }
                    }
                }
            }
        }

        public new void Remove(UUID key)
        {
            throw new InvalidOperationException();
        }

        public new void Remove(ulong regionHandle)
        {
            throw new InvalidOperationException();
        }

        public new void Remove(UUID key, ulong regionHandle)
        {
            throw new InvalidOperationException();
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public void Remove(SceneInterface scene)
        {
            scene.LoginControl.NotReady(SceneInterface.ReadyFlags.Remove);
            m_Log.InfoFormat("Removing region {0} at {1},{2}", scene.Name, scene.GridPosition.X / 256, scene.GridPosition.Y / 256);
            if (OnRegionRemove != null)
            {
                var ev = OnRegionRemove; /* events are not exactly thread-safe, so copy the reference first */
                if (null != ev)
                {
                    Action<SceneInterface>[] invocationList = (Action<SceneInterface>[])ev.GetInvocationList();
                    foreach (Action<SceneInterface> del in invocationList)
                    {
                        try
                        {
                            del(scene);
                        }
                        catch (Exception e)
                        {
                            m_Log.DebugFormat("Exception {0}:{1} at {2}", e.GetType().Name, e.Message, e.StackTrace);
                        }
                    }
                }
            }
            scene.InvokeOnRemove();
            scene.PhysicsScene.Shutdown();
            m_RegionNames.Remove(scene.ID);
            base.Remove(scene.ID);
        }
    }
}
