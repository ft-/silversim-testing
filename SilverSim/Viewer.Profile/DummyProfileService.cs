// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
    }
}
