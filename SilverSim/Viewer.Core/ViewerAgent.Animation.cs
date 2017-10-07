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

using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Agent;
using SilverSim.Viewer.Messages.Avatar;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        protected override void SendAnimations(AvatarAnimation m)
        {
            foreach (AgentCircuit c in Circuits.Values)
            {
                c.Scene.SendAgentAnimToAllAgents(m);
            }
        }

        [PacketHandler(MessageType.AgentAnimation)]
        public void HandleAgentAnimation(Message m)
        {
            var req = (AgentAnimation)m;
            SceneInterface scene;
            AgentCircuit circuit;

            if(!Circuits.TryGetValue(m.CircuitSceneID, out circuit))
            {
                return;
            }

            scene = circuit.Scene;
            if(scene == null)
            {
                return;
            }
            AssetServiceInterface sceneAssetService = scene.AssetService;
            if(sceneAssetService == null)
            {
                return;
            }

            AssetMetadata metadata;

            foreach(var e in req.AnimationEntryList)
            {
                if(e.StartAnim)
                {
                    if(!sceneAssetService.Metadata.TryGetValue(e.AnimID, out metadata))
                    {
                        AssetData data;
                        if(AssetService.TryGetValue(e.AnimID, out data))
                        {
                            sceneAssetService.Store(data);
                            if (data.Type != AssetType.Animation)
                            {
                                /* ignore non-animation content here */
                                continue;
                            }
                        }
                        else
                        {
                            /* asset not there so ignore */
                            continue;
                        }
                    }
                    else if(metadata.Type != AssetType.Animation)
                    {
                        /* ignore non-animation content here */
                        continue;
                    }
                    PlayAnimation(e.AnimID, UUID.Zero);
                }
                else
                {
                    StopAnimation(e.AnimID, UUID.Zero);
                }
            }
        }
    }
}
