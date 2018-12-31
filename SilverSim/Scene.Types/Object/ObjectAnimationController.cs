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
using SilverSim.Scene.Types.Object.Parameters;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Viewer.Messages.Object;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Object
{
    public class ObjectAnimationController
    {
        private readonly object m_Lock = new object();

        private uint m_NextAnimSeqNumber;
        private struct AnimationInfo
        {
            public UUID AnimID;
            public uint AnimSeq;

            public AnimationInfo(UUID animid, uint animseq)
            {
                AnimID = animid;
                AnimSeq = animseq;
            }
        }

        private readonly List<AnimationInfo> m_ActiveAnimations = new List<AnimationInfo>();
        private readonly ObjectPart m_Part;

        public ObjectAnimationController(ObjectPart part)
        {
            m_Part = part;
            m_NextAnimSeqNumber = 1;
        }

        private ObjectAnimation GetMessage()
        {
            var m = new ObjectAnimation
            {
                Sender = m_Part.ID
            };
            lock (m_Lock)
            {
                foreach (var ai in m_ActiveAnimations)
                {
                    m.AnimationList.Add(new ObjectAnimation.AnimationData(ai.AnimID, ai.AnimSeq));
                }
            }
            return m;
        }

        public void SendAnimationsToAgent(IAgent agent)
        {
            ObjectGroup grp = m_Part.ObjectGroup;
            if (grp == null)
            {
                return;
            }
            SceneInterface scene = grp.Scene;
            if (scene == null)
            {
                return;
            }

            if ((m_Part.ExtendedMesh.Flags & ExtendedMeshParams.MeshFlags.AnimatedMeshEnabled) == 0)
            {
                return;
            }

            agent.SendMessageAlways(GetMessage(), scene.ID);
        }

        public void SendAnimations()
        {
            ObjectGroup grp = m_Part.ObjectGroup;
            if (grp == null)
            {
                return;
            }
            SceneInterface scene = grp.Scene;
            if(scene == null)
            {
                return;
            }

            if((m_Part.ExtendedMesh.Flags & ExtendedMeshParams.MeshFlags.AnimatedMeshEnabled) == 0)
            {
                return;
            }

            ObjectAnimation m = GetMessage();

            foreach(IAgent agent in scene.Agents)
            {
                agent.SendMessageAlways(m, scene.ID);
            }
        }

        public void PlayAnimation(UUID animid)
        {
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
                m_ActiveAnimations.Add(new AnimationInfo(animid, m_NextAnimSeqNumber));
            }
            SendAnimations();
        }

        public void StopAnimation(UUID animid)
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

        public void ReplaceAnimation(UUID animid, UUID oldanimid)
        {
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
                m_ActiveAnimations.Add(new AnimationInfo(animid, m_NextAnimSeqNumber));
            }
            SendAnimations();
        }

        public List<UUID> GetPlayingAnimations()
        {
            var res = new List<UUID>();
            lock (m_Lock)
            {
                foreach (var info in m_ActiveAnimations)
                {
                    res.Add(info.AnimID);
                }
            }
            return res;
        }

        public Dictionary<UUID, uint> GetActiveAnimationDetails()
        {
            var res = new Dictionary<UUID, uint>();
            lock(m_Lock)
            {
                foreach(var info in m_ActiveAnimations)
                {
                    res.Add(info.AnimID, info.AnimSeq);
                }
            }

            return res;
        }

        public void RestoreAnimation(UUID animid, uint seqid)
        {
            lock (m_Lock)
            {
                for (int i = 0; i < m_ActiveAnimations.Count; ++i)
                {
                    if (m_ActiveAnimations[i].AnimID == animid)
                    {
                        m_ActiveAnimations[i] = new AnimationInfo(animid, seqid);
                        return;
                    }
                }
                m_ActiveAnimations.Add(new AnimationInfo(animid, seqid));
            }
            SendAnimations();
        }

        public uint NextAnimSeqNumber
        {
            get
            {
                return m_NextAnimSeqNumber;
            }
            set
            {
                lock(m_Lock)
                {
                    m_NextAnimSeqNumber = value;
                }
            }
        }

        public byte[] DbSerialization
        {
            get
            {
                lock(m_Lock)
                {
                    byte[] retblock = new byte[m_ActiveAnimations.Count * 20];
                    int offset = 0;
                    foreach (var info in m_ActiveAnimations)
                    {
                        info.AnimID.ToBytes(retblock, offset);
                        offset += 16;
                        Buffer.BlockCopy(BitConverter.GetBytes(info.AnimSeq), 0, retblock, offset, 4);
                        if (!BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(retblock, offset, 4);
                        }
                        offset += 4;
                    }
                    return retblock;
                }
            }
            set
            {
                if(value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if(value.Length % 20 != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                lock(m_Lock)
                {
                    m_ActiveAnimations.Clear();
                    for (int i = 0; i < value.Length; i += 20)
                    {
                        var e = new AnimationInfo
                        {
                            AnimID = new UUID(value, i)
                        };
                        if (!BitConverter.IsLittleEndian)
                        {
                            byte[] res = new byte[4];
                            Buffer.BlockCopy(value, i + 16, res, 0, 4);
                            e.AnimSeq = BitConverter.ToUInt32(res, 0);
                        }
                        else
                        {
                            e.AnimSeq = BitConverter.ToUInt32(value, i + 16);
                        }
                        m_ActiveAnimations.Add(e);
                    }
                }
            }
        }
    }
}
