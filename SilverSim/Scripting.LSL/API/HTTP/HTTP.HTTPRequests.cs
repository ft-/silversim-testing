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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Scene.Types.Script;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scene.Types.Object;
using System.Runtime.Remoting.Messaging;
using System.Text.RegularExpressions;

namespace SilverSim.Scripting.LSL.API.HTTP
{
    public partial class HTTP_API
    {
        [APILevel(APIFlags.LSL)]
        public const int HTTP_METHOD = 0;
        [APILevel(APIFlags.LSL)]
        public const int HTTP_MIMETYPE = 1;
        [APILevel(APIFlags.LSL)]
        public const int HTTP_BODY_MAXLENGTH = 2;
        [APILevel(APIFlags.LSL)]
        public const int HTTP_VERIFY_CERT = 3;
        [APILevel(APIFlags.LSL)]
        public const int HTTP_VERBOSE_THROTTLE = 4;
        [APILevel(APIFlags.LSL)]
        public const int HTTP_CUSTOM_HEADER = 5;
        [APILevel(APIFlags.LSL)]
        public const int HTTP_PRAGMA_NO_CACHE = 6;

        private string[] m_AllowedHttpHeaders =
        {
            "Accept", "Accept-Charset", "Accept-Encoding", "Accept-Language",
            "Accept-Ranges", "Age", "Allow", "Authorization", "Cache-Control",
            "Connection", "Content-Encoding", "Content-Language",
            "Content-Length", "Content-Location", "Content-MD5",
            "Content-Range", "Content-Type", "Date", "ETag", "Expect",
            "Expires", "From", "Host", "If-Match", "If-Modified-Since",
            "If-None-Match", "If-Range", "If-Unmodified-Since", "Last-Modified",
            "Location", "Max-Forwards", "Pragma", "Proxy-Authenticate",
            "Proxy-Authorization", "Range", "Referer", "Retry-After", "Server",
            "TE", "Trailer", "Transfer-Encoding", "Upgrade", "User-Agent",
            "Vary", "Via", "Warning", "WWW-Authenticate"
        };

        delegate void HttpRequestDelegate(ObjectPart part, UUID requestID, string method, string url, int maxBodyLength, Dictionary<string, string> httpHeaders, string body);
        void httpRequest(ObjectPart part, UUID requestID, string method, string url, int maxBodyLength, Dictionary<string, string> httpHeaders, string body)
        {
#warning Implement httpRequest
            HttpResponseEvent e = new HttpResponseEvent();
            e.RequestID = UUID.Random;
            e.Status = 499;
            part.PostEvent(e);
        }

        void httpRequestEnd(IAsyncResult ar)
        {
            AsyncResult r = (AsyncResult)ar;
            HttpRequestDelegate caller = (HttpRequestDelegate)r.AsyncDelegate;
            caller.EndInvoke(ar);
        }

        static readonly Regex m_AuthRegex = new Regex(@"^(https?:\/\/)(\w+):(\w+)@(.*)$");
        static readonly Encoding UTF8NoBOM = new UTF8Encoding(false);

        [APILevel(APIFlags.LSL)]
        public UUID llHTTPRequest(ScriptInstance Instance, string url, AnArray parameters, string body)
        {
            LSLHTTPClient_RequestQueue.LSLHttpRequest req = new LSLHTTPClient_RequestQueue.LSLHttpRequest();
            lock (Instance)
            {
                req.SceneID = Instance.Part.ObjectGroup.Scene.ID;
                req.PrimID = Instance.Part.ID;
                req.ItemID = Instance.Item.ID;
            }

            if (url.Contains(' '))
            {
                lock (Instance)
                {
                    HttpResponseEvent e = new HttpResponseEvent();
                    e.RequestID = UUID.Random;
                    e.Status = 499;
                    Instance.Part.PostEvent(e);
                    return e.RequestID;
                }
            }

            for (int i = 0; i < parameters.Count; ++i)
            {
                switch(parameters[i].AsInt)
                {
                    case HTTP_METHOD:
                        if(i + 1 >= parameters.Count)
                        {
                            lock(Instance)
                            {
                                Instance.ShoutError("Missing parameter for HTTP_METHOD");
                                return UUID.Zero;
                            }
                        }

                        req.Method = parameters[++i].ToString();
                        break;

                    case HTTP_MIMETYPE:
                        if(i + 1 >= parameters.Count)
                        {
                            lock(Instance)
                            {
                                Instance.ShoutError("Missing parameter for HTTP_MIMEYPE");
                                return UUID.Zero;
                            }
                        }

                        req.MimeType = parameters[++i].ToString();
                        break;

                    case HTTP_BODY_MAXLENGTH:
                        if(i + 1 >= parameters.Count)
                        {
                            lock(Instance)
                            {
                                Instance.ShoutError("Missing parameter for HTTP_METHOD");
                                return UUID.Zero;
                            }
                        }

                        req.MaxBodyLength = parameters[++i].AsInt;
                        break;

                    case HTTP_VERIFY_CERT:
                        if(i + 1 >= parameters.Count)
                        {
                            lock(Instance)
                            {
                                Instance.ShoutError("Missing parameter for HTTP_VERIFY_CERT");
                                return UUID.Zero;
                            }
                        }

                        req.VerifyCert = parameters[++i].AsBoolean;
                        break;

                    case HTTP_VERBOSE_THROTTLE:
                        if(i + 1 >= parameters.Count)
                        {
                            lock(Instance)
                            {
                                Instance.ShoutError("Missing parameter for HTTP_VERBOSE_THROTTLE");
                                return UUID.Zero;
                            }
                        }

                        req.VerboseThrottle = parameters[++i].AsBoolean;
                        break;

                    case HTTP_CUSTOM_HEADER:
                        if(i + 2 >= parameters.Count)
                        {
                            lock(Instance)
                            {
                                Instance.ShoutError("Missing parameter for HTTP_CUSTOM_HEADER");
                                return UUID.Zero;
                            }
                        }

                        string name = parameters[++i].ToString();
                        string value = parameters[++i].ToString();

                        if (!m_AllowedHttpHeaders.Contains(name))
                        {
                            Instance.ShoutError(string.Format("Custom header {0} not allowed", name));
                            return UUID.Zero;
                        }
                        try
                        {
                            req.Headers.Add(name, value);
                        }
                        catch
                        {
                            Instance.ShoutError(string.Format("Custom header {0} already defined", name));
                            return UUID.Zero;
                        }
                        break;

                    case HTTP_PRAGMA_NO_CACHE:
                        if(i + 1 >= parameters.Count)
                        {
                            lock(Instance)
                            {
                                Instance.ShoutError("Missing parameter for HTTP_PRAGMA_NO_CACHE");
                                return UUID.Zero;
                            }
                        }

                        req.SendPragmaNoCache = parameters[++i].AsBoolean;
                        break;

                    default:
                        lock(Instance)
                        {
                            Instance.ShoutError(string.Format("Unknown parameter {0} for llHTTPRequest", parameters[i].AsInt));
                            return UUID.Zero;
                        }
                }
                
            }

            lock (Instance)
            {
                req.Headers.Add("X-SecondLife-Object-Name", Instance.Part.ObjectGroup.Name);
                req.Headers.Add("X-SecondLife-Object-Key", Instance.Part.ObjectGroup.ID);
                req.Headers.Add("X-SecondLife-Region", Instance.Part.ObjectGroup.Scene.RegionData.Name);
                req.Headers.Add("X-SecondLife-Local-Position", string.Format("({0:0.000000}, {1:0.000000}, {2:0.000000})", Instance.Part.ObjectGroup.GlobalPosition.X, Instance.Part.ObjectGroup.GlobalPosition.Y, Instance.Part.ObjectGroup.GlobalPosition.Z));
                req.Headers.Add("X-SecondLife-Local-Velocity", string.Format("({0:0.000000}, {1:0.000000}, {2:0.000000})", Instance.Part.ObjectGroup.Velocity.X, Instance.Part.ObjectGroup.Velocity.Y, Instance.Part.ObjectGroup.Velocity.Z));
                req.Headers.Add("X-SecondLife-Local-Rotation", string.Format("({0:0.000000}, {1:0.000000}, {2:0.000000}, {3:0.000000})", Instance.Part.ObjectGroup.GlobalRotation.X, Instance.Part.ObjectGroup.GlobalRotation.Y, Instance.Part.ObjectGroup.GlobalRotation.Z, Instance.Part.ObjectGroup.GlobalRotation.W));
                req.Headers.Add("X-SecondLife-Owner-Name", Instance.Part.ObjectGroup.Owner.FullName);
                req.Headers.Add("X-SecondLife-Owner-Key", Instance.Part.ObjectGroup.Owner.ID);

                Match authMatch = m_AuthRegex.Match(url);
                if(authMatch.Success)
                {
                    if(authMatch.Groups.Count == 5)
                    {
                        string authData = string.Format("{0}:{1}", authMatch.Groups[2].ToString(), authMatch.Groups[3].ToString());
                        byte[] authDataBinary = UTF8NoBOM.GetBytes(authData);
                        req.Headers.Add("Authorization", string.Format("Basic {0}", Convert.ToBase64String(authDataBinary)));
                    }
                }

                if(m_LSLHTTPClient.Enqueue(req))
                {
                    return req.RequestID;
                }
                else
                {
                    return UUID.Zero;
                }
            }
        }
    }
}
