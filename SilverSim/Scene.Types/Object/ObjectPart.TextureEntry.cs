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
using System.Threading;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart
    {
        private TextureEntry m_TextureEntry = new TextureEntry();
        private byte[] m_TextureEntryBytes = new byte[0];
        readonly ReaderWriterLock m_TextureEntryLock = new ReaderWriterLock();

        private byte[] m_TextureAnimationBytes = new byte[0];
        readonly ReaderWriterLock m_TextureAnimationLock = new ReaderWriterLock();

        private string m_MediaURL = string.Empty;

        public string MediaURL
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_MediaURL;
                }
            }
            set
            {
                if (value != null)
                {
                    lock (m_DataLock)
                    {
                        m_MediaURL = value;
                    }
                }
                else
                {
                    lock (m_DataLock)
                    {
                        m_MediaURL = string.Empty;
                    }
                }
                TriggerOnUpdate(0);
            }
        }

        UpdateChangedFlags ChangedTexParams(TextureEntryFace oldTexFace, TextureEntryFace newTexFace)
        {
            UpdateChangedFlags flags = 0;
            if(oldTexFace.Glow != newTexFace.Glow ||
                oldTexFace.Bump != newTexFace.Bump ||
                oldTexFace.FullBright != newTexFace.FullBright ||
                oldTexFace.MaterialID != newTexFace.MaterialID ||
                oldTexFace.OffsetU != newTexFace.OffsetU ||
                oldTexFace.OffsetV != newTexFace.OffsetV ||
                oldTexFace.RepeatU != newTexFace.RepeatU ||
                oldTexFace.RepeatV != newTexFace.RepeatV ||
                oldTexFace.Rotation != newTexFace.Rotation ||
                oldTexFace.Shiny != newTexFace.Shiny ||
                oldTexFace.TexMapType != newTexFace.TexMapType ||
                oldTexFace.TextureID != newTexFace.TextureID)
            {
                flags |= UpdateChangedFlags.Texture;
            }

            if(oldTexFace.TextureColor.R != newTexFace.TextureColor.R ||
                oldTexFace.TextureColor.G != newTexFace.TextureColor.G ||
                oldTexFace.TextureColor.B != newTexFace.TextureColor.B ||
                oldTexFace.TextureColor.A != newTexFace.TextureColor.A)
            {
                flags |= UpdateChangedFlags.Color;
            }

            if(oldTexFace.MediaFlags != newTexFace.MediaFlags)
            {
                flags |= UpdateChangedFlags.Media;
            }
            return flags;
        }

        UpdateChangedFlags ChangedTexParams(TextureEntry oldTex, TextureEntry newTex)
        {
            UpdateChangedFlags flags = ChangedTexParams(oldTex.DefaultTexture, newTex.DefaultTexture);
            uint index;
            for(index = 0; index < 32; ++index)
            {
                flags |= ChangedTexParams(oldTex[index], newTex[index]);
            }
            return flags;
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
                UpdateChangedFlags flags = 0;
                TextureEntry copy = new TextureEntry(value.GetBytes());
                m_TextureEntryLock.AcquireWriterLock(-1);
                try
                {
                    flags = ChangedTexParams(m_TextureEntry, copy);
                    m_TextureEntry = copy;
                    m_TextureEntryBytes = value.GetBytes();
                }
                finally
                {
                    m_TextureEntryLock.ReleaseWriterLock();
                }
                TriggerOnUpdate(flags);
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
                UpdateChangedFlags flags;
                m_TextureEntryLock.AcquireWriterLock(-1);
                try
                {
                    TextureEntry newTex;
                    m_TextureEntryBytes = value;
                    newTex = new TextureEntry(value);
                    flags = ChangedTexParams(m_TextureEntry, newTex);
                    m_TextureEntry = newTex;
                }
                finally
                {
                    m_TextureEntryLock.ReleaseWriterLock();
                }
                TriggerOnUpdate(flags);
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
                if (value == null || (value.Flags & TextureAnimationEntry.TextureAnimMode.ANIM_ON) == 0)
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
                TriggerOnUpdate(0);
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
                TriggerOnUpdate(0);
            }
        }
    }
}
