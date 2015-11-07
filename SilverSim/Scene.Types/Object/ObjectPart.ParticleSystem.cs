// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.Primitive;
using System;
using System.Threading;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart
    {
        private byte[] m_ParticleSystem = new byte[0];
        readonly ReaderWriterLock m_ParticleSystemLock = new ReaderWriterLock();

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
