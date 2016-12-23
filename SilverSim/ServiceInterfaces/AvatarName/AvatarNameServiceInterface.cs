// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.ServiceInterfaces.AvatarName
{
    public static class AvatarNameServiceExtensionMethods
    {
        public static UUI FindUUIByName(this List<AvatarNameServiceInterface> list, string firstname, string lastname)
        {
            UUI uui;
            foreach(AvatarNameServiceInterface service in list)
            {
                if(service.TryGetValue(firstname, lastname, out uui))
                {
                    return uui;
                }
            }
            throw new KeyNotFoundException();
        }

        public static UUI FindUUIById(this List<AvatarNameServiceInterface> list, UUID id)
        {
            UUI uui;
            foreach (AvatarNameServiceInterface service in list)
            {
                if (service.TryGetValue(id, out uui))
                {
                    return uui;
                }
            }
            throw new KeyNotFoundException();
        }
    }

    public abstract class AvatarNameServiceInterface
    {
        public AvatarNameServiceInterface()
        {

        }

        public abstract UUI this[UUID key] { get; set; } /* setting to null clears an entry if supported */
        public abstract bool TryGetValue(UUID key, out UUI uui);

        /* if setting is not supported, the set access is ignored */
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract UUI this[string firstName, string lastName] { get; }

        public abstract bool TryGetValue(string firstName, string lastName, out UUI uui);

        public abstract List<UUI> Search(string[] names); /* returns empty list when not supported */

        public bool TryGetValue(UUI input, out UUI uui)
        {
            if(!input.IsAuthoritative &&
                TryGetValue(input.ID, out uui))
            {
                return true;
            }
            uui = input;
            return false;
        }

        public UUI this[UUI input]
        {
            get
            {
                UUI resultuui;
                if (!input.IsAuthoritative &&
                    TryGetValue(input.ID, out resultuui))
                {
                    return resultuui;
                }
                return input;
            }
        }
    }
}
