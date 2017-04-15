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

using SilverSim.Main.Common.HttpServer;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.IO;
using System.Net;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using log4net;

namespace SilverSim.Viewer.Core.Capabilities
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
    public abstract class UploadAssetAbstractCapability : ICapabilityInterface
    {
        public abstract string CapabilityName { get; }
        protected UUI m_Creator { get; private set; }
        protected string m_ServerURI;
        protected readonly string m_RemoteIP;

        public UploadAssetAbstractCapability(UUI creator, string serverURI, string remoteip)
        {
            m_Creator = creator;
            m_ServerURI = serverURI;
            m_RemoteIP = remoteip;
        }

        public abstract UUID GetUploaderID(Map reqmap);
        public abstract Map UploadedData(UUID transactionID, AssetData data);
        protected abstract UUID NewAssetID { get; }
        protected abstract bool AssetIsLocal { get; }
        protected abstract bool AssetIsTemporary { get; }
        protected abstract AssetType NewAssetType { get; }
        public abstract int ActiveUploads { get; }

        [Serializable]
        public class UrlNotFoundException : Exception
        {
            public UrlNotFoundException()
            {

            }

            public UrlNotFoundException(string message)
                : base(message)
            {

            }

            protected UrlNotFoundException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public UrlNotFoundException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }

        [Serializable]
        public class InsufficientFundsException : Exception
        {
            public InsufficientFundsException()
            {

            }

            public InsufficientFundsException(string message)
                : base(message)
            {

            }

            protected InsufficientFundsException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public InsufficientFundsException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }

        [Serializable]
        public class UploadErrorException : Exception
        {
            public UploadErrorException()
            {

            }

            public UploadErrorException(string message)
                : base(message)
            {

            }

            protected UploadErrorException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public UploadErrorException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }

        public void HttpRequestHandler(HttpRequest httpreq)
        {
            if (httpreq.CallerIP != m_RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
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
                if(httpreq.ContentType != "application/llsd+xml")
                {
                    httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                    return;
                }

                Map reqmap;
                try
                {
                    IValue iv = LlsdXml.Deserialize(httpreq.Body);
                    reqmap = iv as Map;
                }
                catch
                {
                    httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                    return;
                }
                if (null == reqmap)
                {
                    httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                    return;
                }

                try
                {
                    uploadID = GetUploaderID(reqmap);
                }
                catch(UploadErrorException e)
                {
                    Map llsderrorreply = new Map();
                    llsderrorreply.Add("state", "error");
                    llsderrorreply.Add("message", e.Message);

                    using (HttpResponse httperrorres = httpreq.BeginResponse())
                    {
                        using (Stream outErrorStream = httperrorres.GetOutputStream())
                        {
                            LlsdXml.Serialize(llsderrorreply, outErrorStream);
                        }
                    }
                    return;
                }
                catch(InsufficientFundsException)
                {
                    Map llsderrorreply = new Map();
                    llsderrorreply.Add("state", "insufficient funds");

                    using (HttpResponse httperrorres = httpreq.BeginResponse())
                    {
                        using (Stream outErrorStream = httperrorres.GetOutputStream())
                        {
                            LlsdXml.Serialize(llsderrorreply, outErrorStream);
                        }
                    }
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

                using (HttpResponse httpres = httpreq.BeginResponse())
                {
                    using (Stream outStream = httpres.GetOutputStream())
                    {
                        LlsdXml.Serialize(llsdreply, outStream);
                    }
                }
            }
            else if(parts.Length < 5 || parts[3] != "Upload" || !UUID.TryParse(parts[4], out transactionID))
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
                asset.Name = string.Empty;
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

                    using (HttpResponse httperrorres = httpreq.BeginResponse())
                    {
                        httperrorres.ContentType = "application/llsd+xml";
                        using (Stream outErrorStream = httperrorres.GetOutputStream())
                        {
                            LlsdXml.Serialize(llsderrorreply, outErrorStream);
                        }
                    }
                    return;
                }
                catch (InsufficientFundsException)
                {
                    Map llsderrorreply = new Map();
                    llsderrorreply.Add("state", "insufficient funds");

                    using (HttpResponse httperrorres = httpreq.BeginResponse())
                    {
                        httperrorres.ContentType = "application/llsd+xml";
                        using (Stream outErrorStream = httperrorres.GetOutputStream())
                        {
                            LlsdXml.Serialize(llsderrorreply, outErrorStream);
                        }
                    }
                    return;
                }
                catch
                {
                    httpreq.ErrorResponse(HttpStatusCode.InternalServerError, "Internal Server Error");
                    return;
                }

                llsdreply.Add("new_asset", asset.ID);
                llsdreply.Add("state", "complete");

                using (HttpResponse httpres = httpreq.BeginResponse())
                {
                    httpres.ContentType = "application/llsd+xml";
                    using (Stream outStream = httpres.GetOutputStream())
                    {
                        LlsdXml.Serialize(llsdreply, outStream);
                    }
                }
            }
        }
    }
}
