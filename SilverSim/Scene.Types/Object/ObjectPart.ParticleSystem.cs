/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Types.Primitive;
using System;
using System.Threading;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart
    {
        private byte[] m_ParticleSystem = new byte[0];
        private ReaderWriterLock m_ParticleSystemLock = new ReaderWriterLock();

        public ParticleSystem ParticleSystem
        {
            get
            {
                m_ParticleSystemLock.AcquireReaderLock(-1);
                try
                {
                    if (m_ParticleSystem.Length == 0)
                    {
                        return null;
                    }
                    return new ParticleSystem(m_ParticleSystem, 0);
                }
                finally
                {
                    m_ParticleSystemLock.ReleaseReaderLock();
                }
            }

            set
            {
                m_ParticleSystemLock.AcquireWriterLock(-1);
                try
                {
                    if (value == null)
                    {
                        m_ParticleSystem = new byte[0];
                    }
                    else
                    {
                        m_ParticleSystem = value.GetBytes();
                    }
                }
                finally
                {
                    m_ParticleSystemLock.ReleaseWriterLock();
                }
            }
        }

        public byte[] ParticleSystemBytes
        {
            get
            {
                m_ParticleSystemLock.AcquireReaderLock(-1);
                try
                {
                    byte[] o = new byte[m_ParticleSystem.Length];
                    Buffer.BlockCopy(m_ParticleSystem, 0, o, 0, m_ParticleSystem.Length);
                    return o;
                }
                finally
                {
                    m_ParticleSystemLock.ReleaseReaderLock();
                }
            }

            set
            {
                m_ParticleSystemLock.AcquireWriterLock(-1);
                try
                {
                    if (value == null)
                    {
                        m_ParticleSystem = new byte[0];
                    }
                    else
                    {
                        m_ParticleSystem = new byte[value.Length];
                        Buffer.BlockCopy(value, 0, m_ParticleSystem, 0, value.Length);
                    }
                }
                finally
                {
                    m_ParticleSystemLock.ReleaseWriterLock();
                }
            }
        }
    }
}
