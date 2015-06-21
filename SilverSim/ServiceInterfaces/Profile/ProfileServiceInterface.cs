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

using SilverSim.Types;
using SilverSim.Types.Profile;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Profile
{
    public abstract class ProfileServiceInterface
    {
        public interface IClassifiedsInterface
        {
            Dictionary<UUID, string> getClassifieds(UUI user);
            ProfileClassified this[UUI user, UUID id] { get; }
            void Update(ProfileClassified classified);
            void Delete(UUID id);
        }

        public interface IPicksInterface
        {
            Dictionary<UUID, string> getPicks(UUI user);
            ProfilePick this[UUI user, UUID id] { get; }
            void Update(ProfilePick pick);
            void Delete(UUID id);
        }

        public interface INotesInterface
        {
            ProfileNotes this[UUI user, UUI target] { get; set; }
        }

        public interface IUserPreferencesInterface
        {
            ProfilePreferences this[UUI user] { get; set; }
        }

        public interface IPropertiesInterface
        {
            ProfileProperties this[UUI user] { get; set; }
        }

        public ProfileServiceInterface()
        {

        }

        public abstract IClassifiedsInterface Classifieds { get; }
        public abstract IPicksInterface Picks { get; }
        public abstract INotesInterface Notes { get; }
        public abstract IUserPreferencesInterface Preferences { get; }
        public abstract IPropertiesInterface Properties { get; }
    }
}
