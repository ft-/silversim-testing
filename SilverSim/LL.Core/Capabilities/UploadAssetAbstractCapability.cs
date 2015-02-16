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

using SilverSim.Main.Common.HttpServer;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.IO;
using System.Net;

namespace SilverSim.LL.Core.Capabilities
{
    public abstract class UploadAssetAbstractCapability : ICapabilityInterface
    {
        public abstract string CapabilityName { get; }
        protected UUI m_Creator { get; private set; }
        protected string m_ServerURI;

        public UploadAssetAbstractCapability(UUI creator, string serverURI)
        {
            m_Creator = creator;
            m_ServerURI = serverURI;
        }

        public abstract UUID GetUploaderID(Map reqmap);
        public abstract Map UploadedData(UUID transactionID, AssetData data);
        protected abstract UUID NewAssetID { get; }
        protected abstract bool AssetIsLocal { get; }
        protected abstract bool AssetIsTemporary { get; }
        protected abstract AssetType NewAssetType { get; }
        public abstract int ActiveUploads { get; }

        protected class UrlNotFoundException : Exception
        {
            public UrlNotFoundException()
            {

            }
        }

        protected class InsufficientFundsException : Exception
        {
            public InsufficientFundsException()
            {

            }
        }

        protected class UploadErrorException : Exception
        {
            public UploadErrorException(string message)
                : base(message)
            {

            }
        }

        public void HttpRequestHandler(HttpRequest httpreq)
        {
            UUID transactionID;
            if (httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            string[] parts = httpreq.RawUrl.Substring(1).Split('/');
            if(parts.Length == 3)
            {
                UUID uploadID;
                IValue o;
                if(httpreq.ContentType != "application/llsd+xml")
                {
                    httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                    return;
                }
                try
                {
                    o = LLSD_XML.Deserialize(httpreq.Body);
                }
                catch
                {
                    httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                    return;
                }
                if (!(o is Map))
                {
                    httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                    return;
                }
                Map reqmap = (Map)o;

                try
                {
                    uploadID = GetUploaderID(reqmap);
                }
                catch(UploadErrorException e)
                {
                    Map llsderrorreply = new Map();
                    llsderrorreply.Add("state", "error");
                    llsderrorreply.Add("message", e.Message);

                    HttpResponse httperrorres = httpreq.BeginResponse();
                    Stream outErrorStream = httperrorres.GetOutputStream();
                    LLSD_XML.Serialize(llsderrorreply, outErrorStream);
                    httperrorres.Close();
                    return;
                }
                catch(InsufficientFundsException)
                {
                    Map llsderrorreply = new Map();
                    llsderrorreply.Add("state", "insufficient funds");

                    HttpResponse httperrorres = httpreq.BeginResponse();
                    Stream outErrorStream = httperrorres.GetOutputStream();
                    LLSD_XML.Serialize(llsderrorreply, outErrorStream);
                    httperrorres.Close();
                    return;
                }
                catch
                {
                    httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                    return;
                }
                /* Upload start */
                Map llsdreply = new Map();
                llsdreply.Add("state", "upload");
                llsdreply.Add("uploader", m_ServerURI + httpreq.RawUrl + "/Upload/" + uploadID.ToString());

                HttpResponse httpres = httpreq.BeginResponse();
                Stream outStream = httpres.GetOutputStream();
                LLSD_XML.Serialize(llsdreply, outStream);
                httpres.Close();
            }
            else if(parts[3] != "Upload" || parts.Length < 4)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
            }
            else if(!UUID.TryParse(parts[4], out transactionID))
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
            }
            else
            {
                /* Upload finished */
                AssetData asset = new AssetData();
                Stream body = httpreq.Body;
                asset.Data = new byte[body.Length];
                int readBytes = body.Read(asset.Data, 0, (int)body.Length);
                if (body.Length != readBytes)
                {
                    httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                    return;
                }

                asset.Type = NewAssetType;
                asset.ID = NewAssetID;
                asset.Local = AssetIsLocal;
                asset.Temporary = AssetIsTemporary;
                asset.Name = "";
                asset.Creator = m_Creator;

                Map llsdreply;
                try
                {
                    llsdreply = UploadedData(transactionID, asset);
                }
                catch(UrlNotFoundException)
                {
                    httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                    return;
                }
                catch (UploadErrorException e)
                {
                    Map llsderrorreply = new Map();
                    llsderrorreply.Add("state", "error");
                    llsderrorreply.Add("message", e.Message);

                    HttpResponse httperrorres = httpreq.BeginResponse();
                    httperrorres.ContentType = "application/llsd+xml";
                    Stream outErrorStream = httperrorres.GetOutputStream();
                    LLSD_XML.Serialize(llsderrorreply, outErrorStream);
                    httperrorres.Close();
                    return;
                }
                catch (InsufficientFundsException)
                {
                    Map llsderrorreply = new Map();
                    llsderrorreply.Add("state", "insufficient funds");

                    HttpResponse httperrorres = httpreq.BeginResponse();
                    httperrorres.ContentType = "application/llsd+xml";
                    Stream outErrorStream = httperrorres.GetOutputStream();
                    LLSD_XML.Serialize(llsderrorreply, outErrorStream);
                    httperrorres.Close();
                    return;
                }
                catch
                {
                    httpreq.ErrorResponse(HttpStatusCode.InternalServerError, "Internal Server Error");
                    return;
                }

                llsdreply.Add("new_asset", asset.ID);
                llsdreply.Add("state", "complete");

                HttpResponse httpres = httpreq.BeginResponse();
                httpres.ContentType = "application/llsd+xml";
                Stream outStream = httpres.GetOutputStream();
                LLSD_XML.Serialize(llsdreply, outStream);
                httpres.Close();
            }
        }
    }
}
