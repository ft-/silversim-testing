/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using log4net;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System;
using System.Reflection;
using ThreadedClasses;

namespace SilverSim.Scene.Management.Scene
{
    public class SceneList : RwLockedDoubleDictionary<UUID, ulong, SceneInterface>
    {
        private static readonly ILog m_Log = LogManager.GetLogger("SCENE MANAGER");
        public event Action<SceneInterface> OnRegionAdd;
        public event Action<SceneInterface> OnRegionRemove;
        private RwLockedBiDiMappingDictionary<UUID, string> m_RegionNames = new RwLockedBiDiMappingDictionary<UUID,string>();

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
                return this[m_RegionNames[name]];
            }
        }

        public void Add(SceneInterface scene)
        {
            m_RegionNames.Add(scene.ID, scene.Name);
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
                    foreach (Action<SceneInterface> del in ev.GetInvocationList())
                    {
                        try
                        {
                            del(scene);
                        }
                        catch (Exception e)
                        {
                            m_Log.DebugFormat("Exception {0}:{1} at {2}", e.GetType().Name, e.Message, e.StackTrace.ToString());
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
            m_Log.InfoFormat("Removing region {0} at {1},{2}", scene.Name, scene.GridPosition.X / 256, scene.GridPosition.Y / 256);
            if (OnRegionRemove != null)
            {
                var ev = OnRegionRemove; /* events are not exactly thread-safe, so copy the reference first */
                if (null != ev)
                {
                    foreach (Action<SceneInterface> del in ev.GetInvocationList())
                    {
                        try
                        {
                            del(scene);
                        }
                        catch (Exception e)
                        {
                            m_Log.DebugFormat("Exception {0}:{1} at {2}", e.GetType().Name, e.Message, e.StackTrace.ToString());
                        }
                    }
                }
            }
            scene.InvokeOnRemove();
            m_RegionNames.Remove(scene.ID);
            base.Remove(scene.ID);
        }
    }
}
