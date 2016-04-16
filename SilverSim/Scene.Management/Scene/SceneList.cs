// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;

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
            /* we have to bypass the circular issue we would get when trying to do it via using */
            Assembly assembly = Assembly.Load("SilverSim.Main.Common");
            Type t = assembly.GetType("SilverSim.Main.Common.ConfigurationLoader");
            FieldInfo f = t.GetField("SimulatorShutdownDelegate", BindingFlags.NonPublic | BindingFlags.Static);
            Action<int> act = HandleSimulatorShutdownInNotice;
            f.SetValue(null, act);
            f = t.GetField("SimulatorShutdownAbortDelegate", BindingFlags.NonPublic | BindingFlags.Static);
            Action act2 = HandleSimulatorShutdownAbortNotice;
            f.SetValue(null, act2);
        }

        void HandleSimulatorShutdownInNotice(int timeLeft)
        {
            foreach (SceneInterface scene in Values)
            {
                foreach (IAgent agent in scene.RootAgents)
                {
                    agent.SendRegionNotice(
                        agent.Owner,
                        string.Format(this.GetLanguageString(agent.CurrentCulture, "SimulatorIsShuttingDownInXSeconds", "Simulator is shutting down in {0} seconds."),
                        timeLeft), scene.ID);
                }
            }
        }

        void HandleSimulatorShutdownAbortNotice()
        {
            foreach (SceneInterface scene in Values)
            {
                foreach (IAgent agent in scene.RootAgents)
                {
                    agent.SendRegionNotice(
                        agent.Owner,
                        this.GetLanguageString(agent.CurrentCulture, "SimulatorShutdownIsAborted", "Simulator shutdown is aborted."), scene.ID);
                }
            }
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
        public bool TryGetValue(string name, out SceneInterface scene)
        {
            try
            {
                scene = ((RwLockedDoubleDictionary<UUID, ulong, SceneInterface>)this)[m_RegionNames[name.ToLower()]];
                return true;
            }
            catch
            {
                scene = default(SceneInterface);
                return false;
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
                    foreach (Action<SceneInterface> del in ev.GetInvocationList().OfType<Action<SceneInterface>>())
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

        public void Remove(SceneInterface scene)
        {
            Remove(scene, delegate (CultureInfo culture) 
            {
                return this.GetLanguageString(culture, "RegionIsShuttingDown", "Region is shutting down");
            });
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public void Remove(SceneInterface scene, Func<CultureInfo, string> GetLocalizedOutput)
        {
            scene.LoginControl.NotReady(SceneInterface.ReadyFlags.Remove);
            m_Log.InfoFormat("Removing region {0} at {1},{2}", scene.Name, scene.GridPosition.X / 256, scene.GridPosition.Y / 256);
            if (OnRegionRemove != null)
            {
                var ev = OnRegionRemove; /* events are not exactly thread-safe, so copy the reference first */
                if (null != ev)
                {
                    foreach (Action<SceneInterface> del in ev.GetInvocationList().OfType<Action<SceneInterface>>())
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

            List<IAgent> agentsToLogout = new List<IAgent>(scene.RootAgents);
            int agentCount = agentsToLogout.Count;

            if (agentCount > 0)
            {
                using (Semaphore waitSema = new Semaphore(0, agentCount))
                {
                    foreach (IAgent agent in agentsToLogout)
                    {
                        agent.KickUser(GetLocalizedOutput(agent.CurrentCulture), delegate (bool v)
                        {
                            try
                            {
                                waitSema.Release(1);
                            }
                            catch (ObjectDisposedException)
                            {
                                /* ignore this specific error, we might have disposed it before getting to this call */
                            }
                        });
                    }
                    int count = 0;
                    while (count < agentCount)
                    {
                        try
                        {
                            waitSema.WaitOne(11000);
                        }
                        catch
                        {
                            m_Log.InfoFormat("Remaining agents are forced to be disconnected. Count: {0}", agentCount - count);
                            break;
                        }
                        ++count;
                    }
                }
            }
            /* if there are still agents left, we kill their connections here. */

            scene.InvokeOnRemove();
            scene.PhysicsScene.Shutdown();
            m_RegionNames.Remove(scene.ID);
            base.Remove(scene.ID);
        }
    }
}
