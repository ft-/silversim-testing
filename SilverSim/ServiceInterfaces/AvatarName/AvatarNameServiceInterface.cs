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

namespace SilverSim.ServiceInterfaces.AvatarName
{
    public static class AvatarNameServiceExtensionMethods
    {
        public static UGUIWithName FindUUIByName(this List<AvatarNameServiceInterface> list, string firstname, string lastname)
        {
            UGUIWithName uui;
            foreach(var service in list)
            {
                if(service.TryGetValue(firstname, lastname, out uui))
                {
                    return uui;
                }
            }
            throw new KeyNotFoundException();
        }

        public static UGUIWithName FindUUIById(this List<AvatarNameServiceInterface> list, UUID id)
        {
            UGUIWithName uui;
            foreach (var service in list)
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
        public virtual UGUIWithName this[UUID key]
        {
            get
            {
                UGUIWithName res;
                if(!TryGetValue(key, out res))
                {
                    throw new KeyNotFoundException();
                }
                return res;
            }
        }

        public abstract bool TryGetValue(UUID key, out UGUIWithName uui);

        /** <summary>if setting is not supported, ignore the details and return without exception. Only store authoritative information</summary> */
        public abstract void Store(UGUIWithName uui);
        public abstract bool Remove(UUID key);

        public virtual UGUIWithName this[string firstName, string lastName]
        {
            get
            {
                UGUIWithName res;
                if(!TryGetValue(firstName, lastName, out res))
                {
                    throw new KeyNotFoundException();
                }
                return res;
            }
        }

        public abstract bool TryGetValue(string firstName, string lastName, out UGUIWithName uui);

        public abstract List<UGUIWithName> Search(string[] names); /* returns empty list when not supported */

        public bool TryGetValue(UUID key, out UGUI ugui)
        {
            UGUIWithName uui;
            if(TryGetValue(key, out uui))
            {
                ugui = uui;
                return true;
            }
            else
            {
                ugui = UGUI.Unknown;
                return false;
            }
        }

        public bool TryGetValue(UGUI input, out UGUI ugui)
        {
            UGUI uui;
            if(TryGetValue(input.ID, out uui))
            {
                if(!input.IsAuthoritative || input.EqualsGrid(uui))
                {
                    ugui = uui;
                    return true;
                }
            }
            ugui = default(UGUI);
            return false;
        }

        public bool TryGetValue(UGUIWithName input, out UGUIWithName uui)
        {
            if(!input.IsAuthoritative &&
                TryGetValue(input.ID, out uui))
            {
                return true;
            }
            uui = input;
            return false;
        }

        public bool TryGetValue(UGUI input, out UGUIWithName uui)
        {
            if(TryGetValue(input.ID, out uui))
            {
                if(!input.IsAuthoritative || input.EqualsGrid(uui))
                {
                    return true;
                }
            }
            uui = UGUIWithName.Unknown;
            return false;
        }

        public UGUIWithName this[UGUIWithName input]
        {
            get
            {
                UGUIWithName resultuui;
                if (!input.IsAuthoritative &&
                    TryGetValue(input.ID, out resultuui))
                {
                    return resultuui;
                }
                return input;
            }
        }

        public UGUIWithName this[UGUI input]
        {
            get
            {
                UGUIWithName resultuui;
                if (TryGetValue(input.ID, out resultuui))
                {
                    if (!input.IsAuthoritative || input.EqualsGrid(resultuui))
                    {
                        return resultuui;
                    }
                }
                throw new KeyNotFoundException();
            }
        }

        public UGUIWithName ResolveName(UGUIWithName uui)
        {
            UGUIWithName resultuui;
            return TryGetValue(uui, out resultuui) ? resultuui : uui;
        }

        public UGUIWithName ResolveName(UGUI ugui)
        {
            UGUIWithName resultuui;
            if(TryGetValue(ugui, out resultuui))
            {
                if(!ugui.IsAuthoritative || ugui.EqualsGrid(resultuui))
                {
                    return resultuui;
                }
            }
            return (UGUIWithName)ugui;
        }

        public bool TranslateToUUI(string arg, out UGUI ugui)
        {
            UGUIWithName uui;
            bool success = TranslateToUUI(arg, out uui);
            ugui = uui ?? null;
            return success;
        }

        public bool TranslateToUUI(string arg, out UGUIWithName uui)
        {
            uui = UGUIWithName.Unknown;
            if (UGUIWithName.TryParse(arg, out uui))
            {
                return true;
            }
            else if (arg.Contains("."))
            {
                string[] names = arg.Split(new char[] { '.' }, 2);
                if (names.Length == 1)
                {
                    names = new string[] { names[0], string.Empty };
                }
                UGUIWithName founduui;
                if (TryGetValue(names[0], names[1], out founduui))
                {
                    uui = founduui;
                    return true;
                }
            }
            else if (UUID.TryParse(arg, out uui.ID))
            {
                UGUIWithName founduui;
                if (TryGetValue(uui.ID, out founduui))
                {
                    uui = founduui;
                    return true;
                }
            }
            return false;
        }
    }
}
