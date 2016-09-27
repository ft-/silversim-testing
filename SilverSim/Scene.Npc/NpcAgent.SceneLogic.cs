// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Scene;
using SilverSim.Viewer.Messages.Avatar;

namespace SilverSim.Scene.Npc
{
    public partial class NpcAgent
    {
        SceneInterface m_CurrentScene;

        public override bool IsInScene(SceneInterface scene)
        {
            SceneInterface currentScene = CurrentScene;
            if(null == currentScene)
            {
                return false;
            }
            return (scene.ID == currentScene.ID);
        }

        protected override void SendAnimations(AvatarAnimation m)
        {
            SceneInterface scene = CurrentScene;
            if(null != scene)
            {
                scene.SendAgentAnimToAllAgents(m);
            }
        }

        internal SceneInterface CurrentScene
        {
            get
            {
                return m_CurrentScene;
            }
            set
            {
                m_CurrentScene = value;
            }
        }
    }
}
