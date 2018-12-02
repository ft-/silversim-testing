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
    /** <summary>Universal Experience Identifier</summary> */
    public class UEI : IEquatable<UEI>
    {
        public UUID ID = UUID.Zero;
        public string ExperienceName = string.Empty;
        public Uri HomeURI;
        public bool IsAuthoritative; /* false means Experience Data has been validated through any available resolving service */
        /** <summary>used by ExperienceNameStorage. name data without authorization has null here</summary> */
        public byte[] AuthorizationToken;

        public static explicit operator string(UEI v) => string.Format("{0};{1};{2}", v.ID.ToString(), v.HomeURI.ToString(), v.ExperienceName);

        public bool IsSet => ID != UUID.Zero;

        public string ExperienceData
        {
            get { return string.Format("{0};{1}", HomeURI.ToString(), ExperienceName); }

            set
            {
                string[] parts = value.Split(new char[] { ';' }, 2, StringSplitOptions.None);
                if (parts.Length < 2)
                {
                    throw new ArgumentException("\"" + value + "\" is not a ExperienceData string");
                }
                HomeURI = new Uri(parts[0]);
                ExperienceName = parts[1];
            }
        }

        public string FullName
        {
            get
            {
                return (HomeURI == null) ?
                    string.Format("{0}", ExperienceName) :
                    string.Format("{0} @{1}", ExperienceName.Replace(' ', '.'), HomeURI.ToString());
            }
            set
            {
                string[] names = value.Split(new char[] { ' ' }, 2, StringSplitOptions.None);
                if (names.Length < 2)
                {
                    ExperienceName = names[0];
                    HomeURI = null;
                }
                else
                {
                    if (names[1].StartsWith("@"))
                    {
                        /* HG UUI */
                        HomeURI = new Uri("http://" + names[1]);
                        ExperienceName = names[0].Replace('.', ' ');
                    }
                    else
                    {
                        ExperienceName = value;
                        HomeURI = null;
                    }
                }
            }
        }

        public UEI()
        {
        }

        public UEI(UEI v)
        {
            ID = v.ID;
            ExperienceName = v.ExperienceName;
            HomeURI = v.HomeURI;
        }

        public UEI(UUID v)
        {
            ID = v;
        }

        public override bool Equals(object obj)
        {
            var u = obj as UEI;
            return (u != null) && Equals(u);
        }

        public bool Equals(UEI other) => other.ID == ID;

        public bool EqualsGrid(UEI other)
        {
            if (other.ID == ID && ID == UUID.Zero)
            {
                return true;
            }

            return other.ID == ID &&
                ((other.HomeURI == null && HomeURI == null) ||
                (other.HomeURI != null && HomeURI != null && other.HomeURI.Equals(HomeURI)));
        }

        public override int GetHashCode() => ID.GetHashCode();

        public UEI(UUID ID, string ExperienceName, Uri HomeURI)
        {
            this.ID = ID;
            this.ExperienceName = ExperienceName;
            this.HomeURI = HomeURI;
        }

        public UEI(string uuiString)
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
                ExperienceName = parts[2];
            }
            HomeURI = new Uri(parts[1]);
        }

        public static bool TryParse(string uuiString, out UEI uei)
        {
            try
            {
                uei = new UEI(uuiString);
                return true;
            }
            catch
            {
                uei = UEI.Unknown;
                return false;
            }
        }

        public override string ToString() => (HomeURI != null) ?
                string.Format("{0};{1};{2}", ID.ToString(), HomeURI, ExperienceName) :
                ID.ToString();

        private static readonly char[] Semicolon = new char[1] { ';' };

        public static UEI Unknown => new UEI();

        public static bool operator ==(UEI l, UEI r)
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

        public static bool operator !=(UEI l, UEI r)
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
