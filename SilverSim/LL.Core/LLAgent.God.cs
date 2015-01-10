﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.LL;
using SilverSim.Scene.Types;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Management.Scene;

namespace SilverSim.LL.Core
{
    public partial class LLAgent
    {
        public void HandleRequestGodlikePowers(Messages.God.RequestGodlikePowers m)
        {
            if(m.AgentID != ID || m.SessionID != m.CircuitSessionID)
            {
                return;
            }

            if(m.IsGodlike && !m_IsActiveGod)
            {
                /* request god powers */
                if(CheckForGodPowers(m.CircuitSceneID, ID))
                {
                    Messages.God.GrantGodlikePowers r = new Messages.God.GrantGodlikePowers();
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
                Messages.God.GrantGodlikePowers r = new Messages.God.GrantGodlikePowers();
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
            if(sceneID != m_CurrentSceneID)
            {
                return false;
            }

            Scene.Types.Scene.SceneInterface scene;
            if(!SceneManager.Scenes.TryGetValue(sceneID, out scene))
            {
                return false;
            }

            return scene.IsPossibleGod(new UUI(ID, FirstName, LastName, HomeURI));
        }
    }
}
