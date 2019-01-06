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

using SilverSim.Scene.Types.Agent;
using SilverSim.Types;
using SilverSim.Types.Script;
using SilverSim.Viewer.Messages.Avatar;
using System.Collections.Generic;

namespace SilverSim.Scene.Agent
{
    public partial class Agent
    {
        protected abstract void SendAnimations(AvatarAnimation m);

        public void SetDefaultAnimation(AgentAnimationController.AnimationState anim_state) => m_AnimationController.SetDefaultAnimation(anim_state);

        protected void SendAnimations() => m_AnimationController.SendAnimations();

        public AvatarAnimation GetAvatarAnimation() => m_AnimationController.GetAvatarAnimation();

        private readonly AgentAnimationController m_AnimationController;

        protected void RevokeAnimPermissions(UUID sourceID, ScriptPermissions permissions) => m_AnimationController.RevokePermissions(sourceID, permissions);

        #region Server-Side Animation Override
        public void ResetAnimationOverride() => m_AnimationController.ResetAnimationOverride();
        public void ResetAnimationOverride(AgentAnimationController.AnimationState anim_state) => m_AnimationController.ResetAnimationOverride(anim_state);

        public void SetAnimationOverride(AgentAnimationController.AnimationState anim_state, UUID anim_id) => m_AnimationController.SetAnimationOverride(anim_state, anim_id);

        public string GetAnimationOverride(AgentAnimationController.AnimationState anim_state) => m_AnimationController.GetAnimationOverride(anim_state);
        public Dictionary<AgentAnimationController.AnimationState, UUID> GetAnimationOverrides() => m_AnimationController.GetAnimationOverrides();
        #endregion

        public void PlayAnimation(UUID animid, UUID objectid) => m_AnimationController.PlayAnimation(animid, objectid);

        public void StopAnimation(UUID animid, UUID objectid) => m_AnimationController.StopAnimation(animid, objectid);

        public void ReplaceAnimation(UUID animid, UUID oldanimid, UUID objectid) => m_AnimationController.ReplaceAnimation(animid, oldanimid, objectid);

        public void StopAllAnimations(UUID sourceid) => m_AnimationController.StopAllAnimations(sourceid);

        public void BeginSitAnimation() => m_AnimationController.Sit();

        public void BeginSitOnGroundAnimation() => m_AnimationController.SitOnGround();

        public void EndSitAnimation() => m_AnimationController.UnSit();

        public AgentAnimationController.AnimationState GetDefaultAnimation() => m_AnimationController.GetDefaultAnimation();

        public UUID GetDefaultAnimationID() => m_AnimationController.GetDefaultAnimationID();

        public List<UUID> GetPlayingAnimations() => m_AnimationController.GetPlayingAnimations();
    }
}
