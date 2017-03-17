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

        public abstract UUI this[UUID key] { get; }
        public abstract bool TryGetValue(UUID key, out UUI uui);

        /** <summary>if setting is not supported, ignore the details and return without exception. Only store authoritative information</summary> */
        public abstract void Store(UUI uui);
        public abstract bool Remove(UUID key);

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

        public UUI ResolveName(UUI uui)
        {
            UUI resultuui;
            return TryGetValue(uui, out resultuui) ? resultuui : uui;
        }

        public bool TranslateToUUI(string arg, out UUI uui)
        {
            uui = UUI.Unknown;
            if (arg.Contains("."))
            {
                string[] names = arg.Split(new char[] { '.' }, 2);
                if (names.Length == 1)
                {
                    names = new string[] { names[0], string.Empty };
                }
                UUI founduui;
                if (TryGetValue(names[0], names[1], out founduui))
                {
                    uui = founduui;
                    return true;
                }
            }
            else if (UUID.TryParse(arg, out uui.ID))
            {
                UUI founduui;
                if (TryGetValue(uui.ID, out founduui))
                {
                    uui = founduui;
                    return true;
                }
            }
            else if (UUI.TryParse(arg, out uui))
            {
                return true;
            }
            return false;
        }
    }
}
