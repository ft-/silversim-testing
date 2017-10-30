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
using SilverSim.Types;
using SilverSim.Types.Script;
using SilverSim.Viewer.Messages.Avatar;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SilverSim.Scene.Types.Agent
{
    public class AgentAnimationController
    {
        private static readonly ILog m_Log = LogManager.GetLogger("AGENT ANIMATION");

        private static readonly string[] m_AnimStates = new string[] {
            "crouching",
            "crouchwalking",
            "falling down",
            "flying",
            "flyingslow",
            "hovering",
            "hovering down",
            "hovering up",
            "jumping",
            "landing",
            "prejumping",
            "running",
            "sitting",
            "sitting on ground",
            "standing",
            "standing up",
            "striding",
            "soft landing",
            "taking off",
            "turning left",
            "turning right",
            "walking"
        };

        private readonly object m_Lock = new object();
        public readonly Dictionary<string, UUID> m_AnimationOverride = new Dictionary<string, UUID>();
        public static readonly Dictionary<string, UUID> m_DefaultAnimationOverride = new Dictionary<string, UUID>();

        private string m_CurrentDefaultAnimation = "standing";
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
        private readonly Action<AvatarAnimation> m_SendAnimations;

        public AgentAnimationController(UUID agentID, Action<AvatarAnimation> del)
        {
            m_AgentID = agentID;
            m_SendAnimations = del;
            foreach (string s in m_AnimStates)
            {
                m_AnimationOverride[s] = m_DefaultAnimationOverride[s];
            }
            m_ActiveAnimations.Add(new AnimationInfo(m_AnimationOverride["standing"], 1, UUID.Zero));
            m_NextAnimSeqNumber = 2;
        }

        static AgentAnimationController()
        {
            m_DefaultAnimationOverride["crouching"] = "201f3fdf-cb1f-dbec-201f-7333e328ae7c";
            m_DefaultAnimationOverride["crouchwalking"] = "47f5f6fb-22e5-ae44-f871-73aaaf4a6022";
            m_DefaultAnimationOverride["falling down"] = "666307d9-a860-572d-6fd4-c3ab8865c094";
            m_DefaultAnimationOverride["flying"] = "aec4610c-757f-bc4e-c092-c6e9caf18daf";
            m_DefaultAnimationOverride["flyingslow"] = "2b5a38b2-5e00-3a97-a495-4c826bc443e6";
            m_DefaultAnimationOverride["hovering"] = "4ae8016b-31b9-03bb-c401-b1ea941db41d";
            m_DefaultAnimationOverride["hovering down"] = "20f063ea-8306-2562-0b07-5c853b37b31e";
            m_DefaultAnimationOverride["hovering up"] = "62c5de58-cb33-5743-3d07-9e4cd4352864";
            m_DefaultAnimationOverride["jumping"] = "2305bd75-1ca9-b03b-1faa-b176b8a8c49e";
            m_DefaultAnimationOverride["landing"] = "7a17b059-12b2-41b1-570a-186368b6aa6f";
            m_DefaultAnimationOverride["prejumping"] = "7a4e87fe-de39-6fcb-6223-024b00893244";
            m_DefaultAnimationOverride["running"] = "05ddbff8-aaa9-92a1-2b74-8fe77a29b445";
            m_DefaultAnimationOverride["sitting"] = "1a5fe8ac-a804-8a5d-7cbd-56bd83184568";
            m_DefaultAnimationOverride["sitting on ground"] = "1c7600d6-661f-b87b-efe2-d7421eb93c86";
            m_DefaultAnimationOverride["standing"] = "2408fe9e-df1d-1d7d-f4ff-1384fa7b350f";
            m_DefaultAnimationOverride["standing up"] = "3da1d753-028a-5446-24f3-9c9b856d9422";
            m_DefaultAnimationOverride["striding"] = "1cb562b0-ba21-2202-efb3-30f82cdf9595";
            m_DefaultAnimationOverride["soft landing"] = "7a17b059-12b2-41b1-570a-186368b6aa6f";
            m_DefaultAnimationOverride["taking off"] = "2305bd75-1ca9-b03b-1faa-b176b8a8c49e";
            m_DefaultAnimationOverride["turning left"] = "56e0ba0d-4a9f-7f27-6117-32f2ebbf6135";
            m_DefaultAnimationOverride["turning right"] = "2d6daa51-3192-6794-8e2e-a15f8338ec30";
            m_DefaultAnimationOverride["walking"] = "6ed24bd8-91aa-4b12-ccc7-c97c857ab4e0";
        }

        public void SendAnimations()
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

            m_SendAnimations(m);
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

        public void ResetAnimationOverride(string anim_state)
        {
            if ("ALL" == anim_state)
            {
                lock (m_Lock)
                {
                    if (m_AnimationOverride[m_CurrentDefaultAnimation] != m_DefaultAnimationOverride[m_CurrentDefaultAnimation])
                    {
                        StopAnimation(m_AnimationOverride[m_CurrentDefaultAnimation], UUID.Zero);
                    }
                    foreach (string animstate in m_AnimStates)
                    {
                        m_AnimationOverride[animstate] = m_DefaultAnimationOverride[animstate];
                    }
                    if (m_AnimationOverride[m_CurrentDefaultAnimation] != m_DefaultAnimationOverride[m_CurrentDefaultAnimation])
                    {
                        PlayAnimation(m_AnimationOverride[m_CurrentDefaultAnimation], UUID.Zero);
                    }
                }
            }
            else if (m_AnimStates.Contains(anim_state))
            {
                lock (m_Lock)
                {
                    if (m_CurrentDefaultAnimation == anim_state)
                    {
                        StopAnimation(m_AnimationOverride[anim_state], UUID.Zero);
                    }
                    m_AnimationOverride[anim_state] = m_DefaultAnimationOverride[anim_state];
                    if (m_CurrentDefaultAnimation == anim_state)
                    {
                        PlayAnimation(m_AnimationOverride[anim_state], UUID.Zero);
                    }
                }
            }
        }

        public void SetAnimationOverride(string anim_state, UUID anim_id)
        {
            if (m_AnimStates.Contains(anim_state))
            {
                lock (m_Lock)
                {
                    m_AnimationOverride[anim_state] = anim_id;
                }
            }
        }

        public string GetAnimationOverride(string anim_state)
        {
            if (m_AnimStates.Contains(anim_state))
            {
                lock (m_Lock)
                {
                    return (string)m_AnimationOverride[anim_state];
                }
            }
            return string.Empty;
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
                SendAnimations();
            }
        }

        public void StopAnimation(UUID animid, UUID objectid)
        {
            lock (m_Lock)
            {
                for (int i = 0; i < m_ActiveAnimations.Count; ++i)
                {
                    if (m_ActiveAnimations[i].AnimID == animid)
                    {
                        m_ActiveAnimations.RemoveAt(i);
                        break;
                    }
                }
            }
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

        public void SetDefaultAnimation(string anim_state)
        {
            if (m_AnimStates.Contains(anim_state))
            {
                lock (m_Lock)
                {
                    if (m_CurrentDefaultAnimation != anim_state)
                    {
#if DEBUG
                        m_Log.DebugFormat("Changed default animation to {0} for agent {1}", anim_state, m_AgentID);
#endif
                        StopAnimation(m_AnimationOverride[m_CurrentDefaultAnimation], UUID.Zero);
                        m_CurrentDefaultAnimation = anim_state;
                        PlayAnimation(m_AnimationOverride[anim_state], UUID.Zero);
                    }
                }
            }
            else
            {
                m_Log.ErrorFormat("Unexpected anim_state {0} set for agent {1}.", anim_state, m_AgentID);
            }
        }

        public string GetDefaultAnimation()
        {
            lock (m_Lock)
            {
                return m_CurrentDefaultAnimation;
            }
        }
    }
}
