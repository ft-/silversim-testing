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
        public class AnimationStateAttribute : Attribute
        {
            public string Name { get; }
            public string DefaultID { get; }

            public AnimationStateAttribute(string name, string id)
            {
                Name = name;
                DefaultID = id;
            }
        }

        public enum AnimationState
        {
            [AnimationState("standing", "2408fe9e-df1d-1d7d-f4ff-1384fa7b350f")]
            Standing,

            [AnimationState("crouching", "201f3fdf-cb1f-dbec-201f-7333e328ae7c")]
            Crouching,
            [AnimationState("crouchwalking", "47f5f6fb-22e5-ae44-f871-73aaaf4a6022")]
            CrouchWalking,
            [AnimationState("falling down", "666307d9-a860-572d-6fd4-c3ab8865c094")]
            FallingDown,
            [AnimationState("flying", "aec4610c-757f-bc4e-c092-c6e9caf18daf")]
            Flying,
            [AnimationState("flyingslow", "2b5a38b2-5e00-3a97-a495-4c826bc443e6")]
            FlyingSlow,
            [AnimationState("hovering", "4ae8016b-31b9-03bb-c401-b1ea941db41d")]
            Hovering,
            [AnimationState("hovering down", "20f063ea-8306-2562-0b07-5c853b37b31e")]
            HoveringDown,
            [AnimationState("hovering up", "62c5de58-cb33-5743-3d07-9e4cd4352864")]
            HoveringUp,
            [AnimationState("jumping", "2305bd75-1ca9-b03b-1faa-b176b8a8c49e")]
            Jumping,
            [AnimationState("landing", "7a17b059-12b2-41b1-570a-186368b6aa6f")]
            Landing,
            [AnimationState("prejumping", "7a4e87fe-de39-6fcb-6223-024b00893244")]
            Prejumping,
            [AnimationState("running", "05ddbff8-aaa9-92a1-2b74-8fe77a29b445")]
            Running,
            [AnimationState("sitting", "1a5fe8ac-a804-8a5d-7cbd-56bd83184568")]
            Sitting,
            [AnimationState("sitting on ground", "1c7600d6-661f-b87b-efe2-d7421eb93c86")]
            SittingOnGround,
            [AnimationState("standing up", "3da1d753-028a-5446-24f3-9c9b856d9422")]
            StandingUp,
            [AnimationState("striding", "1cb562b0-ba21-2202-efb3-30f82cdf9595")]
            Striding,
            [AnimationState("soft landing", "7a17b059-12b2-41b1-570a-186368b6aa6f")]
            SoftLanding,
            [AnimationState("taking off", "2305bd75-1ca9-b03b-1faa-b176b8a8c49e")]
            TakingOff,
            [AnimationState("turning left", "56e0ba0d-4a9f-7f27-6117-32f2ebbf6135")]
            TurningLeft,
            [AnimationState("turning right", "2d6daa51-3192-6794-8e2e-a15f8338ec30")]
            TurningRight,
            [AnimationState("walking", "6ed24bd8-91aa-4b12-ccc7-c97c857ab4e0")]
            Walking,

            /* Extension for underwater movement */
            [AnimationState("floating", "4ae8016b-31b9-03bb-c401-b1ea941db41d")]
            Floating,
            [AnimationState("swimming", "aec4610c-757f-bc4e-c092-c6e9caf18daf")]
            Swimming,
            [AnimationState("swimmingslow", "2b5a38b2-5e00-3a97-a495-4c826bc443e6")]
            SwimmingSlow,
            [AnimationState("swimming up", "62c5de58-cb33-5743-3d07-9e4cd4352864")]
            SwimmingUp,
            [AnimationState("swimming down", "20f063ea-8306-2562-0b07-5c853b37b31e")]
            SwimmingDown
        }

        private static readonly Dictionary<string, AnimationState> m_StateStringToNumber = new Dictionary<string, AnimationState>();
        private static readonly Dictionary<AnimationState, string> m_StateNumberToString = new Dictionary<AnimationState, string>();
        public static readonly Dictionary<AnimationState, UUID> m_DefaultAnimationOverride = new Dictionary<AnimationState, UUID>();

        static AgentAnimationController()
        {
            Type aType = typeof(AnimationState);
            foreach(AnimationState state in aType.GetEnumValues().OfType<AnimationState>())
            {
                MemberInfo mi = aType.GetMember(state.ToString()).First();
                AnimationStateAttribute nameAttr = mi.GetCustomAttribute(typeof(AnimationStateAttribute)) as AnimationStateAttribute;
                m_DefaultAnimationOverride.Add(state, nameAttr.DefaultID);
                m_StateNumberToString.Add(state, nameAttr.Name);
                m_StateStringToNumber.Add(nameAttr.Name, state);
            }
        }

        private readonly object m_Lock = new object();
        public readonly Dictionary<AnimationState, UUID> m_AnimationOverride = new Dictionary<AnimationState, UUID>();

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

        public string GetAnimationOverride(AnimationState anim_state)
        {
            lock (m_Lock)
            {
                return (string)m_AnimationOverride[anim_state];
            }
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

        public void SetDefaultAnimationString(string anim_state)
        {
            AnimationState selState;
            if (m_StateStringToNumber.TryGetValue(anim_state, out selState))
            {
                SetDefaultAnimation(selState);
            }
            else
            {
                m_Log.ErrorFormat("Unexpected anim_state {0} set for agent {1}.", anim_state, m_AgentID);
            }
        }

        public void SetDefaultAnimation(AnimationState anim_state)
        {
            lock (m_Lock)
            {
                if (m_CurrentDefaultAnimation != anim_state)
                {
                    if (!m_IsSitting)
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
                return m_AnimationOverride[m_IsSitting ? m_CurrentSitDefaultAnimation : m_CurrentDefaultAnimation];
            }
        }

        public AnimationState GetDefaultAnimation()
        {
            lock (m_Lock)
            {
                return m_IsSitting ? m_CurrentSitDefaultAnimation : m_CurrentDefaultAnimation;
            }
        }

        public string GetDefaultAnimationString() => m_StateNumberToString[GetDefaultAnimation()];

        bool m_IsSitting;
        public bool IsSitting => m_IsSitting;
        private AnimationState m_CurrentSitDefaultAnimation;

        private void Sit(AnimationState anim)
        {
            lock (m_Lock)
            {
                if (!m_IsSitting)
                {
                    if (m_CurrentDefaultAnimation != AnimationState.Sitting)
                    {
#if DEBUG
                        m_Log.DebugFormat("Changed default animation to {0} for agent {1}", anim.ToString(), m_AgentID);
#endif
                        ReplaceAnimation(m_AnimationOverride[anim], m_AnimationOverride[m_CurrentDefaultAnimation], UUID.Zero);
                    }
                    m_CurrentSitDefaultAnimation = anim;
                    m_IsSitting = true;
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
                if (m_IsSitting)
                {
                    ReplaceAnimation(m_AnimationOverride[m_CurrentDefaultAnimation], m_AnimationOverride[m_CurrentSitDefaultAnimation], UUID.Zero);
                }
                m_IsSitting = false;
            }
        }
    }
}
