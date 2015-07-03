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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreadedClasses;
using SilverSim.Types;
using SilverSim.LL.Messages.Avatar;
using SilverSim.LL.Messages.Agent;
using SilverSim.Scene.Types.Agent;
using SilverSim.LL.Messages;

namespace SilverSim.LL.Core
{
    public partial class LLAgent
    {
        void SendAnimations(AvatarAnimation m)
        {
            Circuits.ForEach(delegate(Circuit c)
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

        [PacketHandler(MessageType.AgentAnimation)]
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
