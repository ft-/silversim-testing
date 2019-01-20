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

using SilverSim.Types.Primitive;
using System;
using System.Linq;
using System.Threading;

namespace SilverSim.Scene.Types.Object.Localization
{
    public sealed partial class ObjectPartLocalizedInfo
    {
        private byte[] m_ParticleSystem;

        public ParticleSystem ParticleSystem
        {
            get
            {
                byte[] ps = m_ParticleSystem;
                if(ps == null)
                {
                    return m_ParentInfo.ParticleSystem;
                }
                if (ps.Length == 0)
                {
                    return null;
                }
                return new ParticleSystem(ps, 0);
            }

            set
            {
                bool changed;
                if(value == null)
                {
                    if(m_ParentInfo == null)
                    {
                        byte[] oldBytes = Interlocked.Exchange(ref m_ParticleSystem, new byte[0]);
                        changed = oldBytes.Length != 0;
                    }
                    else
                    {
                        byte[] oldBytes = Interlocked.Exchange(ref m_ParticleSystem, null);
                        changed = oldBytes != null;
                    }
                }
                else
                {
                    byte[] newBytes = value.GetBytes();
                    byte[] oldBytes = Interlocked.Exchange(ref m_ParticleSystem, newBytes);
                    changed = oldBytes == null || oldBytes.Length == 0 || !oldBytes.SequenceEqual(newBytes);
                }
                if (changed)
                {
                    UpdateData(UpdateDataFlags.AllObjectUpdate);
                    if (m_ParentInfo == null)
                    {
                        foreach (ObjectPartLocalizedInfo localization in m_Part.NamedLocalizations)
                        {
                            if (!localization.HasParticleSystem)
                            {
                                localization.UpdateData(UpdateDataFlags.AllObjectUpdate);
                            }
                        }
                    }
                    m_Part.TriggerOnUpdate(UpdateChangedFlags.None);
                }
            }
        }

        public bool HasParticleSystem => m_ParticleSystem != null;

        public byte[] ParticleSystemBytes
        {
            get
            {
                var ps = m_ParticleSystem;
                if(ps == null && m_ParentInfo != null)
                {
                    return m_ParentInfo.ParticleSystemBytes;
                }
                var o = new byte[ps.Length];
                Buffer.BlockCopy(ps, 0, o, 0, ps.Length);
                return o;
            }

            set
            {
                bool changed;
                if (value == null)
                {
                    if (m_ParentInfo == null)
                    {
                        byte[] oldBytes = Interlocked.Exchange(ref m_ParticleSystem, new byte[0]);
                        changed = oldBytes.Length != 0;
                    }
                    else
                    {
                        byte[] oldBytes = Interlocked.Exchange(ref m_ParticleSystem, null);
                        changed = oldBytes != null;
                    }
                }
                else
                {
                    var ps = new byte[value.Length];
                    Buffer.BlockCopy(value, 0, ps, 0, value.Length);
                    byte[] oldBytes = Interlocked.Exchange(ref m_ParticleSystem, ps);

                    changed = oldBytes == null || oldBytes.Length == 0 || !oldBytes.SequenceEqual(ps);
                }
                if(changed)
                {
                    UpdateData(UpdateDataFlags.AllObjectUpdate);
                    if (m_ParentInfo == null)
                    {
                        foreach (ObjectPartLocalizedInfo localization in m_Part.NamedLocalizations)
                        {
                            if (!localization.HasParticleSystem)
                            {
                                localization.UpdateData(UpdateDataFlags.AllObjectUpdate);
                            }
                        }
                    }
                    m_Part.TriggerOnUpdate(UpdateChangedFlags.None);
                }
            }
        }
    }
}
