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
