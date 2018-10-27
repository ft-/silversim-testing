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

using System.Collections.Generic;

namespace SilverSim.Types.UserSession
{
    public enum KnownUserSessionInfoVariables
    {
        LocationRegionID = 1, /* set to UUID.Zero if Avatar is not on local map server */
        LocationGridURI = 2
    }

    public sealed class UserSessionInfo
    {
        public sealed class Entry
        {
            public string Value = string.Empty;
            public Date ExpiryDate;

            public Entry()
            {
            }

            public Entry(Entry src)
            {
                Value = src.Value;
                ExpiryDate = new Date(src.ExpiryDate);
            }
        }

        public UGUI User = UGUI.Unknown;
        public UUID SessionID = UUID.Zero;
        public UUID SecureSessionID = UUID.Zero;
        public string ClientIPAddress = string.Empty;
        /* key is <protocol-association>/<variable-name> */
        public readonly Dictionary<string, Entry> DynamicData = new Dictionary<string, Entry>();
        public Date Timestamp = Date.Now;

        public UserSessionInfo()
        {
        }

        public UserSessionInfo(UserSessionInfo src)
        {
            User = new UGUI(src.User);
            SessionID = src.SessionID;
            ClientIPAddress = src.ClientIPAddress;
            SecureSessionID = src.SecureSessionID;
            foreach(KeyValuePair<string, Entry> kvp in src.DynamicData)
            {
                DynamicData.Add(kvp.Key, new Entry(kvp.Value));
            }
        }

        private static readonly Dictionary<KnownUserSessionInfoVariables, string> m_AssocDict = new Dictionary<KnownUserSessionInfoVariables, string>();
        private static readonly Dictionary<KnownUserSessionInfoVariables, string> m_NameDict = new Dictionary<KnownUserSessionInfoVariables, string>();

        private static void AddVar(KnownUserSessionInfoVariables varid, string assoc, string varname)
        {
            m_AssocDict.Add(varid, assoc);
            m_NameDict.Add(varid, varname);
        }

        static UserSessionInfo()
        {
            AddVar(KnownUserSessionInfoVariables.LocationRegionID, "location", "name");
            AddVar(KnownUserSessionInfoVariables.LocationGridURI, "location", "grid-uri");
        }

        public string this[string assoc, string varname]
        {
            get
            {
                Entry entry = DynamicData[$"{assoc}/{varname}"];
                if (!TryGetValue(assoc, varname, out entry))
                {
                    throw new KeyNotFoundException();
                }
                return entry.Value;
            }
        }

        public string this[KnownUserSessionInfoVariables varid]
        {
            get
            {
                Entry value;
                if(!TryGetValue(varid, out value))
                {
                    throw new KeyNotFoundException();
                }
                return value.Value;
            }
        }

        public bool TryGetValue(string assoc, string varname, out Entry entry)
        {
            bool f = DynamicData.TryGetValue($"{assoc}/{varname}", out entry);
            if (f && entry.ExpiryDate != null && entry.ExpiryDate.AsULong < Date.Now.AsULong)
            {
                f = false;
                entry = default(Entry);
            }
            return f;
        }

        public bool TryGetValue(string assoc, string varname, out string val)
        {
            Entry entry;
            val = default(string);
            bool f = TryGetValue(assoc, varname, out entry);
            if (f)
            {
                val = entry.Value;
            }
            return f;
        }

        public bool ContainsKey(string assoc, string varname)
        {
            Entry entry;
            return TryGetValue(assoc, varname, out entry);
        }

        public bool TryGetValue(KnownUserSessionInfoVariables varid, out string val)
        {
            val = default(string);
            string assoc;
            string varname;
            return TryGetVarInfo(varid, out assoc, out varname) && TryGetValue(assoc, varname, out val);
        }

        public bool TryGetValue(KnownUserSessionInfoVariables varid, out UUID id)
        {
            id = default(UUID);
            string val;
            return TryGetValue(varid, out val) && UUID.TryParse(val, out id);
        }

        public bool TryGetValue(KnownUserSessionInfoVariables varid, out Entry val)
        {
            val = default(Entry);
            string assoc;
            string varname;
            return TryGetVarInfo(varid, out assoc, out varname) && TryGetValue(assoc, varname, out val);
        }

        public bool ContainsKey(KnownUserSessionInfoVariables varid)
        {
            string assoc;
            string varname;
            return TryGetVarInfo(varid, out assoc, out varname) && ContainsKey(assoc, varname);
        }

        public static bool TryGetVarInfo(KnownUserSessionInfoVariables varid, out string assoc, out string varname)
        {
            varname = default(string);
            return m_AssocDict.TryGetValue(varid, out assoc) && m_NameDict.TryGetValue(varid, out varname);
        }

        public static bool TryGetVarInfo(KnownUserSessionInfoVariables varid, out string fullvarname)
        {
            fullvarname = default(string);
            string assoc;
            string varname;
            bool r = TryGetVarInfo(varid, out assoc, out varname);
            if(r)
            {
                fullvarname = $"{assoc}/{varname}";
            }
            return r;
        }
    }
}
