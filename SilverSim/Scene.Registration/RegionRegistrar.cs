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

using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Scene.Registration
{
    [Description("Grid Registration Handler")]
    [PluginName("SceneRegistrar")]
    public class SceneRegistrar : IPlugin
    {
        private readonly RwLockedList<SceneInterface> m_RegisteredScenes = new RwLockedList<SceneInterface>();
        private SceneList m_Scenes;

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            m_Scenes.OnRegionAdd += RegionAdded;
            m_Scenes.OnRegionRemove += RegionRemoved;

            /* register missing scenes */
            foreach(SceneInterface si in m_Scenes.Values)
            {
                if(!m_RegisteredScenes.Contains(si))
                {
                    RegionAdded(si);
                }
            }
        }

        public void RegionAdded(SceneInterface scene)
        {
            m_RegisteredScenes.Add(scene);
            RegionInfo ri = scene.GetRegionInfo();
            if(ri.Owner == null)
            {
                ri.Owner = new UUI();
            }

            Dictionary<string, string> gridFeatures = scene.GridService.GetGridExtraFeatures();
            if (gridFeatures.ContainsKey("GridURL"))
            {
                ri.GridURI = gridFeatures["GridURL"];
            }
            scene.GridService.RegisterRegion(ri);
        }

        public void RegionRemoved(SceneInterface scene)
        {
            scene.GridService.UnregisterRegion(UUID.Zero, scene.ID);
            m_RegisteredScenes.Remove(scene);
        }
    }
}
