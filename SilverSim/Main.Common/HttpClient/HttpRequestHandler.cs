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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Web;

namespace SilverSim.Main.Common.HttpClient
{
    public static partial class HttpRequestHandler
    {
        private static string BuildQueryString(IDictionary<string, string> parameters)
        {
            List<string> items = new List<string>(parameters.Count);
            string outStr = string.Empty;
            foreach(KeyValuePair<string, string> kvp in parameters)
            {
                if(outStr != string.Empty)
                {
                    outStr += "&";
                }

                string[] names = kvp.Key.Split('?');
                outStr += HttpUtility.UrlEncode(names[0]) + "=" + HttpUtility.UrlEncode(kvp.Value);
            }

            return outStr;
        }

        #region Synchronous calls
        /**********************************************************************/
        /* synchronous calls */
        public static Stream DoStreamGetRequest(string url, IDictionary<string, string> getValues, int timeoutms)
        {
            return DoStreamRequest("GET", url, getValues, string.Empty, string.Empty, false, timeoutms);
        }

        /*---------------------------------------------------------------------*/
        public static Stream DoStreamHeadRequest(string url, IDictionary<string, string> getvalues, int timeoutms)
        {
            return DoStreamRequest("HEAD", url, getvalues, string.Empty, string.Empty, false, timeoutms);
        }

        /*---------------------------------------------------------------------*/
        public static Stream DoStreamPostRequest(string url, IDictionary<string, string> getValues, IDictionary<string, string> postValues, bool compressed, int timeoutms)
        {
            string post = BuildQueryString(postValues);
            return DoStreamRequest("POST", url, getValues, "application/x-www-form-urlencoded", post, compressed, timeoutms);
        }

        /**********************************************************************/
        /* synchronous calls */
        public static string DoGetRequest(string url, IDictionary<string, string> getValues, int timeoutms)
        {
            return DoRequest("GET", url, getValues, string.Empty, string.Empty, false, timeoutms);
        }

        /*---------------------------------------------------------------------*/
        public static string DoHeadRequest(string url, IDictionary<string, string> getvalues, int timeoutms)
        {
            return DoRequest("HEAD", url, getvalues, string.Empty, string.Empty, false, timeoutms);
        }

        /*---------------------------------------------------------------------*/
        public static string DoPostRequest(string url, IDictionary<string, string> getValues, IDictionary<string, string> postValues, bool compressed, int timeoutms)
        {
            string post = BuildQueryString(postValues);
            return DoRequest("POST", url, getValues, "application/x-www-form-urlencoded", post, compressed, timeoutms);
        }

        /*---------------------------------------------------------------------*/
        public static string DoRequest(string method, string url, IDictionary<string, string> getValues, string content_type, string post, bool compressed, int timeoutms)
        {
            using (Stream responseStream = DoStreamRequest(method, url, getValues, content_type, post, compressed, timeoutms))
            {
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /*---------------------------------------------------------------------*/
        public static string DoRequest(string method, string url, IDictionary<string, string> getValues, string content_type, int content_length, StreamPostDelegate postdelegate, bool compressed, int timeoutms)
        {
            using (Stream responseStream = DoStreamRequest(method, url, getValues, content_type, content_length, postdelegate, compressed, timeoutms))
            {
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        #endregion
    }
}