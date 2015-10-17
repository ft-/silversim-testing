// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.Asset.Format;
using System;
using System.Collections.Generic;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.Types.Agent
{
    public class AppearanceInfo
    {
        public class AvatarTextureData
        {
            public readonly static int TextureCount = 21;
            private UUID[] m_AvatarTextures = new UUID[TextureCount];
            private ReaderWriterLock m_RwLock = new ReaderWriterLock();

            public AvatarTextureData()
            {

            }

            public UUID[] All
            {
                get
                {
                    m_RwLock.AcquireReaderLock(-1);
                    try
                    {
                        UUID[] textures = new UUID[TextureCount];
                        for (int i = 0; i < TextureCount; ++i)
                        {
                            textures[i] = new UUID(m_AvatarTextures[i]);
                        }
                        return textures;
                    }
                    finally
                    {
                        m_RwLock.ReleaseReaderLock();
                    }
                }
                set
                {
                    if(value.Length != TextureCount)
                    {
                        throw new ArgumentException("Invalid number of elements");
                    }
                    m_RwLock.AcquireWriterLock(-1);
                    try
                    {
                        for (int i = 0; i < TextureCount; ++i)
                        {
                            m_AvatarTextures[i] = new UUID(value[i]);
                        }
                    }
                    finally
                    {
                        m_RwLock.ReleaseWriterLock();
                    }
                }
            }

            public UUID this[int texIndex]
            {
                get
                {
                    if(texIndex < 0 || texIndex >= TextureCount)
                    {
                        throw new KeyNotFoundException();
                    }
                    m_RwLock.AcquireReaderLock(-1);
                    try
                    {
                        return m_AvatarTextures[texIndex];
                    }
                    finally
                    {
                        m_RwLock.ReleaseReaderLock();
                    }
                }

                set
                {
                    if (texIndex < 0 || texIndex >= TextureCount)
                    {
                        throw new KeyNotFoundException();
                    }
                    m_RwLock.AcquireWriterLock(-1);
                    try
                    {
                        m_AvatarTextures[texIndex] = value;
                    }
                    finally
                    {
                        m_RwLock.ReleaseWriterLock();
                    }
                }
            }
        }

        private AgentWearables m_Wearables = new AgentWearables();
        public AgentWearables Wearables
        {
            get
            {
                return m_Wearables;
            }
            set
            {
                m_Wearables.All = value;
            }
        }

        public RwLockedDictionaryAutoAdd<AttachmentPoint, RwLockedDictionary<UUID, UUID>> Attachments =
            new RwLockedDictionaryAutoAdd<AttachmentPoint, RwLockedDictionary<UUID, UUID>>(delegate() { return new RwLockedDictionary<UUID, UUID>(); });
        private ReaderWriterLock m_VisualParamsLock = new ReaderWriterLock();
        private byte[] m_VisualParams = new byte[] { 33, 61, 85, 23, 58, 127, 63, 85, 63, 42, 0, 85, 63, 36, 85, 95, 153, 63, 34, 0, 63, 109, 88, 132, 63, 136, 81, 85, 103, 136, 127, 0, 150, 150, 150, 127, 0, 0, 0, 0, 0, 127, 0, 0, 255, 127, 114, 127, 99, 63, 127, 140, 127, 127, 0, 0, 0, 191, 0, 104, 0, 0, 0, 0, 0, 0, 0, 0, 0, 145, 216, 133, 0, 127, 0, 127, 170, 0, 0, 127, 127, 109, 85, 127, 127, 63, 85, 42, 150, 150, 150, 150, 150, 150, 150, 25, 150, 150, 150, 0, 127, 0, 0, 144, 85, 127, 132, 127, 85, 0, 127, 127, 127, 127, 127, 127, 59, 127, 85, 127, 127, 106, 47, 79, 127, 127, 204, 2, 141, 66, 0, 0, 127, 127, 0, 0, 0, 0, 127, 0, 159, 0, 0, 178, 127, 36, 85, 131, 127, 127, 127, 153, 95, 0, 140, 75, 27, 127, 127, 0, 150, 150, 198, 0, 0, 63, 30, 127, 165, 209, 198, 127, 127, 153, 204, 51, 51, 255, 255, 255, 204, 0, 255, 150, 150, 150, 150, 150, 150, 150, 150, 150, 150, 0, 150, 150, 150, 150, 150, 0, 127, 127, 150, 150, 150, 150, 150, 150, 150, 150, 0, 0, 150, 51, 132, 150, 150, 150 };
        public double AvatarHeight;
        public UInt32 Serial = 1;

        public byte[] BakeIndices
        {
            get
            {
                return new byte[] { 8, 9, 10, 11, 19, 20 };
            }
        }
        public readonly static int MaxVisualParams = 260;

        public readonly AvatarTextureData AvatarTextures = new AvatarTextureData();

        public byte[] VisualParams
        {
            get
            {
                m_VisualParamsLock.AcquireReaderLock(-1);
                try
                {
                    byte[] res = new byte[m_VisualParams.Length];
                    Buffer.BlockCopy(m_VisualParams, 0, res, 0, m_VisualParams.Length);
                    return res;
                }
                finally
                {
                    m_VisualParamsLock.ReleaseReaderLock();
                }
            }
            set
            {
                m_VisualParamsLock.AcquireWriterLock(-1);
                try
                {
                    int VisualParamCount = MaxVisualParams < value.Length ? MaxVisualParams : value.Length;
                    m_VisualParams = new byte[VisualParamCount];
                    Buffer.BlockCopy(value, 0, m_VisualParams, 0, VisualParamCount);
                }
                finally
                {
                    m_VisualParamsLock.ReleaseWriterLock();
                }
            }
        }
    }
}
