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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;

namespace SilverSim.Http.Client
{
    public static partial class HttpClient
    {
        private static string BuildQueryString(IDictionary<string, string> parameters)
        {
            var outStr = new StringBuilder();
            foreach(KeyValuePair<string, string> kvp in parameters)
            {
                if(outStr.Length != 0)
                {
                    outStr.Append("&");
                }

                string[] names = kvp.Key.Split('?');
                outStr.Append(HttpUtility.UrlEncode(names[0]));
                outStr.Append("=");
                outStr.Append(HttpUtility.UrlEncode(kvp.Value));
            }

            return outStr.ToString();
        }

        #region Synchronous calls
        /**********************************************************************/
        /* synchronous calls */
        public static Stream DoStreamGetRequest(string url, IDictionary<string, string> getValues, int timeoutms) =>
            DoStreamRequest("GET", url, getValues, string.Empty, string.Empty, false, timeoutms);

        /*---------------------------------------------------------------------*/
        public static Stream DoStreamHeadRequest(string url, IDictionary<string, string> getvalues, int timeoutms) =>
            DoStreamRequest("HEAD", url, getvalues, string.Empty, string.Empty, false, timeoutms);

        /*---------------------------------------------------------------------*/
        public static Stream DoStreamPostRequest(string url, IDictionary<string, string> getValues, IDictionary<string, string> postValues, bool compressed, int timeoutms) =>
            DoStreamRequest("POST", url, getValues, "application/x-www-form-urlencoded", BuildQueryString(postValues), compressed, timeoutms);

        /**********************************************************************/
        /* synchronous calls */
        public static string DoGetRequest(string url, IDictionary<string, string> getValues, int timeoutms) =>
            DoRequest("GET", url, getValues, string.Empty, string.Empty, false, timeoutms);

        /*---------------------------------------------------------------------*/
        public static string DoHeadRequest(string url, IDictionary<string, string> getvalues, int timeoutms) =>
            DoRequest("HEAD", url, getvalues, string.Empty, string.Empty, false, timeoutms);

        /*---------------------------------------------------------------------*/
        public static string DoPostRequest(string url, IDictionary<string, string> getValues, IDictionary<string, string> postValues, bool compressed, int timeoutms) =>
            DoRequest("POST", url, getValues, "application/x-www-form-urlencoded", BuildQueryString(postValues), compressed, timeoutms);

        /*---------------------------------------------------------------------*/
        public static string DoRequest(string method, string url, IDictionary<string, string> getValues, string content_type, string post, bool compressed, int timeoutms)
        {
            using (var responseStream = DoStreamRequest(method, url, getValues, content_type, post, compressed, timeoutms))
            {
                using (var reader = new StreamReader(responseStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /*---------------------------------------------------------------------*/
        public static string DoRequest(string method, string url, IDictionary<string, string> getValues, string content_type, int content_length, Action<Stream> postdelegate, bool compressed, int timeoutms)
        {
            using (var responseStream = DoStreamRequest(method, url, getValues, content_type, content_length, postdelegate, compressed, timeoutms))
            {
                using (var reader = new StreamReader(responseStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        #endregion
    }
}