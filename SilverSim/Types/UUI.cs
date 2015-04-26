/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using System;

namespace SilverSim.Types
{
    public sealed class UUI : IEquatable<UUI>
    {
        public UUID ID = UUID.Zero;
        public string FirstName = string.Empty;
        public string LastName = string.Empty;
        public Uri HomeURI = null;
        public bool IsAuthoritative = false; /* means User Data has been validated through any available resolving service */

        public static explicit operator string(UUI v)
        {
            if (v.HomeURI != null)
            {
                return string.Format("{0}", v.ID.ToString());
            }
            else
            {
                return string.Format("{0};{1};{2} {3}", v.ID.ToString(), v.HomeURI.ToString(), v.FirstName, v.LastName);
            }
        }

        public override bool Equals(object obj)
        {
            return (obj is UUI) ? this == (UUI)obj : false;
        }

        public bool Equals(UUI uui)
        {
            return uui.ID == ID;
        }

        public override int GetHashCode()
        {
            Uri h = HomeURI;
            if (h != null)
            {
                return ID.GetHashCode() ^ FirstName.GetHashCode() ^ LastName.GetHashCode() ^ h.GetHashCode();
            }
            else
            {
                return ID.GetHashCode() ^ FirstName.GetHashCode() ^ LastName.GetHashCode();
            }
        }



        public string CreatorData
        {
            get
            {
                return string.Format("{0};{1} {2}", HomeURI.ToString(), FirstName, LastName);
            }
            set
            {
                string[] parts = value.Split(new char[] { ';' }, 2, StringSplitOptions.None);
                if(parts.Length < 2)
                {
                    throw new ArgumentException();
                }
                HomeURI = new Uri(parts[0]);
                string[] names = parts[1].Split(new char[] { ' ' }, 2, StringSplitOptions.None);
                FirstName = names[0];
                if (names.Length > 1)
                {
                    LastName = names[1];
                }
                else
                {
                    LastName = string.Empty;
                }
            }
        }

        public string FullName
        {
            get
            {
                if (HomeURI == null)
                {
                    return string.Format("{0} {1}", FirstName.Replace(" ", "."), LastName.Replace(" ", "."));
                }
                else
                {
                    return string.Format("{0}.{1} @{2}", FirstName.Replace(" ", "."), LastName.Replace(" ", "."), HomeURI.ToString());
                }
            }
            set
            {
                string[] names = value.Split(new char[] { ' ' }, 2, StringSplitOptions.None);
                if(names.Length < 2)
                {
                    FirstName = names[0];
                    LastName = "";
                    HomeURI = null;
                }
                else
                {
                    if (names[1].StartsWith("@"))
                    {
                        /* HG UUI */
                        HomeURI = new Uri("http://" + names[1]);
                        names = names[0].Split(new char[] { '.' }, 2, StringSplitOptions.None);
                        if(names.Length < 2)
                        {
                            FirstName = names[0];
                            LastName = "";
                        }
                        else
                        {
                            FirstName = names[0];
                            LastName = names[1];
                        }
                    }
                    else
                    {
                        FirstName = names[0];
                        LastName = names[1];
                        HomeURI = null;
                    }
                }
            }
        }

        public UUI()
        {
        }

        public UUI(UUI uui)
        {
            this.ID = uui.ID;
            this.FirstName = uui.FirstName;
            this.LastName = uui.LastName;
            this.HomeURI = uui.HomeURI;
        }

        public UUI(UUID ID, string FirstName, string LastName, Uri HomeURI)
        {
            this.ID = ID;
            this.FirstName = FirstName;
            this.LastName = LastName;
            this.HomeURI = HomeURI;
        }

        public UUI(UUID ID, string FirstName, string LastName)
        {
            this.ID = ID;
            this.FirstName = FirstName;
            this.LastName = LastName;
        }

        public UUI(UUID ID, string creatorData)
        {
            this.ID = ID;
            string[] parts = creatorData.Split(Semicolon, 2);
            if(parts.Length < 2)
            {
                throw new ArgumentException("Invald UUI");
            }
            string[] names = parts[1].Split(Whitespace, 2);
            if(names.Length == 2)
            {
                LastName = names[1];
            }
            FirstName = names[0];
            HomeURI = new Uri(parts[0]);
        }

        public UUI(string uuiString)
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
                string[] names = parts[2].Split(Whitespace, 2);
                if (names.Length == 2)
                {
                    LastName = names[1];
                }
                FirstName = names[0];
            }
            HomeURI = new Uri(parts[1]);
        }

        public override string ToString()
        {
            if(HomeURI != null)
            {
                return String.Format("{0};{1};{2} {3}", ID.ToString(), HomeURI, FirstName.Replace(' ', '.'), LastName.Replace(' ', '.'));
            }
            else
            {
                return ID.ToString();
            }
        }

        private static readonly char[] Semicolon = new char[1] { (char)';' };
        private static readonly char[] Whitespace = new char[1] { (char)' ' };

        public static UUI Unknown
        {
            get
            {
                return new UUI();
            }
        }
    }
}
