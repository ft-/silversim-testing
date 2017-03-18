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
using SilverSim.Types;
using SilverSim.Types.Profile;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.Profile
{
    public sealed class DummyProfileService : ProfileServiceInterface
    {
        public DummyProfileService()
        {

        }

        public sealed class DummyClassifieds : IClassifiedsInterface
        {
            public DummyClassifieds()
            {

            }


            public Dictionary<UUID, string> GetClassifieds(UUI user)
            {
                return new Dictionary<UUID, string>();
            }

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            public ProfileClassified this[UUI user, UUID id]
            {
                get { throw new KeyNotFoundException(); }
            }

            public void Update(ProfileClassified classified)
            {
                throw new NotSupportedException();
            }

            public bool TryGetValue(UUI user, UUID id, out ProfileClassified classified)
            {
                classified = default(ProfileClassified);
                return false;
            }

            public bool ContainsKey(UUI user, UUID id)
            {
                return false;
            }

            public void Delete(UUID id)
            {
                throw new NotSupportedException();
            }
        }

        public sealed class DummyPicks : IPicksInterface
        {
            public DummyPicks()
            {

            }

            public Dictionary<UUID, string> GetPicks(UUI user)
            {
                return new Dictionary<UUID, string>();
            }

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            public ProfilePick this[UUI user, UUID id]
            {
                get { throw new KeyNotFoundException(); }
            }

            public bool TryGetValue(UUI user, UUID id, out ProfilePick pick)
            {
                pick = default(ProfilePick);
                return false;
            }

            public bool ContainsKey(UUI user, UUID id)
            {
                return false;
            }

            public void Update(ProfilePick pick)
            {
                throw new NotSupportedException();
            }

            public void Delete(UUID id)
            {
                throw new NotSupportedException();
            }
        }

        public sealed class DummyNotes : INotesInterface
        {
            public DummyNotes()
            {

            }

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            public string this[UUI user, UUI target]
            {
                get
                {
                    return string.Empty;
                }
                set
                {
                    throw new NotSupportedException();
                }
            }

            public bool TryGetValue(UUI user, UUI target, out string notes)
            {
                notes = string.Empty;
                return false;
            }

            public bool ContainsKey(UUI user, UUI target)
            {
                return false;
            }
        }

        public sealed class DummyUserPrefs : IUserPreferencesInterface
        {
            public DummyUserPrefs()
            {

            }

            public ProfilePreferences this[UUI user]
            {
                get
                {
                    ProfilePreferences prefs = new ProfilePreferences();
                    prefs.IMviaEmail = false;
                    prefs.User = user;
                    prefs.Visible = false;
                    return prefs;
                }
                set
                {
                    throw new NotSupportedException();
                }
            }

            public bool TryGetValue(UUI user, out ProfilePreferences prefs)
            {
                prefs = default(ProfilePreferences);
                return false;
            }

            public bool ContainsKey(UUI user)
            {
                return false;
            }
        }

        public sealed class DummyProperties : IPropertiesInterface
        {
            public DummyProperties()
            {

            }

            public ProfileProperties this[UUI user]
            {
                get 
                {
                    ProfileProperties props = new ProfileProperties();
                    props.User = user;
                    props.Partner = UUI.Unknown;
                    props.PublishProfile = false;
                    props.PublishMature = false;
                    props.WebUrl = string.Empty;
                    props.WantToMask = 0;
                    props.WantToText = string.Empty;
                    props.SkillsMask = 0;
                    props.SkillsText = string.Empty;
                    props.Language = string.Empty;
                    props.ImageID = UUID.Zero;
                    props.AboutText = string.Empty;
                    props.FirstLifeImageID = UUID.Zero;
                    props.FirstLifeText = string.Empty;
                    return props;
                }
            }

            public ProfileProperties this[UUI user, PropertiesUpdateFlags flags]
            {
                set { throw new NotSupportedException(); }
            }
        }

        public override IClassifiedsInterface Classifieds
        {
            get { return new DummyClassifieds(); }
        }

        public override IPicksInterface Picks
        {
            get { return new DummyPicks(); }
        }

        public override INotesInterface Notes
        {
            get { return new DummyNotes(); }
        }

        public override IUserPreferencesInterface Preferences
        {
            get { return new DummyUserPrefs(); }
        }

        public override IPropertiesInterface Properties
        {
            get { return new DummyProperties(); }
        }

        public override void Remove(UUID scopeID, UUID accountID)
        {
            throw new NotSupportedException();
        }
    }
}
