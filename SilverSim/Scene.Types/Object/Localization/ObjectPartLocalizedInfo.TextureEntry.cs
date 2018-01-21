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

using SilverSim.Threading;
using SilverSim.Types.Primitive;
using System;
using System.Threading;

namespace SilverSim.Scene.Types.Object.Localization
{
    public sealed partial class ObjectPartLocalizedInfo
    {
        private TextureEntry m_TextureEntry;
        private byte[] m_TextureEntryBytes;
        private readonly ReaderWriterLock m_TextureEntryLock = new ReaderWriterLock();

        private byte[] m_TextureAnimationBytes;

        private string m_MediaURL;

        public string MediaURL
        {
            get
            {
                return m_MediaURL ?? m_ParentInfo.MediaURL;
            }
            set
            {
                if (value != null)
                {
                    m_MediaURL = value;
                }
                else
                {
                    if(m_ParentInfo == null)
                    {
                        throw new InvalidOperationException();
                    }
                    m_MediaURL = value;
                }
                m_Part.TriggerOnUpdate(0);
            }
        }

        private UpdateChangedFlags ChangedTexParams(TextureEntryFace oldTexFace, TextureEntryFace newTexFace)
        {
            UpdateChangedFlags flags = 0;
            if (oldTexFace.Glow != newTexFace.Glow ||
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

            if (oldTexFace.TextureColor.R != newTexFace.TextureColor.R ||
                oldTexFace.TextureColor.G != newTexFace.TextureColor.G ||
                oldTexFace.TextureColor.B != newTexFace.TextureColor.B ||
                oldTexFace.TextureColor.A != newTexFace.TextureColor.A)
            {
                flags |= UpdateChangedFlags.Color;
            }

            if (oldTexFace.MediaFlags != newTexFace.MediaFlags)
            {
                flags |= UpdateChangedFlags.Media;
            }
            return flags;
        }

        private UpdateChangedFlags ChangedTexParams(TextureEntry oldTex, TextureEntry newTex)
        {
            UpdateChangedFlags flags = ChangedTexParams(oldTex.DefaultTexture, newTex.DefaultTexture);
            uint index;
            for (index = 0; index < 32; ++index)
            {
                flags |= ChangedTexParams(oldTex[index], newTex[index]);
            }
            return flags;
        }

        public TextureEntry TextureEntry
        {
            get
            {
                return m_TextureEntryLock.AcquireReaderLock(() => new TextureEntry(m_TextureEntryBytes));
            }
            set
            {
                UpdateChangedFlags flags = 0;
                var copy = new TextureEntry(value.GetBytes());
                m_TextureEntryLock.AcquireWriterLock(() =>
                {
                    flags = ChangedTexParams(m_TextureEntry, copy);
                    m_TextureEntry = copy;
                    m_TextureEntryBytes = value.GetBytes();
                });
                m_Part.TriggerOnUpdate(flags);
            }
        }

        public byte[] TextureEntryBytes
        {
            get
            {
                return m_TextureEntryLock.AcquireReaderLock(() =>
                {
                    var b = new byte[m_TextureEntryBytes.Length];
                    Buffer.BlockCopy(m_TextureEntryBytes, 0, b, 0, m_TextureEntryBytes.Length);
                    return b;
                });
            }
            set
            {
                if (value == null)
                {
                    if(m_ParentInfo == null)
                    {
                        throw new InvalidOperationException();
                    }
                    m_TextureEntryLock.AcquireWriterLock(() =>
                    {
                        m_TextureEntryBytes = null;
                        m_TextureEntry = null;
                    });
                    m_Part.TriggerOnUpdate(UpdateChangedFlags.Texture | UpdateChangedFlags.Color);
                }
                else
                {
                    UpdateChangedFlags flags = m_TextureEntryLock.AcquireWriterLock(() =>
                    {
                        TextureEntry newTex;
                        m_TextureEntryBytes = value;
                        newTex = new TextureEntry(value);
                        UpdateChangedFlags flag = ChangedTexParams(m_TextureEntry, newTex);
                        m_TextureEntry = newTex;
                        return flag;
                    });
                    m_Part.TriggerOnUpdate(flags);
                }
            }
        }

        public TextureAnimationEntry TextureAnimation
        {
            get
            {
                byte[] tab = m_TextureAnimationBytes;
                return tab == null ? m_ParentInfo.TextureAnimation : new TextureAnimationEntry(tab, 0);
            }
            set
            {
                if(value == null && m_ParentInfo != null)
                {
                    m_TextureAnimationBytes = null;
                }
                else if (value == null || (value.Flags & TextureAnimationEntry.TextureAnimMode.ANIM_ON) == 0)
                {
                    m_TextureAnimationBytes = new byte[0];
                }
                else
                {
                    m_TextureAnimationBytes = value.GetBytes();
                }
                m_Part.TriggerOnUpdate(0);
            }
        }

        public byte[] TextureAnimationBytes
        {
            get
            {
                byte[] b = m_TextureAnimationBytes;
                if(b == null)
                {
                    return m_ParentInfo.TextureAnimationBytes;
                }
                else
                {
                    var res = new byte[b.Length];
                    Buffer.BlockCopy(b, 0, res, 0, b.Length);
                    return res;
                }
            }
            set
            {
                if(value == null)
                {
                    m_TextureAnimationBytes = m_ParentInfo != null ? null : new byte[0];
                }
                else
                {
                    m_TextureAnimationBytes = value;
                }
                m_Part.TriggerOnUpdate(0);
            }
        }
    }
}
