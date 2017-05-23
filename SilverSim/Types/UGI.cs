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
    public sealed class UGI : IEquatable<UGI>
    {
        public UUID ID = UUID.Zero;
        public string GroupName = string.Empty;
        public Uri HomeURI;
        public bool IsAuthoritative; /* false means Group Data has been validated through any available resolving service */

        public static explicit operator string(UGI v) => string.Format("{0};{1};{2}", v.ID.ToString(), v.HomeURI.ToString(), v.GroupName);

        public string GroupData
        {
            get { return string.Format("{0};{1}", HomeURI.ToString(), GroupName); }

            set
            {
                string[] parts = value.Split(new char[] { ';' }, 2, StringSplitOptions.None);
                if (parts.Length < 2)
                {
                    throw new ArgumentException("\"" + value + "\" is not a GroupData string");
                }
                HomeURI = new Uri(parts[0]);
                GroupName = parts[1];
            }
        }

        public string FullName
        {
            get
            {
                return (HomeURI == null) ?
                    string.Format("{0}", GroupName) :
                    string.Format("{0} @{1}", GroupName.Replace(' ', '.'), HomeURI.ToString());
            }
            set
            {
                string[] names = value.Split(new char[] { ' ' }, 2, StringSplitOptions.None);
                if(names.Length < 2)
                {
                    GroupName = names[0];
                    HomeURI = null;
                }
                else
                {
                    if (names[1].StartsWith("@"))
                    {
                        /* HG UUI */
                        HomeURI = new Uri("http://" + names[1]);
                        GroupName = names[0].Replace('.', ' ');
                    }
                    else
                    {
                        GroupName = value;
                        HomeURI = null;
                    }
                }
            }
        }

        public UGI()
        {
        }

        public UGI(UGI v)
        {
            ID = v.ID;
            GroupName = v.GroupName;
            HomeURI = v.HomeURI;
        }

        public UGI(UUID v)
        {
            ID = v;
        }

        public override bool Equals(object obj)
        {
            var u = obj as UGI;
            return (u != null) && Equals(u);
        }

        public bool Equals(UGI ugi)
        {
            if (ugi.ID == ID && ID == UUID.Zero)
            {
                return true;
            }

            return ugi.ID == ID &&
                ((ugi.HomeURI == null && HomeURI == null) ||
                (ugi.HomeURI != null && HomeURI != null && ugi.HomeURI.Equals(HomeURI)));
        }

        public override int GetHashCode()
        {
            var h = HomeURI;
            return (h != null) ?
                ID.GetHashCode() ^ GroupName.GetHashCode() ^ h.GetHashCode() :
                ID.GetHashCode() ^ GroupName.GetHashCode();
        }

        public UGI(UUID ID, string GroupName, Uri HomeURI)
        {
            this.ID = ID;
            this.GroupName = GroupName;
            this.HomeURI = HomeURI;
        }

        public UGI(string uuiString)
        {
            var parts = uuiString.Split(Semicolon, 3);
            if (parts.Length < 2)
            {
                ID = new UUID(parts[0]);
                return;
            }
            ID = new UUID(parts[0]);
            if (parts.Length > 2)
            {
                GroupName = parts[2];
            }
            HomeURI = new Uri(parts[1]);
        }

        public override string ToString() => (HomeURI != null) ?
                String.Format("{0};{1};{2}", ID.ToString(), HomeURI, GroupName) :
                ID.ToString();

        private static readonly char[] Semicolon = new char[1] { ';' };

        public static UGI Unknown => new UGI();

        public static bool operator ==(UGI l, UGI r)
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

        public static bool operator !=(UGI l, UGI r)
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
