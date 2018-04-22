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

using log4net;
using SilverSim.Types;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Xml;

namespace SilverSim.Main.Common.HttpServer.WebDAV
{
    public sealed class WebDAVServer
    {
        private static readonly ILog m_Log = LogManager.GetLogger("WEBDAV SERVER");
        private readonly WebDAVCollection m_RootFolder;
        private readonly string m_BasePath;

        public WebDAVServer(WebDAVCollection rootFolder, string basepath)
        {
            m_BasePath = basepath.TrimEnd(new char[] { '/' });

            if(rootFolder == null)
            {
                throw new ArgumentNullException(nameof(rootFolder));
            }
            m_RootFolder = rootFolder;
        }

        private WebDAVResource GetResource(string[] pathcomps)
        {
            WebDAVResource res = m_RootFolder;
            foreach(string pathcomp in pathcomps)
            {
                res = res.GetResource(Uri.UnescapeDataString(pathcomp));
            }

            return res;
        }

        private WebDAVResource GetResourceTo(string[] pathcomps, out string last)
        {
            WebDAVResource res = m_RootFolder;
            last = Uri.UnescapeDataString(pathcomps[pathcomps.Length - 1]);
            for(int i = 0; i < pathcomps.Length - 1; ++i)
            {
                res = res.GetResource(Uri.UnescapeDataString(pathcomps[i]));
            }

            return res;
        }

        public void HandleWebDAVRequest(HttpRequest req)
        {
            string url = req.RawUrl;
            if(!url.StartsWith(m_BasePath))
            {
                req.ErrorResponse(HttpStatusCode.NotFound);
                return;
            }
            url = url.Substring(m_BasePath.Length);
            if(url == "/")
            {
                url = string.Empty; /* root collection */
            }
            else if(!url.StartsWith("/"))
            {
                req.ErrorResponse(HttpStatusCode.NotFound);
                return;
            }
            else
            {
                url = url.Substring(1);
            }

            switch(req.Method)
            {
                case "GET": case "HEAD":
                    if(url.Length == 0)
                    {
                        m_RootFolder.HandleGetHead(req);
                    }
                    break;

                case "PUT":
                    {
                        WebDAVResource resource;
                        string name;
                        if (url.Length == 0)
                        {
                            req.ErrorResponse(HttpStatusCode.Conflict);
                            return;
                        }
                        else
                        {
                            try
                            {
                                resource = GetResourceTo(url.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries), out name);
                            }
                            catch (WebDAVResourceNotFoundException)
                            {
                                req.ErrorResponse(HttpStatusCode.NotFound);
                                return;
                            }
                        }
                        resource.HandlePut(req, name);
                    }
                    break;

                case "DELETE":
                    if(url.Length == 0)
                    {
                        req.ErrorResponse(HttpStatusCode.Forbidden);
                    }
                    else
                    {
                        WebDAVResource resource;
                        try
                        {
                            resource = GetResource(url.Split('/'));
                        }
                        catch(WebDAVResourceNotFoundException)
                        {
                            req.ErrorResponse(HttpStatusCode.NotFound);
                            return;
                        }
                        resource.HandleDelete(req);
                    }
                    break;

                case "LOCK":
                    {
                        WebDAVResource resource;
                        if (url.Length == 0)
                        {
                            resource = m_RootFolder;
                        }
                        else
                        {
                            try
                            {
                                resource = GetResource(url.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
                            }
                            catch (WebDAVResourceNotFoundException)
                            {
                                req.ErrorResponse(HttpStatusCode.NotFound);
                                return;
                            }
                        }
                        resource.HandleLock(req);
                    }
                    break;

                case "UNLOCK":
                    {
                        WebDAVResource resource;
                        if (url.Length == 0)
                        {
                            resource = m_RootFolder;
                        }
                        else
                        {
                            try
                            {
                                resource = GetResource(url.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
                            }
                            catch (WebDAVResourceNotFoundException)
                            {
                                req.ErrorResponse(HttpStatusCode.NotFound);
                                return;
                            }
                        }
                        resource.HandleUnlock(req);
                    }
                    break;

                case "MKCOL":
                    {
                        WebDAVResource resource;
                        string name;
                        if (url.Length == 0)
                        {
                            req.ErrorResponse(HttpStatusCode.Conflict);
                            return;
                        }
                        else
                        {
                            try
                            {
                                resource = GetResourceTo(url.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries), out name);
                            }
                            catch (WebDAVResourceNotFoundException)
                            {
                                req.ErrorResponse(HttpStatusCode.NotFound);
                                return;
                            }
                        }
                        resource.HandleMkcol(req, name);
                    }
                    break;

                case "PROPFIND":
                    {
                        WebDAVResource resource;
                        if (url.Length == 0)
                        {
                            resource = m_RootFolder;
                        }
                        else
                        {
                            try
                            {
                                resource = GetResource(url.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
                            }
                            catch (WebDAVResourceNotFoundException)
                            {
                                req.ErrorResponse(HttpStatusCode.NotFound);
                                return;
                            }
                        }
                        HandlePropFind(resource, req);
                    }
                    break;

                case "PROPPATCH":
                    {
                        WebDAVResource resource;
                        if (url.Length == 0)
                        {
                            resource = m_RootFolder;
                        }
                        else
                        {
                            try
                            {
                                resource = GetResource(url.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
                            }
                            catch (WebDAVResourceNotFoundException)
                            {
                                req.ErrorResponse(HttpStatusCode.NotFound);
                                return;
                            }
                        }
                        HandlePropPatch(resource, req);
                    }
                    break;

                case "COPY":
                    if (url.Length == 0)
                    {
                        req.ErrorResponse(HttpStatusCode.Forbidden);
                    }
                    else
                    {
                        WebDAVResource resource;
                        try
                        {
                            resource = GetResource(url.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
                        }
                        catch (WebDAVResourceNotFoundException)
                        {
                            req.ErrorResponse(HttpStatusCode.NotFound);
                            return;
                        }
                        resource.HandleCopy(req);
                    }
                    break;

                case "MOVE":
                    if (url.Length == 0)
                    {
                        req.ErrorResponse(HttpStatusCode.Forbidden);
                    }
                    else
                    {
                        WebDAVResource resource;
                        try
                        {
                            resource = GetResource(url.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
                        }
                        catch (WebDAVResourceNotFoundException)
                        {
                            req.ErrorResponse(HttpStatusCode.NotFound);
                            return;
                        }
                        resource.HandleMove(req);
                    }
                    break;

                default:
                    req.ErrorResponse(HttpStatusCode.MethodNotAllowed);
                    break;
            }
        }

        private void HandlePropFind(WebDAVResource resource, HttpRequest req)
        {
            var doc = new XmlDocument
            {
                XmlResolver = null
            };
            using (Stream s = req.Body)
            {
                doc.Load(s);
            }

            if(doc.DocumentElement.Name != "propfind")
            {
                req.ErrorResponse(HttpStatusCode.BadRequest);
                return;
            }

            int depth;
            string depthstr;
            if (req.TryGetHeader("depth", out depthstr))
            {
                depth = depthstr == "infinity" ? int.MaxValue : int.Parse(depthstr);
            }
            else
            {
                depth = 1;
            }

            var prop = (XmlElement)doc.DocumentElement.GetElementsByTagName("prop")[0];
            var resDoc = new XmlDocument
            {
                XmlResolver = null
            };
            XmlElement elem = resDoc.CreateElement("DAV", "multistatus");
            resDoc.AppendChild(elem);
            elem.AppendChild(PropFindProcess(req.RawUrl, resource, resDoc, prop));

            if(depth > 0 && resource is WebDAVCollection)
            {
                string childUrl = req.RawUrl;
                if(!childUrl.EndsWith("/"))
                {
                    childUrl += "/";
                }

                foreach (WebDAVResource child in ((WebDAVCollection)resource).Children)
                {
                    elem.AppendChild(PropFindProcess(childUrl + Uri.EscapeDataString(child.ResourceName), resource, resDoc, prop));
                }
            }

            using (HttpResponse res = req.BeginResponse("application/xml"))
            using (Stream s = res.GetOutputStream())
            using (XmlTextWriter writer = s.UTF8XmlTextWriter())
            {
                resDoc.WriteTo(writer);
            }
        }

        private XmlElement PropFindProcess(string rawurl, WebDAVResource resource, XmlDocument resDoc, XmlElement reqprop)
        {
            XmlElement responseElem = resDoc.CreateElement("DAV", "response");
            XmlElement hrefElem = resDoc.CreateElement("DAV", "href");
            hrefElem.InnerText = rawurl;
            responseElem.AppendChild(hrefElem);
            XmlElement propStatElem = resDoc.CreateElement("DAV", "propstat");
            responseElem.AppendChild(propStatElem);

            foreach (XmlElement srcelem in reqprop.ChildNodes.OfType<XmlElement>())
            {
                XmlElement propElem = resDoc.CreateElement("DAV", "prop");
                propStatElem.AppendChild(propElem);
                try
                {
                    XmlElement propData = resource.GetProperty(srcelem.Prefix, srcelem.LocalName, resDoc);
                    propElem.AppendChild(propData);
                    XmlElement status = resDoc.CreateElement("DAV", "status");
                    status.InnerText = "HTTP/1.1 200 OK";
                    propStatElem.AppendChild(status);
                }
                catch (HttpException e)
                {
                    XmlElement propData = resDoc.CreateElement(srcelem.Prefix, srcelem.LocalName);
                    propElem.AppendChild(propData);
                    XmlElement status = resDoc.CreateElement("DAV", "status");
                    status.InnerText = $"HTTP/1.1 {e.GetHttpCode()} {e.Message}";
                    propStatElem.AppendChild(status);
                }
                catch (Exception)
                {
                    XmlElement propData = resDoc.CreateElement(srcelem.Prefix, srcelem.LocalName);
                    propElem.AppendChild(propData);
                    XmlElement status = resDoc.CreateElement("DAV", "status");
                    status.InnerText = "HTTP/1.1 500 Internal Server Error";
                    propStatElem.AppendChild(status);
                }
            }
            return responseElem;
        }

        private void HandlePropPatch(WebDAVResource resource, HttpRequest req)
        {
            var doc = new XmlDocument
            {
                XmlResolver = null
            };
            using (Stream s = req.Body)
            {
                doc.Load(s);
            }

            if (doc.DocumentElement.LocalName != "propertyupdate")
            {
                req.ErrorResponse(HttpStatusCode.BadRequest);
                return;
            }

            var prop = (XmlElement)doc.DocumentElement.GetElementsByTagName("set")[0];
            prop = (XmlElement)prop.GetElementsByTagName("prop")[0];
            var resDoc = new XmlDocument
            {
                XmlResolver = null
            };
            XmlElement elem = resDoc.CreateElement("DAV", "multistatus");
            resDoc.AppendChild(elem);
            XmlElement responseElem = resDoc.CreateElement("DAV", "response");
            elem.AppendChild(responseElem);
            XmlElement hrefElem = resDoc.CreateElement("DAV", "href");
            hrefElem.InnerText = "";
            responseElem.AppendChild(hrefElem);
            XmlElement propStatElem = resDoc.CreateElement("DAV", "propstat");
            responseElem.AppendChild(propStatElem);

            foreach (XmlElement srcelem in prop.ChildNodes.OfType<XmlElement>())
            {
                XmlElement propElem = resDoc.CreateElement("DAV", "prop");
                propStatElem.AppendChild(propElem);
                try
                {
                    resource.SetProperty(srcelem.Prefix, srcelem.LocalName, srcelem);
                    XmlElement status = resDoc.CreateElement("DAV", "status");
                    status.InnerText = "HTTP/1.1 200 OK";
                    propStatElem.AppendChild(status);
                }
                catch (HttpException e)
                {
                    XmlElement propData = resDoc.CreateElement(srcelem.Prefix, srcelem.LocalName);
                    propElem.AppendChild(propData);
                    XmlElement status = resDoc.CreateElement("DAV", "status");
                    status.InnerText = $"HTTP/1.1 {e.GetHttpCode()} {e.Message}";
                    propStatElem.AppendChild(status);
                }
                catch (Exception)
                {
                    XmlElement propData = resDoc.CreateElement(srcelem.Prefix, srcelem.LocalName);
                    propElem.AppendChild(propData);
                    XmlElement status = resDoc.CreateElement("DAV", "status");
                    status.InnerText = "HTTP/1.1 500 Internal Server Error";
                    propStatElem.AppendChild(status);
                }
            }

            using (HttpResponse res = req.BeginResponse("application/xml"))
            using (Stream s = res.GetOutputStream())
            using (XmlTextWriter writer = s.UTF8XmlTextWriter())
            {
                resDoc.WriteTo(writer);
            }
        }
    }
}
