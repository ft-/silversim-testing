// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart
    {
        #region Media Properties
        public PrimitiveMedia m_Media;

        public PrimitiveMedia Media
        {
            get
            {
                return m_Media;
            }
        }

        public void ClearMedia()
        {
            lock (m_DataLock)
            {
                m_Media = null;
                m_MediaURL = string.Empty;
            }
            TriggerOnUpdate(UpdateChangedFlags.Media);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public void UpdateMedia(PrimitiveMedia media, UUID updaterID)
        {
            lock(m_DataLock)
            {
                string mediaURL;
                if(string.IsNullOrEmpty(m_MediaURL))
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
            TriggerOnUpdate(UpdateChangedFlags.Media);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public void UpdateMediaFace(int face, PrimitiveMedia.Entry entry, UUID updaterID)
        {
            if(face >= 32)
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
                if(null == m_Media)
                {
                    m_Media = new PrimitiveMedia();
                }
                while(m_Media.Count <= face)
                {
                    m_Media.Add(null);
                }
                m_Media[face] = entry;
                m_MediaURL = mediaURL;
            }
            TriggerOnUpdate(UpdateChangedFlags.Media);
        }
        #endregion
    }
}
