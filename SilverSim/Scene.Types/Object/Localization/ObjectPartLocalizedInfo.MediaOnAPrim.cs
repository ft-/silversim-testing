﻿// SilverSim is distributed under the terms of the
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

using SilverSim.Types;
using SilverSim.Types.Primitive;

namespace SilverSim.Scene.Types.Object.Localization
{
    public sealed partial class ObjectPartLocalizedInfo
    {
        #region Media Properties
        public PrimitiveMedia m_Media;

        public PrimitiveMedia Media => m_Media ?? m_ParentInfo?.m_Media;

        internal void SetMedia(PrimitiveMedia media)
        {
            lock (m_DataLock)
            {
                m_Media = media;
                if (m_Media == null)
                {
                    m_MediaURL = string.Empty;
                }
            }
        }

        public void ClearMedia()
        {
            lock (m_DataLock)
            {
                m_Media = null;
                m_MediaURL = string.Empty;
            }
            m_Part.TriggerOnUpdate(UpdateChangedFlags.Media);
        }

        public void UpdateMedia(PrimitiveMedia media, UUID updaterID)
        {
            lock (m_DataLock)
            {
                string mediaURL;
                if (string.IsNullOrEmpty(m_MediaURL))
                {
                    mediaURL = "x-mv:00000000/" + updaterID.ToString();
                }
                else
                {
                    string rawVersion = m_MediaURL.Substring(5, 10);
                    int version = int.Parse(rawVersion);
                    mediaURL = string.Format("x-mv:{0:D10}/{1}", ++version, updaterID);
                }
                m_MediaURL = mediaURL;
                m_Media = media;
            }
            m_Part.TriggerOnUpdate(UpdateChangedFlags.Media);
        }

        public void UpdateMediaFace(int face, PrimitiveMedia.Entry entry, UUID updaterID)
        {
            if (face >= 32)
            {
                return;
            }
            lock (m_DataLock)
            {
                string mediaURL;
                if (string.IsNullOrEmpty(m_MediaURL))
                {
                    mediaURL = "x-mv:00000000/" + updaterID.ToString();
                }
                else
                {
                    string rawVersion = m_MediaURL.Substring(5, 10);
                    int version = int.Parse(rawVersion);
                    mediaURL = string.Format("x-mv:{0:D10}/{1}", ++version, updaterID);
                }
                if (null == m_Media)
                {
                    m_Media = new PrimitiveMedia();
                }
                while (m_Media.Count <= face)
                {
                    m_Media.Add(null);
                }
                m_Media[face] = entry;
                m_MediaURL = mediaURL;
            }
            m_Part.TriggerOnUpdate(UpdateChangedFlags.Media);
        }
        #endregion
    }
}