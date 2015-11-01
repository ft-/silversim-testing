// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace SilverSim.ServiceInterfaces.Avatar
{
    [Serializable]
    public class AvatarUpdateFailedException : Exception
    {
        public AvatarUpdateFailedException()
        { 
        }

        public AvatarUpdateFailedException(string message)
            : base(message)
        {

        }

        protected AvatarUpdateFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public AvatarUpdateFailedException(string message, Exception innerException)
            : base(message, innerException)
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

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract string this[UUID avatarID, string itemKey]
        {
            get;
            set;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract List<string> this[UUID avatarID, IList<string> itemKeys]
        {
            get;
            set;
        }

        public abstract void Remove(UUID avatarID, IList<string> nameList);
        public abstract void Remove(UUID avatarID, string name);
    }
}
