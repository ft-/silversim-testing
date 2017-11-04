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
    public sealed class RegionAddress
    {
        public readonly string RegionName;
        public readonly string GatekeeperUri;

        public RegionAddress(string regionName, string gatekeeperUri)
        {
            RegionName = regionName ?? string.Empty;
            if(!string.IsNullOrEmpty(gatekeeperUri) && !Uri.IsWellFormedUriString(gatekeeperUri, UriKind.Absolute))
            {
                throw new ArgumentException(nameof(gatekeeperUri));
            }
            GatekeeperUri = gatekeeperUri ?? string.Empty;
        }

        public RegionAddress(string regionName)
        {
            Parse(regionName, out RegionName, out GatekeeperUri);
        }

        private bool IsGatekeeperValid => !string.IsNullOrEmpty(GatekeeperUri) && Uri.IsWellFormedUriString(GatekeeperUri, UriKind.Absolute);

        public bool IsDefaultRegion => string.IsNullOrEmpty(RegionName) && !string.IsNullOrEmpty(GatekeeperUri);
        public bool IsForeignGrid => !string.IsNullOrEmpty(GatekeeperUri);
        public bool IsOwnGrid => string.IsNullOrEmpty(GatekeeperUri);
        public bool IsValid => !string.IsNullOrEmpty(RegionName) || IsGatekeeperValid;
        public bool TargetsGatekeeperUri(string uri)
        {
            Uri a;
            Uri b;
            return Uri.TryCreate(GatekeeperUri, UriKind.Absolute, out a) &&
                Uri.TryCreate(uri, UriKind.Absolute, out b) &&
                Uri.Compare(a, b, UriComponents.SchemeAndServer, UriFormat.SafeUnescaped, StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        private static void Parse(string requestedName, out string regionName, out string gatekeeperURI)
        {
            bool isForeignGridTarget = false;
            regionName = requestedName;
            gatekeeperURI = string.Empty;

            string[] s = requestedName.Split(new char[] { ':' }, 3);
            if (s.Length > 1)
            {
                /* could be a foreign grid URI, check for number in second place */
                uint val;
                if (!uint.TryParse(s[1], out val))
                {
                    /* not a foreign grid map name */
                }
                else if (val > 65535)
                {
                    /* not a foreign grid map name */
                }
                else if (!Uri.IsWellFormedUriString("http://" + s[0] + ":" + s[1] + "/", UriKind.Absolute))
                {
                    /* not a foreign grid map name */
                }
                else
                {
                    gatekeeperURI = "http://" + s[0] + ":" + s[1] + "/";
                    regionName = (s.Length > 2) ?
                        s[2] :
                        string.Empty; /* Default Region */
                    isForeignGridTarget = true;
                }
            }
            if (isForeignGridTarget)
            {
                /* already identified one form */
            }
            else
            {
                s = requestedName.Split(new char[] { ' ' }, 2);
                if (s.Length > 1)
                {
                    if (Uri.IsWellFormedUriString(s[0], UriKind.Absolute))
                    {
                        /* this is a foreign grid URI of form <url> <region name> */
                        gatekeeperURI = s[0];
                        regionName = s[1];
                    }
                    else
                    {
                        /* does not look like a uri at all */
                    }
                }
                else if (Uri.IsWellFormedUriString(requestedName, UriKind.Absolute))
                {
                    /* this is a foreign Grid URI for the Default Region */
                    gatekeeperURI = requestedName;
                    regionName = string.Empty;
                }
                else
                {
                    /* local Grid URI */
                }
            }
        }
    }
}
