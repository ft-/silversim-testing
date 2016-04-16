// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.God;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        [PacketHandler(MessageType.RequestGodlikePowers)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleRequestGodlikePowers(Message p)
        {
            RequestGodlikePowers m = (RequestGodlikePowers)p;
            if(m.AgentID != ID || m.SessionID != m.CircuitSessionID)
            {
                return;
            }

            if(m.IsGodlike && !m_IsActiveGod)
            {
                /* request god powers */
                if(CheckForGodPowers(m.CircuitSceneID, ID))
                {
                    GrantGodlikePowers r = new GrantGodlikePowers();
                    r.AgentID = m.AgentID;
                    r.SessionID = m.SessionID;

                    r.GodLevel = 255;
                    r.Token = UUID.Zero;
                    m_IsActiveGod = true;
                    SendMessageIfRootAgent(r, m.CircuitSceneID);
                }
                else
                {
                    Messages.Alert.AlertMessage r = new Messages.Alert.AlertMessage();
                    r.Message = "NOTIFY: GodlikeRequestFailed";
                    SendMessageIfRootAgent(r, m.CircuitSceneID);
                }
            }
            else if(!m.IsGodlike && m_IsActiveGod)
            {
                GrantGodlikePowers r = new GrantGodlikePowers();
                r.AgentID = m.AgentID;
                r.SessionID = m.SessionID;

                r.GodLevel = 0;
                r.Token = UUID.Zero;
                m_IsActiveGod = false;
                SendMessageAlways(r, m.CircuitSceneID);
            }
        }

        private bool CheckForGodPowers(UUID sceneID, UUID agentID)
        {
            if (sceneID != SceneID)
            {
                return false;
            }

            SceneInterface scene;
            if(!m_Scenes.TryGetValue(sceneID, out scene))
            {
                return false;
            }

            return scene.IsPossibleGod(new UUI(ID, FirstName, LastName, HomeURI));
        }
    }
}
