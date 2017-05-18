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
using System.IO;
using System.Web;

namespace SilverSim.Types.StructuredData.REST
{
    public static class REST
    {
        public static Dictionary<string, object> ParseREST(Stream input)
        {
            var result = new Dictionary<string, object>();

            string body;
            using (var sr = new StreamReader(input))
            {
                 body = sr.ReadToEnd().Trim();
            }
            var queryterms = body.Split('&');

            if (queryterms.Length == 0)
            {
                return result;
            }

            foreach (var term in queryterms)
            {
                var termparts = term.Split('=');

                var name = HttpUtility.UrlDecode(termparts[0]);
                var value = string.Empty;
                if (termparts.Length > 1)
                {
                    value = HttpUtility.UrlDecode(termparts[1]);
                }

                if (name.EndsWith("[]"))
                {
                    /* handle list based entries */
                    var baseName = name.Substring(0, name.Length - 2);
                    if (!result.ContainsKey(baseName))
                    {
                        var newList = new List<string>
                        {
                            value
                        };
                        result.Add(baseName, newList);
                    }
                    /* we have to check whether we have a request that has been mixed up with no list and list entries resulting into same name */
                    else if (result[baseName] is List<string>)
                    {
                        var l = (List<string>)result[baseName];
                        l.Add(value);
                    }
                }
                else if (!result.ContainsKey(name))
                {
                    /* plain string handling */
                    result[name] = value;
                }
            }

            return result;
        }
    }
}
