// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Types;
using SilverSim.Types.Script;
using SilverSim.Viewer.Messages.Avatar;
using System.Collections.Generic;

namespace SilverSim.Scene.Agent
{
    partial class Agent
    {
        protected abstract void SendAnimations(AvatarAnimation m);

        protected void SendAnimations()
        {
            m_AnimationController.SendAnimations();
        }

        AgentAnimationController m_AnimationController;

        protected void RevokeAnimPermissions(UUID sourceID, ScriptPermissions permissions)
        {
            m_AnimationController.RevokePermissions(sourceID, permissions);
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

        public string GetDefaultAnimation()
        {
            return m_AnimationController.GetDefaultAnimation();
        }

        public List<UUID> GetPlayingAnimations()
        {
            return m_AnimationController.GetPlayingAnimations();
        }
    }
}
