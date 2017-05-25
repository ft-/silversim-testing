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
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;

namespace SilverSim.Scene.Management.Scene
{
    public class SceneList : RwLockedDoubleDictionary<UUID, ulong, SceneInterface>
    {
        private static readonly ILog m_Log = LogManager.GetLogger("SCENE MANAGER");
        public event Action<SceneInterface> OnRegionAdd;
        public event Action<SceneInterface> OnRegionRemove;
        private readonly RwLockedBiDiMappingDictionary<UUID, string> m_RegionNames = new RwLockedBiDiMappingDictionary<UUID, string>();

        public SceneList()
        {
            /* we have to bypass the circular issue we would get when trying to do it via using */
            Assembly assembly = Assembly.Load("SilverSim.Main.Common");
            Type t = assembly.GetType("SilverSim.Main.Common.ConfigurationLoader");
            FieldInfo f = t.GetField("SimulatorShutdownDelegate", BindingFlags.Public | BindingFlags.Static);
            Action<int> act = HandleSimulatorShutdownInNotice;
            f.SetValue(null, act);
            f = t.GetField("SimulatorShutdownAbortDelegate", BindingFlags.Public | BindingFlags.Static);
            Action act2 = HandleSimulatorShutdownAbortNotice;
            f.SetValue(null, act2);
        }

        private void HandleSimulatorShutdownInNotice(int timeLeft)
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

        private void HandleSimulatorShutdownAbortNotice()
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

        public SceneInterface this[GridVector gv] => this[gv.RegionHandle];

        public SceneInterface this[string name] => this[m_RegionNames[name.ToLower()]];

        public bool TryGetValue(string name, out SceneInterface scene)
        {
            try
            {
                scene = this[m_RegionNames[name.ToLower()]];
                return true;
            }
            catch
            {
                scene = default(SceneInterface);
                return false;
            }
        }

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
            foreach (Action<SceneInterface> del in OnRegionAdd?.GetInvocationList() ?? new Delegate[0])
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
            Remove(scene, (CultureInfo culture) => this.GetLanguageString(culture, "RegionIsShuttingDown", "Region is shutting down"));
        }

        public void Remove(SceneInterface scene, Func<CultureInfo, string> GetLocalizedOutput)
        {
            scene.LoginControl.NotReady(SceneInterface.ReadyFlags.Remove);
            m_Log.InfoFormat("Removing region {0} at {1},{2}", scene.Name, scene.GridPosition.X / 256, scene.GridPosition.Y / 256);
            foreach (Action<SceneInterface> del in OnRegionRemove?.GetInvocationList() ?? new Delegate[0])
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

            var agentsToLogout = new List<IAgent>(scene.RootAgents);
            int agentCount = agentsToLogout.Count;

            if (agentCount > 0)
            {
                m_Log.InfoFormat("Ensuring agents logout at region {0} at {1},{2}", scene.Name, scene.GridPosition.X / 256, scene.GridPosition.Y / 256);
                using (var waitSema = new Semaphore(0, agentCount))
                {
                    foreach (IAgent agent in agentsToLogout)
                    {
                        agent.KickUser(GetLocalizedOutput(agent.CurrentCulture), (bool v) =>
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
                    while (count < 3 && agentCount > 0)
                    {
                        try
                        {
                            waitSema.WaitOne(11000);
                        }
                        catch
                        {
                            m_Log.InfoFormat("Remaining agents are forced to be disconnected. Count: {0}", agentCount);
                            break;
                        }
                        ++count;
                    }
                }
                if (agentCount > 0)
                {
                    m_Log.InfoFormat("Dropping remaining agents at region {0} at {1},{2}", scene.Name, scene.GridPosition.X / 256, scene.GridPosition.Y / 256);
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
