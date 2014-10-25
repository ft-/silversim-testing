using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Types
{
    public sealed class UGI
    {
        public UUID ID = UUID.Zero;
        public string GroupName = string.Empty;
        public Uri HomeURI = null;

        public static implicit operator string(UGI v)
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
                    throw new ArgumentException();
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
        private static readonly char[] Whitespace = new char[1] { (char)' ' };

        public static UGI Unknown
        {
            get
            {
                return new UGI();
            }
        }
    }
}
