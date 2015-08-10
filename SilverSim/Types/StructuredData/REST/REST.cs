// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;
using System.IO;
using System.Web;

namespace SilverSim.Types.StructuredData.REST
{
    public static class REST
    {
        public static Dictionary<string, object> parseREST(Stream input)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            StreamReader sr = new StreamReader(input);
            string body = sr.ReadToEnd().Trim();
            string[] queryterms = body.Split('&');

            if(queryterms.Length == 0)
            {
                return result;
            }

            foreach (string term in queryterms)
            {
                string[] termparts = term.Split('=');

                string name = HttpUtility.UrlDecode(termparts[0]);
                string value = string.Empty;
                if (termparts.Length > 1)
                {
                    value = HttpUtility.UrlDecode(termparts[1]);
                }

                if(name.EndsWith("[]"))
                {
                    /* handle list based entries */
                    string baseName = name.Substring(0, name.Length - 2);
                    if(!result.ContainsKey(baseName))
                    {
                        List<string> newList = new List<string>();
                        newList.Add(value);
                        result.Add(baseName, newList);
                    }
                    /* we have to check whether we have a request that has been mixed up with no list and list entries resulting into same name */
                    else if (result[baseName] is List<string>)
                    {
                        List<string> l = (List<string>)result[baseName];
                        l.Add(value);
                    }

                }
                else if(!result.ContainsKey(name))
                {
                    /* plain string handling */
                    result[name] = value;
                }
            }

            return result;
        }
    }
}
