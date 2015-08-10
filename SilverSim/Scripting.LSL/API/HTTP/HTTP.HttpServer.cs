// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;

namespace SilverSim.Scripting.LSL.API.HTTP
{
    public partial class HTTP_API
    {
        [APILevel(APIFlags.LSL)]
        public LSLKey llRequestURL(ScriptInstance Instance)
        {
            lock(Instance)
            {
                UUID reqID = UUID.Random;
                try
                {
                    UUID urlID = m_HTTPHandler.RequestURL(Instance.Part, Instance.Item);
                    HttpRequestEvent ev = new HttpRequestEvent();
                    ev.RequestID = reqID;
                    ev.Method = URL_REQUEST_GRANTED;
                    ev.Body = "";
                    Instance.PostEvent(ev);
                }
                catch
                {
                    HttpRequestEvent ev = new HttpRequestEvent();
                    ev.RequestID = reqID;
                    ev.Method = URL_REQUEST_DENIED;
                    ev.Body = "";
                    Instance.PostEvent(ev);
                }
                return reqID;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llReleaseURL(ScriptInstance Instance, string url)
        {
            lock (Instance)
            {
                m_HTTPHandler.ReleaseURL(url);
            }
        }

        [APILevel(APIFlags.LSL)]
        public LSLKey llRequestSecureURL(ScriptInstance Instance)
        {
            lock (Instance)
            {
                UUID reqID = UUID.Random;
                try
                {
                    UUID urlID = m_HTTPHandler.RequestSecureURL(Instance.Part, Instance.Item);
                    HttpRequestEvent ev = new HttpRequestEvent();
                    ev.RequestID = reqID;
                    ev.Method = URL_REQUEST_GRANTED;
                    ev.Body = "https://" + m_HTTPHandler;
                    Instance.PostEvent(ev);
                }
                catch
                {
                    HttpRequestEvent ev = new HttpRequestEvent();
                    ev.RequestID = reqID;
                    ev.Method = URL_REQUEST_DENIED;
                    ev.Body = "";
                    Instance.PostEvent(ev);
                }
                return reqID;
            }
        }

        [APILevel(APIFlags.LSL)]
        public string llGetHTTPHeader(ScriptInstance Instance, LSLKey requestID, string header)
        {
            lock (Instance)
            {
                return m_HTTPHandler.GetHttpHeader(requestID, header);
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llHTTPResponse(ScriptInstance Instance, LSLKey requestID, int status, string body)
        {
            lock(Instance)
            {
                m_HTTPHandler.HttpResponse(requestID, status, body);
            }
        }

        [APILevel(APIFlags.LSL)]
        public const int CONTENT_TYPE_TEXT = 0;
        [APILevel(APIFlags.LSL)]
        public const int CONTENT_TYPE_HTML = 1;
        [APILevel(APIFlags.LSL)]
        public const int CONTENT_TYPE_XML = 2;
        [APILevel(APIFlags.LSL)]
        public const int CONTENT_TYPE_XHTML = 3;
        [APILevel(APIFlags.LSL)]
        public const int CONTENT_TYPE_ATOM = 4;
        [APILevel(APIFlags.LSL)]
        public const int CONTENT_TYPE_JSON = 5;
        [APILevel(APIFlags.LSL)]
        public const int CONTENT_TYPE_LLSD = 6;
        [APILevel(APIFlags.LSL)]
        public const int CONTENT_TYPE_FORM = 7;
        [APILevel(APIFlags.LSL)]
        public const int CONTENT_TYPE_RSS = 8;

        [APILevel(APIFlags.LSL)]
        public void llSetContentType(ScriptInstance Instance, LSLKey requestID, int contenttype)
        {
            lock(Instance)
            {
                switch(contenttype)
                {
                    case CONTENT_TYPE_TEXT: m_HTTPHandler.SetContentType(requestID, "text/plain"); break;
                    case CONTENT_TYPE_HTML: m_HTTPHandler.SetContentType(requestID, "text/html"); break;
                    case CONTENT_TYPE_XML: m_HTTPHandler.SetContentType(requestID, "application/xml"); break;
                    case CONTENT_TYPE_XHTML: m_HTTPHandler.SetContentType(requestID, "application/xhtml+xml"); break;
                    case CONTENT_TYPE_ATOM: m_HTTPHandler.SetContentType(requestID, "application/atom+xml"); break;
                    case CONTENT_TYPE_JSON: m_HTTPHandler.SetContentType(requestID, "application/json"); break;
                    case CONTENT_TYPE_LLSD: m_HTTPHandler.SetContentType(requestID, "application/llsd+xml"); break;
                    case CONTENT_TYPE_FORM: m_HTTPHandler.SetContentType(requestID, "application/x-www-form-urlencoded "); break;
                    case CONTENT_TYPE_RSS: m_HTTPHandler.SetContentType(requestID, "application/rss+xml "); break;
                    default: m_HTTPHandler.SetContentType(requestID, "text/plain"); break;
                }
            }
        }

        [APILevel(APIFlags.OSSL)]
        public void osSetContentType(ScriptInstance Instance, LSLKey id, string type)
        {
            lock(Instance)
            {
                m_HTTPHandler.SetContentType(id, type);
            }
        }

        [ExecutedOnScriptReset]
        public static void ResetURLs(ScriptInstance Instance)
        {
            Script script = (Script)Instance;
            lock (script)
            {
#warning Implement ResetURLs
            }
        }
    }
}
