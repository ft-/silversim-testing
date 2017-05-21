﻿// SilverSim is distributed under the terms of the
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

using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart
    {
        public class SoundParam
        {
            #region Fields
            public UUID SoundID;
            public double Gain;
            public double Radius;
            public PrimitiveSoundFlags Flags; /* byte */
            #endregion

            public byte[] Serialization
            {
                get
                {
                    var serialized = new byte[33];
                    SoundID.ToBytes(serialized, 0);
                    Buffer.BlockCopy(BitConverter.GetBytes(Gain), 0, serialized, 16, 8);
                    Buffer.BlockCopy(BitConverter.GetBytes(Radius), 0, serialized, 24, 8);
                    serialized[32] = (byte)Flags;
                    if(!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(serialized, 16, 8);
                        Array.Reverse(serialized, 24, 8);
                    }
                    return serialized;
                }
                set
                {
                    if(value.Length != 33)
                    {
                        throw new ArgumentException("Array length must be 33.");
                    }
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(value, 16, 8);
                        Array.Reverse(value, 24, 8);
                    }
                    SoundID.FromBytes(value, 0);
                    Gain = BitConverter.ToDouble(value, 16);
                    Radius = BitConverter.ToDouble(value, 24);
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(value, 16, 8);
                        Array.Reverse(value, 24, 8);
                    }
                }
            }
        }

        private readonly SoundParam m_Sound = new SoundParam();

        public SoundParam Sound
        {
            get
            {
                var p = new SoundParam();
                lock(m_Sound)
                {
                    p.Flags = m_Sound.Flags;
                    p.Gain = m_Sound.Gain;
                    p.Radius = m_Sound.Radius;
                    p.SoundID = m_Sound.SoundID;
                }
                return p;
            }
            set
            {
                lock(m_Sound)
                {
                    m_Sound.SoundID = value.SoundID;
                    m_Sound.Gain = value.Gain;
                    m_Sound.Radius = value.Radius;
                    m_Sound.Flags = value.Flags;
                }
                lock(m_UpdateDataLock)
                {
                    value.SoundID.ToBytes(m_FullUpdateFixedBlock2, (int)FullFixedBlock2Offset.LoopedSound);
                    byte[] val = BitConverter.GetBytes((float)value.Gain);
                    if(!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(val);
                    }
                    Buffer.BlockCopy(val, 0, m_FullUpdateFixedBlock2, (int)FullFixedBlock2Offset.SoundGain, val.Length);

                    val = BitConverter.GetBytes((float)value.Radius);
                    if(!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(val);
                    }
                    Buffer.BlockCopy(val, 0, m_FullUpdateFixedBlock2, (int)FullFixedBlock2Offset.SoundRadius, val.Length);

                    m_FullUpdateFixedBlock2[(int)FullFixedBlock2Offset.SoundFlags] = (byte)value.Flags;
                    if(value.SoundID != UUID.Zero)
                    {
                        Owner.ID.ToBytes(m_FullUpdateFixedBlock2, (int)FullFixedBlock2Offset.SoundOwner);
                    }
                    else
                    {
                        UUID.Zero.ToBytes(m_FullUpdateFixedBlock2, (int)FullFixedBlock2Offset.SoundOwner);
                    }
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public class CollisionSoundParam
        {
            #region Fields
            public UUID ImpactSound = UUID.Zero;
            public double ImpactVolume;
            #endregion

            public byte[] Serialization
            {
                get
                {
                    var serialized = new byte[24];
                    ImpactSound.ToBytes(serialized, 0);
                    Buffer.BlockCopy(BitConverter.GetBytes(ImpactVolume), 0, serialized, 16, 8);
                    if(!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(serialized, 16, 8);
                    }
                    return serialized;
                }

                set
                {
                    if(value.Length != 24)
                    {
                        throw new ArgumentException("Array length must be 24.");
                    }
                    if(!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(value, 16, 8);
                    }
                    ImpactSound.FromBytes(value, 0);
                    ImpactVolume = BitConverter.ToDouble(value, 16);
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(value, 16, 8);
                    }
                }
            }
        }

        private readonly CollisionSoundParam m_CollisionSound = new CollisionSoundParam();

        private bool m_IsSoundQueueing;

        public CollisionSoundParam CollisionSound
        {
            get
            {
                var res = new CollisionSoundParam();
                lock (m_CollisionSound)
                {
                    res.ImpactSound = m_CollisionSound.ImpactSound;
                    res.ImpactVolume = m_CollisionSound.ImpactVolume;
                }
                return res;
            }
            set
            {
                lock (m_CollisionSound)
                {
                    m_CollisionSound.ImpactSound = value.ImpactSound;
                    m_CollisionSound.ImpactVolume = value.ImpactVolume;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }
    }
}
