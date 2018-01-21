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

using SilverSim.Scene.Types.Object.Localization;
using SilverSim.Scene.Types.Object.Parameters;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System.Globalization;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart
    {
        private readonly ObjectPartLocalizedInfo m_DefaultLocalization;

        #region Default Localization
        public PrimitiveMedia Media => m_DefaultLocalization.Media;

        public ObjectPartLocalizedInfo GetLocalization(CultureInfo culture) => m_DefaultLocalization;

        public void ClearMedia() => m_DefaultLocalization.ClearMedia();

        public void UpdateMedia(PrimitiveMedia media, UUID updaterID) => m_DefaultLocalization.UpdateMedia(media, updaterID);

        public void UpdateMediaFace(int face, PrimitiveMedia.Entry entry, UUID updaterID) => m_DefaultLocalization.UpdateMediaFace(face, entry, updaterID);

        public ParticleSystem ParticleSystem
        {
            get
            {
                return m_DefaultLocalization.ParticleSystem;
            }

            set
            {
                m_DefaultLocalization.ParticleSystem = value;
            }
        }

        public byte[] ParticleSystemBytes
        {
            get
            {
                return m_DefaultLocalization.ParticleSystemBytes;
            }

            set
            {
                m_DefaultLocalization.ParticleSystemBytes = value;
            }
        }

        public SoundParam Sound
        {
            get
            {
                return m_DefaultLocalization.Sound;
            }
            set
            {
                m_DefaultLocalization.Sound = value;
            }
        }

        public CollisionSoundParam CollisionSound
        {
            get
            {
                return m_DefaultLocalization.CollisionSound;
            }
            set
            {
                m_DefaultLocalization.CollisionSound = value;
            }
        }

        public TextParam Text
        {
            get
            {
                return m_DefaultLocalization.Text;
            }
            set
            {
                m_DefaultLocalization.Text = value;
            }
        }

        public string MediaURL
        {
            get
            {
                return m_DefaultLocalization.MediaURL;
            }
            set
            {
                m_DefaultLocalization.MediaURL = value;
            }
        }

        public TextureEntry TextureEntry
        {
            get
            {
                return m_DefaultLocalization.TextureEntry;
            }
            set
            {
                m_DefaultLocalization.TextureEntry = value;
            }
        }

        public byte[] TextureEntryBytes
        {
            get
            {
                return m_DefaultLocalization.TextureEntryBytes;
            }
            set
            {
                m_DefaultLocalization.TextureEntryBytes = value;
            }
        }

        public TextureAnimationEntry TextureAnimation
        {
            get
            {
                return m_DefaultLocalization.TextureAnimation;
            }
            set
            {
                m_DefaultLocalization.TextureAnimation = value;
            }
        }

        public byte[] TextureAnimationBytes
        {
            get
            {
                return m_DefaultLocalization.TextureAnimationBytes;
            }
            set
            {
                m_DefaultLocalization.TextureAnimationBytes = value;
            }
        }

        public ProjectionParam Projection
        {
            get
            {
                return m_DefaultLocalization.Projection;
            }
            set
            {
                m_DefaultLocalization.Projection = value;
            }
        }

        private void UpdateExtraParams() => m_DefaultLocalization.UpdateExtraParams();

        internal void UpdateData(ObjectPartLocalizedInfo.UpdateDataFlags flags)
        {
            m_DefaultLocalization.UpdateData(flags);
        }

        private void UpdateData(ObjectPartLocalizedInfo.UpdateDataFlags flags, bool incSerial)
        {
            m_DefaultLocalization.UpdateData(flags, incSerial);
        }

        internal ObjectPartLocalizedInfo[] Localizations => new ObjectPartLocalizedInfo[] { m_DefaultLocalization };
        #endregion
    }
}
