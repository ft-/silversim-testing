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
                    props.ImageID = "5748decc-f629-461c-9a36-a35a221fe21f";
                    props.AboutText = "";
                    props.FirstLifeImageID = "5748decc-f629-461c-9a36-a35a221fe21f";
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
