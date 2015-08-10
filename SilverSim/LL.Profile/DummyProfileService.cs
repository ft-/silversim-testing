// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Profile;
using SilverSim.Types;
using SilverSim.Types.Profile;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Profile
{
    class DummyProfileService : ProfileServiceInterface
    {
        public DummyProfileService()
        {

        }

        class DummyClassifieds : IClassifiedsInterface
        {
            public DummyClassifieds()
            {

            }


            public Dictionary<UUID, string> getClassifieds(UUI user)
            {
                return new Dictionary<UUID, string>();
            }

            public ProfileClassified this[UUI user, UUID id]
            {
                get { throw new KeyNotFoundException(); }
            }

            public void Update(ProfileClassified classified)
            {
                throw new NotImplementedException();
            }

            public void Delete(UUID id)
            {
                throw new NotImplementedException();
            }
        }

        class DummyPicks : IPicksInterface
        {
            public DummyPicks()
            {

            }

            public Dictionary<UUID, string> getPicks(UUI user)
            {
                return new Dictionary<UUID, string>();
            }

            public ProfilePick this[UUI user, UUID id]
            {
                get { throw new KeyNotFoundException(); }
            }

            public void Update(ProfilePick pick)
            {
                throw new NotImplementedException();
            }

            public void Delete(UUID id)
            {
                throw new NotImplementedException();
            }
        }

        class DummyNotes : INotesInterface
        {
            public DummyNotes()
            {

            }

            public string this[UUI user, UUI target]
            {
                get
                {
                    return string.Empty;
                }
                set
                {
                    throw new NotImplementedException();
                }
            }
        }

        class DummyUserPrefs : IUserPreferencesInterface
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
                    throw new NotImplementedException();
                }
            }
        }

        class DummyProperties : IPropertiesInterface
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
                    props.WantToText = "";
                    props.SkillsMask = 0;
                    props.SkillsText = "";
                    props.Language = "";
                    props.ImageID = UUID.Zero;
                    props.AboutText = "";
                    props.FirstLifeImageID = UUID.Zero;
                    props.FirstLifeText = "";
                    return props;
                }
            }

            public ProfileProperties this[UUI user, PropertiesUpdateFlags flags]
            {
                set { throw new NotImplementedException(); }
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
