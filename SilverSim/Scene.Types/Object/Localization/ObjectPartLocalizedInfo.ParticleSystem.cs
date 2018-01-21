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
                if(value == null)
                {
                    m_ParticleSystem = m_ParentInfo == null ? new byte[0] : null;
                }
                else
                {
                    m_ParticleSystem = value.GetBytes();
                }
                m_Part.TriggerOnUpdate(UpdateChangedFlags.None);
            }
        }

        public byte[] ParticleSystemBytes
        {
            get
            {
                var ps = m_ParticleSystem;
                var o = new byte[ps.Length];
                Buffer.BlockCopy(ps, 0, o, 0, ps.Length);
                return o;
            }

            set
            {
                if (value == null)
                {
                    m_ParticleSystem = m_ParentInfo == null ? new byte[0] : null;
                }
                else
                {
                    var ps = new byte[value.Length];
                    Buffer.BlockCopy(value, 0, ps, 0, value.Length);
                    m_ParticleSystem = ps;
                }
            }
        }
    }
}
