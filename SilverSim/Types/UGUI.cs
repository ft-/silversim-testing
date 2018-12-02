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

using System;

namespace SilverSim.Types
{
    /** <summary> Unique Grid User Identifier </summary> */
    public sealed class UGUI : IEquatable<UGUI>
    {
        public UUID ID = UUID.Zero;
        public Uri HomeURI;
        public bool IsAuthoritative; /* false means User Data has been validated through any available resolving service */

        public static explicit operator string(UGUI v) => (v.HomeURI != null) ?
                string.Format("{0};{1};", v.ID.ToString(), v.HomeURI.ToString()) :
                string.Format("{0}", v.ID.ToString());

        /* make UGUI typecastable to UUI */
        public static explicit operator UGUIWithName(UGUI v) => new UGUIWithName(v.ID) { HomeURI = v.HomeURI };

        public static implicit operator UGUI(UGUIWithName v) => new UGUI(v.ID, v.HomeURI) { IsAuthoritative = v.IsAuthoritative };

        public bool IsSet => ID != UUID.Zero;

        public override bool Equals(object obj)
        {
            var u = obj as UGUI;
            return u != null && Equals(u);
        }

        public bool Equals(UGUI other) => ID == other.ID;

        public bool EqualsGrid(UGUI ugui)
        {
            if ((ugui.HomeURI != null && HomeURI == null) ||
                (ugui.HomeURI == null && HomeURI != null))
            {
                return false;
            }
            else if (ugui.HomeURI != null)
            {
                return ugui.ID == ID && ugui.HomeURI.Equals(HomeURI);
            }
            else
            {
                return ugui.ID == ID;
            }
        }

        public override int GetHashCode() => ID.GetHashCode();

        public string CreatorData
        {
            get
            {
                return (HomeURI != null) ?
                    string.Format("{0};", HomeURI.ToString()) :
                    string.Empty;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    HomeURI = null;
                }
                else
                {
                    var parts = value.Split(new char[] { ';' }, 2, StringSplitOptions.None);
                    HomeURI = new Uri(parts[0]);
                }
            }
        }

        public UGUI()
        {
        }

        public UGUI(UGUI ugui)
        {
            ID = ugui.ID;
            HomeURI = ugui.HomeURI;
        }

        public UGUI(UGUIWithName uui)
        {
            ID = uui.ID;
            HomeURI = uui.HomeURI;
        }

        public UGUI(UUID ID)
        {
            this.ID = ID;
        }

        public UGUI(UUID ID, Uri HomeURI)
        {
            this.ID = ID;
            this.HomeURI = HomeURI;
        }

        public UGUI(UUID ID, string creatorData)
        {
            this.ID = ID;
            var parts = creatorData.Split(Semicolon, 2);
            HomeURI = new Uri(parts[0]);
        }

        public UGUI(string uuiString)
        {
            var parts = uuiString.Split(Semicolon, 4); /* 4 allows for secret from friends entries */
            if (parts.Length < 2)
            {
                ID = new UUID(parts[0]);
                return;
            }
            ID = new UUID(parts[0]);
            HomeURI = new Uri(parts[1]);
        }

        public static bool TryParse(string uuiString, out UGUI ugui)
        {
            UUID id;
            Uri homeURI;
            ugui = default(UGUI);
            var parts = uuiString.Split(Semicolon, 4); /* 4 allows for secrets from friends entries */
            if (parts.Length < 2)
            {
                if (!UUID.TryParse(parts[0], out id))
                {
                    return false;
                }
                ugui = new UGUI(id);
                return true;
            }
            if (!UUID.TryParse(parts[0], out id))
            {
                return false;
            }
            if (!Uri.TryCreate(parts[1], UriKind.Absolute, out homeURI))
            {
                return false;
            }
            ugui = new UGUI(id, homeURI);
            return true;
        }

        public override string ToString() => (HomeURI != null) ?
                string.Format("{0};{1};", ID.ToString(), HomeURI) :
                ID.ToString();

        private static readonly char[] Semicolon = new char[1] { ';' };

        public static UGUI Unknown => new UGUI();

        public static bool operator ==(UGUI l, UGUI r)
        {
            /* get rid of type specifics */
            object lo = l;
            object ro = r;
            if (lo == null && ro == null)
            {
                return true;
            }
            else if (lo == null || ro == null)
            {
                return false;
            }
            return l.Equals(r);
        }

        public static bool operator !=(UGUI l, UGUI r)
        {
            /* get rid of type specifics */
            object lo = l;
            object ro = r;
            if (lo == null && ro == null)
            {
                return false;
            }
            else if (lo == null || ro == null)
            {
                return true;
            }
            return !l.Equals(r);
        }
    }
}
