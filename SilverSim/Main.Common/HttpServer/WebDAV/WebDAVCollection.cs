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
using System.Net;
using System.Web;
using System.Xml;

namespace SilverSim.Main.Common.HttpServer.WebDAV
{
    public abstract class WebDAVCollection : WebDAVResource
    {
        public WebDAVCollection(string resourcename)
        {
            ResourceName = resourcename;
        }
        public string ResourceName { get; }

        public abstract string DisplayName { get; }

        public abstract WebDAVResource GetResource(string pathseg);

        public virtual void HandlePut(HttpRequest req, string name)
        {
            req.ErrorResponse(HttpStatusCode.Forbidden);
        }

        public virtual void HandleLock(HttpRequest req)
        {
            req.ErrorResponse(HttpStatusCode.MethodNotAllowed);
        }

        public virtual void HandleUnlock(HttpRequest req)
        {
            req.ErrorResponse(HttpStatusCode.MethodNotAllowed);
        }

        public virtual void HandleCopy(HttpRequest req)
        {
            req.ErrorResponse(HttpStatusCode.Forbidden);
        }

        public virtual void HandleDelete(HttpRequest req)
        {
            req.ErrorResponse(HttpStatusCode.Forbidden);
        }

        public virtual void HandleGetHead(HttpRequest req)
        {
            req.ErrorResponse(HttpStatusCode.Forbidden);
        }

        public abstract void HandleMkcol(HttpRequest req, string name);

        public virtual void HandleMove(HttpRequest req)
        {
            req.ErrorResponse(HttpStatusCode.Forbidden);
        }

        public virtual void HandlePut(HttpRequest req)
        {
            req.ErrorResponse(HttpStatusCode.MethodNotAllowed);
        }

        public virtual void SetProperty(string prefix, string localname, XmlElement fragment)
        {
            throw new HttpException((int)HttpStatusCode.Forbidden, "Forbidden");
        }

        public virtual XmlElement GetProperty(string prefix, string localname, XmlDocument xmlDocument)
        {
            throw new HttpException((int)HttpStatusCode.NotFound, "Not Found");
        }

        public WebDAVResource[] Children { get; }
    }
}
