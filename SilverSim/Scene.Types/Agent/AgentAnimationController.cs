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

using log4net;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Script;
using SilverSim.Viewer.Messages.Avatar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SilverSim.Scene.Types.Agent
{
    public class AgentAnimationController
    {
        private static readonly ILog m_Log = LogManager.GetLogger("AGENT ANIMATION");

        [Serializable]
        public class DefaultAnimationAttribute : Attribute
        {
            public string DefaultID { get; }

            public DefaultAnimationAttribute(string id)
            {
                DefaultID = id;
            }
        }

        public enum AnimationState
        {
            [DefaultAnimation("2408fe9e-df1d-1d7d-f4ff-1384fa7b350f")]
            Standing,

            [DefaultAnimation("201f3fdf-cb1f-dbec-201f-7333e328ae7c")]
            Crouching,
            [DefaultAnimation("47f5f6fb-22e5-ae44-f871-73aaaf4a6022")]
            CrouchWalking,
            [DefaultAnimation("666307d9-a860-572d-6fd4-c3ab8865c094")]
            FallingDown,
            [DefaultAnimation("aec4610c-757f-bc4e-c092-c6e9caf18daf")]
            Flying,
            [DefaultAnimation("2b5a38b2-5e00-3a97-a495-4c826bc443e6")]
            FlyingSlow,
            [DefaultAnimation("4ae8016b-31b9-03bb-c401-b1ea941db41d")]
            Hovering,
            [DefaultAnimation("20f063ea-8306-2562-0b07-5c853b37b31e")]
            HoveringDown,
            [DefaultAnimation("62c5de58-cb33-5743-3d07-9e4cd4352864")]
            HoveringUp,
            [DefaultAnimation("2305bd75-1ca9-b03b-1faa-b176b8a8c49e")]
            Jumping,
            [DefaultAnimation("7a17b059-12b2-41b1-570a-186368b6aa6f")]
            Landing,
            [DefaultAnimation("7a4e87fe-de39-6fcb-6223-024b00893244")]
            Prejumping,
            [DefaultAnimation("05ddbff8-aaa9-92a1-2b74-8fe77a29b445")]
            Running,
            [DefaultAnimation("1a5fe8ac-a804-8a5d-7cbd-56bd83184568")]
            Sitting,
            [DefaultAnimation("1c7600d6-661f-b87b-efe2-d7421eb93c86")]
            SittingOnGround,
            [DefaultAnimation("3da1d753-028a-5446-24f3-9c9b856d9422")]
            StandingUp,
            [DefaultAnimation("1cb562b0-ba21-2202-efb3-30f82cdf9595")]
            Striding,
            [DefaultAnimation("7a17b059-12b2-41b1-570a-186368b6aa6f")]
            SoftLanding,
            [DefaultAnimation("2305bd75-1ca9-b03b-1faa-b176b8a8c49e")]
            TakingOff,
            [DefaultAnimation("56e0ba0d-4a9f-7f27-6117-32f2ebbf6135")]
            TurningLeft,
            [DefaultAnimation("2d6daa51-3192-6794-8e2e-a15f8338ec30")]
            TurningRight,
            [DefaultAnimation("6ed24bd8-91aa-4b12-ccc7-c97c857ab4e0")]
            Walking,

            /* Extension for underwater movement */
            [DefaultAnimation("4ae8016b-31b9-03bb-c401-b1ea941db41d")]
            Floating,
            [DefaultAnimation("aec4610c-757f-bc4e-c092-c6e9caf18daf")]
            Swimming,
            [DefaultAnimation("2b5a38b2-5e00-3a97-a495-4c826bc443e6")]
            SwimmingSlow,
            [DefaultAnimation("62c5de58-cb33-5743-3d07-9e4cd4352864")]
            SwimmingUp,
            [DefaultAnimation("20f063ea-8306-2562-0b07-5c853b37b31e")]
            SwimmingDown
        }

        private static readonly Dictionary<AnimationState, UUID> m_DefaultAnimationOverride = new Dictionary<AnimationState, UUID>();

        static AgentAnimationController()
        {
            Type aType = typeof(AnimationState);
            foreach(AnimationState state in aType.GetEnumValues().OfType<AnimationState>())
            {
                MemberInfo mi = aType.GetMember(state.ToString()).First();
                DefaultAnimationAttribute nameAttr = mi.GetCustomAttribute(typeof(DefaultAnimationAttribute)) as DefaultAnimationAttribute;
                m_DefaultAnimationOverride.Add(state, nameAttr.DefaultID);
            }
        }

        private readonly object m_Lock = new object();
        private readonly Dictionary<AnimationState, UUID> m_AnimationOverride = new Dictionary<AnimationState, UUID>();

        private AnimationState m_CurrentDefaultAnimation = AnimationState.Standing;
        private uint m_NextAnimSeqNumber;
        private struct AnimationInfo
        {
            public UUID AnimID;
            public UUID SourceID;
            public uint AnimSeq;

            public AnimationInfo(UUID animid, uint animseq, UUID sourceid)
            {
                AnimID = animid;
                AnimSeq = animseq;
                SourceID = sourceid;
            }
        }

        private readonly List<AnimationInfo> m_ActiveAnimations = new List<AnimationInfo>();
        private readonly UUID m_AgentID;
        private readonly IAgent m_Agent;
        private readonly Action<AvatarAnimation> m_SendAnimations;

        public AgentAnimationController(IAgent agent, Action<AvatarAnimation> del)
        {
            m_Agent = agent;
            m_AgentID = agent.ID;
            m_SendAnimations = del;
            foreach (KeyValuePair<AnimationState, UUID> kvp in m_DefaultAnimationOverride)
            {
                m_AnimationOverride[kvp.Key] = kvp.Value;
            }
            m_ActiveAnimations.Add(new AnimationInfo(m_AnimationOverride[AnimationState.Standing], 1, UUID.Zero));
            m_NextAnimSeqNumber = 2;
        }

        public void SendAnimations()
        {
            m_SendAnimations(GetAvatarAnimation());
        }

        public AvatarAnimation GetAvatarAnimation()
        {
            var m = new AvatarAnimation
            {
                Sender = m_AgentID
            };
            lock (m_Lock)
            {
                foreach (var ai in m_ActiveAnimations)
                {
                    m.AnimationList.Add(new AvatarAnimation.AnimationData(ai.AnimID, ai.AnimSeq, ai.SourceID));
                }
            }

            return m;
        }

        public void RevokePermissions(UUID sourceID, ScriptPermissions permissions)
        {
            int i;
            if ((permissions & ScriptPermissions.TriggerAnimation) != 0)
            {
                lock (m_Lock)
                {
                    i = 0;
                    while (i < m_ActiveAnimations.Count)
                    {
                        if (m_ActiveAnimations[i].SourceID == sourceID)
                        {
                            m_ActiveAnimations.RemoveAt(i);
                        }
                        else
                        {
                            ++i;
                        }
                    }
                }
            }
        }

        public void ResetAnimationOverride()
        {
            lock (m_Lock)
            {
                if (m_AnimationOverride[m_CurrentDefaultAnimation] != m_DefaultAnimationOverride[m_CurrentDefaultAnimation])
                {
                    StopAnimation(m_AnimationOverride[m_CurrentDefaultAnimation], UUID.Zero);
                }
                foreach (KeyValuePair<AnimationState, UUID> kvp in m_DefaultAnimationOverride)
                {
                    m_AnimationOverride[kvp.Key] = kvp.Value;
                }
                if (m_AnimationOverride[m_CurrentDefaultAnimation] != m_DefaultAnimationOverride[m_CurrentDefaultAnimation])
                {
                    PlayAnimation(m_AnimationOverride[m_CurrentDefaultAnimation], UUID.Zero);
                }
            }
        }

        public void ResetAnimationOverride(AnimationState anim_state)
        {
            lock (m_Lock)
            {
                if (m_CurrentDefaultAnimation == anim_state)
                {
                    ReplaceAnimation(m_DefaultAnimationOverride[anim_state], m_AnimationOverride[anim_state], UUID.Zero);
                }
                m_AnimationOverride[anim_state] = m_DefaultAnimationOverride[anim_state];
            }
        }

        public void SetAnimationOverride(AnimationState anim_state, UUID anim_id)
        {
            lock (m_Lock)
            {
                if(anim_state == m_CurrentDefaultAnimation)
                {
                    ReplaceAnimation(anim_id, m_AnimationOverride[anim_state], UUID.Zero);
                }
                m_AnimationOverride[anim_state] = anim_id;
            }
        }

        public UUID GetAnimationOverride(AnimationState anim_state)
        {
            lock (m_Lock)
            {
                return m_AnimationOverride[anim_state];
            }
        }

        public Dictionary<AnimationState, UUID> GetAnimationOverrides()
        {
            var result = new Dictionary<AnimationState, UUID>();
            lock(m_Lock)
            {
                foreach(KeyValuePair<AnimationState, UUID> kvp in m_AnimationOverride)
                {
                    result.Add(kvp.Key, kvp.Value);
                }
            }
            return result;
        }

        public void PlayAnimation(UUID animid, UUID objectid)
        {
            if(objectid == UUID.Zero)
            {
                objectid = m_AgentID;
            }

            lock (m_Lock)
            {
                for (int i = 0; i < m_ActiveAnimations.Count; ++i)
                {
                    if (m_ActiveAnimations[i].AnimID == animid)
                    {
                        return;
                    }
                }
                ++m_NextAnimSeqNumber;
                m_ActiveAnimations.Add(new AnimationInfo(animid, m_NextAnimSeqNumber, objectid));
            }
            m_Agent.PostEvent(new ChangedEvent(ChangedEvent.ChangedFlags.Animation));
            SendAnimations();
        }

        public void StopAnimation(UUID animid, UUID objectid)
        {
            bool found = false;
            lock (m_Lock)
            {
                for (int i = 0; i < m_ActiveAnimations.Count; ++i)
                {
                    if (m_ActiveAnimations[i].AnimID == animid)
                    {
                        m_ActiveAnimations.RemoveAt(i);
                        found = true;
                        break;
                    }
                }
            }
            if (found)
            {
                m_Agent.PostEvent(new ChangedEvent(ChangedEvent.ChangedFlags.Animation));
            }
            SendAnimations();
        }

        public void ReplaceAnimation(UUID animid, UUID oldanimid, UUID objectid)
        {
            if (objectid == UUID.Zero)
            {
                objectid = m_AgentID;
            }

            lock (m_Lock)
            {
                for (int i = 0; i < m_ActiveAnimations.Count; ++i)
                {
                    if (m_ActiveAnimations[i].AnimID == oldanimid)
                    {
                        m_ActiveAnimations.RemoveAt(i);
                        break;
                    }
                }
                ++m_NextAnimSeqNumber;
                m_ActiveAnimations.Add(new AnimationInfo(animid, m_NextAnimSeqNumber, objectid));
            }
            m_Agent.PostEvent(new ChangedEvent(ChangedEvent.ChangedFlags.Animation));
            SendAnimations();
        }

        public void StopAllAnimations(UUID sourceid)
        {
            lock (m_Lock)
            {
                for (int i = 0; i < m_ActiveAnimations.Count; ++i)
                {
                    if (m_ActiveAnimations[i].SourceID == sourceid)
                    {
                        m_ActiveAnimations.RemoveAt(i);
                        break;
                    }
                }
            }
            m_Agent.PostEvent(new ChangedEvent(ChangedEvent.ChangedFlags.Animation));
            SendAnimations();
        }

        public List<UUID> GetPlayingAnimations()
        {
            var res = new List<UUID>();
            lock(m_Lock)
            {
                foreach(var info in m_ActiveAnimations)
                {
                    res.Add(info.AnimID);
                }
            }
            return res;
        }

        public void SetDefaultAnimation(AnimationState anim_state)
        {
            lock (m_Lock)
            {
                if (m_CurrentDefaultAnimation != anim_state)
                {
                    if (!IsSitting)
                    {
#if DEBUG
                        m_Log.DebugFormat("Changed default animation to {0} for agent {1}", anim_state.ToString(), m_AgentID);
#endif
                        for (int i = 0; i < m_ActiveAnimations.Count; ++i)
                        {
                            if (m_ActiveAnimations[i].AnimID == m_AnimationOverride[m_CurrentDefaultAnimation])
                            {
                                m_ActiveAnimations.RemoveAt(i);
                                break;
                            }
                        }
                        ++m_NextAnimSeqNumber;
                        m_ActiveAnimations.Add(new AnimationInfo(m_AnimationOverride[anim_state], m_NextAnimSeqNumber, m_AgentID));
                    }
                    m_CurrentDefaultAnimation = anim_state;
                }
            }
            m_Agent.PostEvent(new ChangedEvent(ChangedEvent.ChangedFlags.Animation));
            SendAnimations();
        }

        public UUID GetDefaultAnimationID()
        {
            lock(m_Lock)
            {
                return m_AnimationOverride[IsSitting ? m_CurrentSitDefaultAnimation : m_CurrentDefaultAnimation];
            }
        }

        public AnimationState GetDefaultAnimation()
        {
            lock (m_Lock)
            {
                return IsSitting ? m_CurrentSitDefaultAnimation : m_CurrentDefaultAnimation;
            }
        }

        public bool IsSitting { get; private set; }

        private AnimationState m_CurrentSitDefaultAnimation;

        private void Sit(AnimationState anim)
        {
            lock (m_Lock)
            {
                if (!IsSitting)
                {
                    if (m_CurrentDefaultAnimation != AnimationState.Sitting)
                    {
#if DEBUG
                        m_Log.DebugFormat("Changed default animation to {0} for agent {1}", anim.ToString(), m_AgentID);
#endif
                        ReplaceAnimation(m_AnimationOverride[anim], m_AnimationOverride[m_CurrentDefaultAnimation], UUID.Zero);
                    }
                    m_CurrentSitDefaultAnimation = anim;
                    IsSitting = true;
                }
                else if(m_CurrentSitDefaultAnimation != anim)
                {
#if DEBUG
                    m_Log.DebugFormat("Changed default animation to {0} for agent {1}", anim.ToString(), m_AgentID);
#endif
                    ReplaceAnimation(m_AnimationOverride[anim], m_AnimationOverride[m_CurrentDefaultAnimation], UUID.Zero);
                    m_CurrentSitDefaultAnimation = anim;
                }
            }
        }

        public void Sit() => Sit(AnimationState.Sitting);
        public void SitOnGround() => Sit(AnimationState.SittingOnGround);

        public void UnSit()
        {
            lock(m_Lock)
            {
                if (IsSitting)
                {
                    ReplaceAnimation(m_AnimationOverride[m_CurrentDefaultAnimation], m_AnimationOverride[m_CurrentSitDefaultAnimation], UUID.Zero);
                }
                IsSitting = false;
            }
        }
    }
}
