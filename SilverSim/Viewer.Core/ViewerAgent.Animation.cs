// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreadedClasses;
using SilverSim.Types;
using SilverSim.Viewer.Messages.Avatar;
using SilverSim.Viewer.Messages.Agent;
using SilverSim.Scene.Types.Agent;
using SilverSim.Viewer.Messages;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        void SendAnimations(AvatarAnimation m)
        {
            Circuits.ForEach(delegate(AgentCircuit c)
            {
                c.Scene.SendAgentAnimToAllAgents(m);
            });
        }

        void SendAnimations()
        {
            m_AnimationController.SendAnimations();
        }

        public AgentAnimationController m_AnimationController;

        void InitAnimations()
        {
            m_AnimationController = new AgentAnimationController(ID, SendAnimations);
        }

        #region Server-Side Animation Override
        public void ResetAnimationOverride(string anim_state)
        {
            m_AnimationController.ResetAnimationOverride(anim_state);
        }

        public void SetAnimationOverride(string anim_state, UUID anim_id)
        {
            m_AnimationController.SetAnimationOverride(anim_state, anim_id);
        }

        public string GetAnimationOverride(string anim_state)
        {
            return m_AnimationController.GetAnimationOverride(anim_state);
        }
        #endregion

        public void PlayAnimation(UUID animid, UUID objectid)
        {
            m_AnimationController.PlayAnimation(animid, objectid);
        }

        public void StopAnimation(UUID animid, UUID objectid)
        {
            m_AnimationController.StopAnimation(animid, objectid);
        }

        public List<UUID> GetPlayingAnimations()
        {
            return m_AnimationController.GetPlayingAnimations();
        }

        [PacketHandler(MessageType.AgentAnimation)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleAgentAnimation(Message m)
        {
            AgentAnimation req = (AgentAnimation)m;

            foreach(AgentAnimation.AnimationEntry e in req.AnimationEntryList)
            {
                if(e.StartAnim)
                {
                    m_AnimationController.PlayAnimation(e.AnimID, UUID.Zero);
                }
                else
                {
                    m_AnimationController.StopAnimation(e.AnimID, UUID.Zero);
                }
            }
        }
    }
}
