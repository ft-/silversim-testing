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

using SilverSim.Types;
using SilverSim.Types.Profile;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.ServiceInterfaces.Profile
{
    public abstract class ProfileServiceInterface
    {
        public interface IClassifiedsInterface
        {
            Dictionary<UUID, string> GetClassifieds(UUI user);
            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            ProfileClassified this[UUI user, UUID id] { get; }
            bool TryGetValue(UUI user, UUID id, out ProfileClassified classified);
            bool ContainsKey(UUI user, UUID id);
            void Update(ProfileClassified classified);
            void Delete(UUID id);
        }

        public interface IPicksInterface
        {
            Dictionary<UUID, string> GetPicks(UUI user);
            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            ProfilePick this[UUI user, UUID id] { get; }
            bool TryGetValue(UUI user, UUID id, out ProfilePick pick);
            bool ContainsKey(UUI user, UUID id);
            void Update(ProfilePick pick);
            void Delete(UUID id);
        }

        public interface INotesInterface
        {
            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            string this[UUI user, UUI target] { get; set; }
            bool TryGetValue(UUI user, UUI target, out string notes);
            bool ContainsKey(UUI user, UUI target);
        }

        public interface IUserPreferencesInterface
        {
            ProfilePreferences this[UUI user] { get; set; }
            bool TryGetValue(UUI user, out ProfilePreferences profilePrefs);
            bool ContainsKey(UUI user);
        }

        [Flags]
        [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
        public enum PropertiesUpdateFlags
        {
            Properties = 1,
            Interests = 2
        }

        public interface IPropertiesInterface
        {
            ProfileProperties this[UUI user] { get; }
            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
            ProfileProperties this[UUI user, PropertiesUpdateFlags flags] { set; }
        }

        public abstract IClassifiedsInterface Classifieds { get; }
        public abstract IPicksInterface Picks { get; }
        public abstract INotesInterface Notes { get; }
        public abstract IUserPreferencesInterface Preferences { get; }
        public abstract IPropertiesInterface Properties { get; }

        public abstract void Remove(UUID scopeID, UUID accountID);

        public virtual List<UUID> GetUserImageAssets(UUI userId) => new List<UUID>();
    }
}
