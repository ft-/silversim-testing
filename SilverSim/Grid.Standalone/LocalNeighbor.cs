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
using SilverSim.ServiceInterfaces.Neighbor;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System.ComponentModel;

namespace SilverSim.Grid.Standalone
{
    [Description("Local Neighbor Connector")]
    [PluginName("LocalNeighbor")]
    public sealed class LocalNeighbor : NeighborServiceInterface, IPlugin
    {
        private readonly RwLockedDictionary<UUID, NeighborList> m_NeighborLists = new RwLockedDictionary<UUID, NeighborList>();

        private SceneList m_Scenes;

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
        }

        public override void NotifyNeighborStatus(RegionInfo fromRegion)
        {
            SceneInterface fromScene;
            if (m_Scenes.TryGetValue(fromRegion.ID, out fromScene))
            {
                if ((fromRegion.Flags & RegionFlags.RegionOnline) != 0)
                {
                    m_NeighborLists.GetOrAddIfNotExists(fromRegion.ID, () => new NeighborList());
                    foreach (RegionInfo neighbor in NeighborRequester.GetNeighbors(fromScene.GridService, fromRegion))
                    {
                        SceneInterface scene;
                        if (m_Scenes.TryGetValue(neighbor.ID, out scene))
                        {
                            scene.NotifyNeighborOnline(fromRegion);
                        }
                    }
                }
                else
                {
                    NeighborList list;
                    if(m_NeighborLists.Remove(fromRegion.ID, out list))
                    {
                        foreach(UUID id in list.Keys)
                        {
                            SceneInterface scene;
                            if(m_Scenes.TryGetValue(id, out scene))
                            {
                                scene.NotifyNeighborOffline(fromRegion);
                            }
                        }
                    }
                }
            }
        }
    }
}
