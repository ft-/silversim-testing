﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpServer;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.Core.Capabilities
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

        [Serializable]
        public class UrlNotFoundException : Exception
        {
            public UrlNotFoundException()
            {

            }
        }

        [Serializable]
        public class InsufficientFundsException : Exception
        {
            public InsufficientFundsException()
            {

            }
        }

        [Serializable]
        public class UploadErrorException : Exception
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

                Map reqmap;
                try
                {
                    reqmap = LLSD_XML.Deserialize(httpreq.Body) as Map;
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
                            LLSD_XML.Serialize(llsderrorreply, outErrorStream);
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
                            LLSD_XML.Serialize(llsderrorreply, outErrorStream);
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
                        LLSD_XML.Serialize(llsdreply, outStream);
                    }
                }
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
                            LLSD_XML.Serialize(llsderrorreply, outErrorStream);
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
                            LLSD_XML.Serialize(llsderrorreply, outErrorStream);
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
                        LLSD_XML.Serialize(llsdreply, outStream);
                    }
                }
            }
        }
    }
}
