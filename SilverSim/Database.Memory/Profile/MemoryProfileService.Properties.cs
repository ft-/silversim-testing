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

using SilverSim.ServiceInterfaces.Profile;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Profile;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Database.Memory.Profile
{
    partial class MemoryProfileService : ProfileServiceInterface.IPropertiesInterface
    {
        private readonly Dictionary<UUID, ProfileProperties> m_Properties = new Dictionary<UUID, ProfileProperties>();
        private readonly ReaderWriterLock m_PropertiesLock = new ReaderWriterLock();

        public ProfileProperties this[UUI user, PropertiesUpdateFlags flags]
        {
            set
            {
                m_PropertiesLock.AcquireWriterLock(() =>
                {
                    ProfileProperties props;
                    if (!m_Properties.TryGetValue(user.ID, out props))
                    {
                        props = value;
                        props.User = user;
                    }

                    if ((flags & PropertiesUpdateFlags.Properties) != 0)
                    {
                        props.PublishProfile = value.PublishProfile;
                        props.PublishMature = value.PublishMature;
                        props.WebUrl = value.WebUrl;
                        props.ImageID = value.ImageID;
                        props.AboutText = value.AboutText;
                        props.FirstLifeImageID = value.FirstLifeImageID;
                        props.FirstLifeText = value.FirstLifeText;
                    }
                    if ((flags & PropertiesUpdateFlags.Interests) != 0)
                    {
                        props.WantToMask = value.WantToMask;
                        props.WantToText = value.WantToText;
                        props.SkillsMask = value.SkillsMask;
                        props.SkillsText = value.SkillsText;
                        props.Language = value.Language;
                    }

                    m_Properties[user.ID] = props;
                });
            }
        }

        ProfileProperties IPropertiesInterface.this[UUI user] => m_PropertiesLock.AcquireReaderLock(() =>
        {
            ProfileProperties props;
            if (!m_Properties.TryGetValue(user.ID, out props))
            {
                props = new ProfileProperties()
                {
                    User = user,
                    Partner = UUI.Unknown,
                    WebUrl = string.Empty,
                    WantToText = string.Empty,
                    SkillsText = string.Empty,
                    Language = string.Empty,
                    AboutText = string.Empty,
                    FirstLifeText = string.Empty
                };
            }
            return props;
        });
    }
}
