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
        public RwLockedDictionaryAutoAdd<WearableType, RwLockedList<KeyValuePair<UUID, UUID>>> Wearables =
            new RwLockedDictionaryAutoAdd<WearableType, RwLockedList<KeyValuePair<UUID, UUID>>>(delegate() { return new RwLockedList<KeyValuePair<UUID, UUID>>(); });
        public RwLockedDictionaryAutoAdd<AttachmentPoint, RwLockedDictionary<UUID, UUID>> Attachments =
            new RwLockedDictionaryAutoAdd<AttachmentPoint, RwLockedDictionary<UUID, UUID>>(delegate() { return new RwLockedDictionary<UUID, UUID>(); });
        private ReaderWriterLock m_VisualParamsLock = new ReaderWriterLock();
        private byte[] m_VisualParams = new byte[] { 33, 61, 85, 23, 58, 127, 63, 85, 63, 42, 0, 85, 63, 36, 85, 95, 153, 63, 34, 0, 63, 109, 88, 132, 63, 136, 81, 85, 103, 136, 127, 0, 150, 150, 150, 127, 0, 0, 0, 0, 0, 127, 0, 0, 255, 127, 114, 127, 99, 63, 127, 140, 127, 127, 0, 0, 0, 191, 0, 104, 0, 0, 0, 0, 0, 0, 0, 0, 0, 145, 216, 133, 0, 127, 0, 127, 170, 0, 0, 127, 127, 109, 85, 127, 127, 63, 85, 42, 150, 150, 150, 150, 150, 150, 150, 25, 150, 150, 150, 0, 127, 0, 0, 144, 85, 127, 132, 127, 85, 0, 127, 127, 127, 127, 127, 127, 59, 127, 85, 127, 127, 106, 47, 79, 127, 127, 204, 2, 141, 66, 0, 0, 127, 127, 0, 0, 0, 0, 127, 0, 159, 0, 0, 178, 127, 36, 85, 131, 127, 127, 127, 153, 95, 0, 140, 75, 27, 127, 127, 0, 150, 150, 198, 0, 0, 63, 30, 127, 165, 209, 198, 127, 127, 153, 204, 51, 51, 255, 255, 255, 204, 0, 255, 150, 150, 150, 150, 150, 150, 150, 150, 150, 150, 0, 150, 150, 150, 150, 150, 0, 127, 127, 150, 150, 150, 150, 150, 150, 150, 150, 0, 0, 150, 51, 132, 150, 150, 150 };
        public double AvatarHeight;
        public Int32 Serial = 1;

        public readonly static byte[] BakeIndices = new byte[] { 8, 9, 10, 11, 19, 20 };
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
