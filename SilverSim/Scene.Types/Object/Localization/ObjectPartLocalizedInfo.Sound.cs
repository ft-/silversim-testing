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

using SilverSim.Scene.Types.Object.Parameters;
using SilverSim.Types;
using System;

namespace SilverSim.Scene.Types.Object.Localization
{
    public sealed partial class ObjectPartLocalizedInfo
    {
        private SoundParam m_Sound;

        public SoundParam Sound
        {
            get
            {
                SoundParam sp = m_Sound;
                if(sp == null)
                {
                    return m_ParentInfo.Sound;
                }
                else
                {
                    return new SoundParam(sp);
                }
            }
            set
            {
                if(value == null)
                {
                    if (m_ParentInfo == null)
                    {
                        throw new InvalidOperationException();
                    }
                    m_Sound = null;
                }
                else
                {
                    m_Sound = new SoundParam(value);
                }
                lock (m_UpdateDataLock)
                {
                    SoundParam sound = Sound;
                    sound.SoundID.ToBytes(m_FullUpdateFixedBlock2, (int)FullFixedBlock2Offset.LoopedSound);
                    byte[] val = BitConverter.GetBytes((float)sound.Gain);
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(val);
                    }
                    Buffer.BlockCopy(val, 0, m_FullUpdateFixedBlock2, (int)FullFixedBlock2Offset.SoundGain, val.Length);

                    val = BitConverter.GetBytes((float)sound.Radius);
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(val);
                    }
                    Buffer.BlockCopy(val, 0, m_FullUpdateFixedBlock2, (int)FullFixedBlock2Offset.SoundRadius, val.Length);

                    m_FullUpdateFixedBlock2[(int)FullFixedBlock2Offset.SoundFlags] = (byte)sound.Flags;
                    if (sound.SoundID != UUID.Zero)
                    {
                        m_Part.Owner.ID.ToBytes(m_FullUpdateFixedBlock2, (int)FullFixedBlock2Offset.SoundOwner);
                    }
                    else
                    {
                        UUID.Zero.ToBytes(m_FullUpdateFixedBlock2, (int)FullFixedBlock2Offset.SoundOwner);
                    }
                }
                m_Part.TriggerOnUpdate(0);
            }
        }

        private CollisionSoundParam m_CollisionSound;

        private bool m_IsSoundQueueing;

        public CollisionSoundParam CollisionSound
        {
            get
            {
                CollisionSoundParam csp = m_CollisionSound;
                if(csp == null)
                {
                    return m_ParentInfo.CollisionSound;
                }
                else
                {
                    return new CollisionSoundParam(csp);
                }
            }
            set
            {
                if(value == null)
                {
                    if(m_ParentInfo == null)
                    {
                        throw new InvalidOperationException();
                    }
                    m_CollisionSound = null;
                }
                else
                {
                    m_CollisionSound = new CollisionSoundParam(value);
                }
                m_Part.TriggerOnUpdate(0);
            }
        }
    }
}
