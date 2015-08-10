// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Avatar
{
    public class AvatarUpdateFailedException : Exception
    {
        public AvatarUpdateFailedException()
        { 
        }
    }

    public abstract class AvatarServiceInterface
    {
        #region Constructor
        public AvatarServiceInterface()
        {

        }
        #endregion

        public abstract Dictionary<string, string> this[UUID avatarID]
        {
            get;
            set; /* setting null means remove of avatar settings */
        }

        public abstract string this[UUID avatarID, string itemKey]
        {
            get;
            set;
        }

        public abstract List<string> this[UUID avatarID, IList<string> itemKeys]
        {
            get;
            set;
        }

        public abstract void Remove(UUID avatarID, IList<string> nameList);
        public abstract void Remove(UUID avatarID, string name);
    }
}
