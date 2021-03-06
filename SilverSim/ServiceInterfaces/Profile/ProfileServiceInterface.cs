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

using SilverSim.ServiceInterfaces.Profile.This;
using SilverSim.Types;
using SilverSim.Types.Profile;
using System;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Profile
{
    public abstract class ProfileServiceInterface :
        IPicksThisInterface,
        IClassifiedsThisInterface
    {
        [Flags]
        public enum PropertiesUpdateFlags
        {
            None = 0,
            Properties = 1,
            Interests = 2
        }

        public abstract IClassifiedsInterface Classifieds { get; }
        public abstract IPicksInterface Picks { get; }
        public abstract INotesInterface Notes { get; }
        public abstract IUserPreferencesInterface Preferences { get; }
        public abstract IPropertiesInterface Properties { get; }

        ProfileClassified IClassifiedsThisInterface.this[UGUI user, UUID id]
        {
            get
            {
                ProfileClassified classified;
                if(Classifieds.TryGetValue(user, id, out classified))
                {
                    return classified;
                }
                throw new KeyNotFoundException();
            }
        }

        ProfilePick IPicksThisInterface.this[UGUI user, UUID id]
        {
            get
            {
                ProfilePick pick;
                if(Picks.TryGetValue(user, id, out pick))
                {
                    return pick;
                }
                throw new KeyNotFoundException();
            }
        }

        public abstract void Remove(UUID accountID);

        public virtual List<UUID> GetUserImageAssets(UGUI userId) => new List<UUID>();
    }
}
