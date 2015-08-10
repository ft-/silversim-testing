// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.Primitive;
using System;
using System.Threading;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart
    {
        private TextureEntry m_TextureEntry = new TextureEntry();
        private byte[] m_TextureEntryBytes = new byte[0];
        private ReaderWriterLock m_TextureEntryLock = new ReaderWriterLock();

        private byte[] m_TextureAnimationBytes = new byte[0];
        private ReaderWriterLock m_TextureAnimationLock = new ReaderWriterLock();

        private string m_MediaURL = string.Empty;

        public string MediaURL
        {
            get
            {
                lock (this)
                {
                    return m_MediaURL;
                }
            }
            set
            {
                if (value != null)
                {
                    lock (this)
                    {
                        m_MediaURL = value;
                    }
                }
                else
                {
                    lock (this)
                    {
                        m_MediaURL = "";
                    }
                }
                TriggerOnUpdate(0);
            }
        }

        public TextureEntry TextureEntry
        {
            get
            {
                m_TextureEntryLock.AcquireReaderLock(-1);
                try
                {
                    return new TextureEntry(m_TextureEntryBytes);
                }
                finally
                {
                    m_TextureEntryLock.ReleaseReaderLock();
                }
            }
            set
            {
                TextureEntry copy = new TextureEntry(value.GetBytes());
                m_TextureEntryLock.AcquireWriterLock(-1);
                try
                {
                    m_TextureEntry = copy;
                    m_TextureEntryBytes = value.GetBytes();
                }
                finally
                {
                    m_TextureEntryLock.ReleaseWriterLock();
                }
            }
        }

        public byte[] TextureEntryBytes
        {
            get
            {
                m_TextureEntryLock.AcquireReaderLock(-1);
                try
                {
                    byte[] b = new byte[m_TextureEntryBytes.Length];
                    Buffer.BlockCopy(m_TextureEntryBytes, 0, b, 0, m_TextureEntryBytes.Length);
                    return b;
                }
                finally
                {
                    m_TextureEntryLock.ReleaseReaderLock();
                }
            }
            set
            {
                m_TextureEntryLock.AcquireWriterLock(-1);
                try
                {
                    m_TextureEntryBytes = value;
                    m_TextureEntry = new TextureEntry(value);
                }
                finally
                {
                    m_TextureEntryLock.ReleaseWriterLock();
                }
            }
        }

        public TextureAnimationEntry TextureAnimation
        {
            get
            {
                m_TextureAnimationLock.AcquireReaderLock(-1);
                try
                {
                    return new TextureAnimationEntry(m_TextureAnimationBytes, 0);
                }
                finally
                {
                    m_TextureAnimationLock.ReleaseReaderLock();
                }
            }
            set
            {
                if (value != null ? (value.Flags & TextureAnimationEntry.TextureAnimMode.ANIM_ON) == 0 : true)
                {
                    m_TextureAnimationLock.AcquireWriterLock(-1);
                    try
                    {
                        m_TextureAnimationBytes = new byte[0];
                    }
                    finally
                    {
                        m_TextureAnimationLock.ReleaseWriterLock();
                    }
                }
                else
                {
                    m_TextureAnimationLock.AcquireWriterLock(-1);
                    try
                    {
                        m_TextureAnimationBytes = value.GetBytes();
                    }
                    finally
                    {
                        m_TextureAnimationLock.ReleaseWriterLock();
                    }
                }
            }
        }

        public byte[] TextureAnimationBytes
        {
            get
            {
                m_TextureAnimationLock.AcquireReaderLock(-1);
                try
                {
                    byte[] b = new byte[m_TextureAnimationBytes.Length];
                    Buffer.BlockCopy(m_TextureEntryBytes, 0, b, 0, m_TextureAnimationBytes.Length);
                    return b;
                }
                finally
                {
                    m_TextureAnimationLock.ReleaseReaderLock();
                }
            }
            set
            {
                m_TextureAnimationLock.AcquireWriterLock(-1);
                try
                {
                    m_TextureAnimationBytes = value;
                }
                finally
                {
                    m_TextureAnimationLock.ReleaseWriterLock();
                }
            }
        }
    }
}
