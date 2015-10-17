// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Types
{
    public sealed class UGI : IEquatable<UGI>
    {
        public UUID ID = UUID.Zero;
        public string GroupName = string.Empty;
        public Uri HomeURI;
        public bool IsAuthoritative; /* false means Group Data has been validated through any available resolving service */

        public static explicit operator string(UGI v)
        {
            return string.Format("{0};{1};{2}", v.ID.ToString(), v.HomeURI.ToString(), v.GroupName);
        }

        public string GroupData
        {
            get
            {
                return string.Format("{0};{1}", HomeURI.ToString(), GroupName);
            }
            set
            {
                string[] parts = value.Split(new char[] { ';' }, 2, StringSplitOptions.None);
                if(parts.Length < 2)
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
                if (HomeURI == null)
                {
                    return string.Format("{0}", GroupName);
                }
                else
                {
                    return string.Format("{0} @{1}", GroupName.Replace(" ", "."), HomeURI.ToString());
                }
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
            return (obj is UGI) ? this.Equals((UGI)obj) : false;
        }

        public bool Equals(UGI ugi)
        {
            if (ugi.ID == ID && ID == UUID.Zero)
            {
                return true;
            }

            return ugi.ID == ID && ugi.GroupName == GroupName && ugi.HomeURI.Equals(HomeURI);
        }

        public override int GetHashCode()
        {
            Uri h = HomeURI;
            if (h != null)
            {
                return ID.GetHashCode() ^ GroupName.GetHashCode() ^ h.GetHashCode();
            }
            else
            {
                return ID.GetHashCode() ^ GroupName.GetHashCode();
            }
        }


        public UGI(UUID ID, string GroupName, Uri HomeURI)
        {
            this.ID = ID;
            this.GroupName = GroupName;
            this.HomeURI = HomeURI;
        }

        public UGI(string uuiString)
        {
            string[] parts = uuiString.Split(Semicolon, 3);
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

        public override string ToString()
        {
            if(HomeURI != null)
            {
                return String.Format("{0};{1};{2}", ID.ToString(), HomeURI, GroupName);
            }
            else
            {
                return ID.ToString();
            }
        }

        private static readonly char[] Semicolon = new char[1] { (char)';' };

        public static UGI Unknown
        {
            get
            {
                return new UGI();
            }
        }
    }
}
