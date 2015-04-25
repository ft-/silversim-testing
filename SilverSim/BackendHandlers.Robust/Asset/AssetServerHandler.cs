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

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.StructuredData.AssetXml;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace SilverSim.BackendHandlers.Robust.Asset
{
    #region Service Implementation
    class RobustAssetServerHandler : IPlugin
    {
        protected static readonly ILog m_Log = LogManager.GetLogger("ROBUST ASSET HANDLER");
        private BaseHttpServer m_HttpServer;
        AssetServiceInterface m_TemporaryAssetService = null;
        AssetServiceInterface m_PersistentAssetService = null;
        AssetServiceInterface m_ResourceAssetService = null;
        string m_TemporaryAssetServiceName;
        string m_PersistentAssetServiceName;
        bool m_EnableGet;
        private static Encoding UTF8NoBOM = new System.Text.UTF8Encoding(false);

        public RobustAssetServerHandler(string persistentAssetServiceName, string temporaryAssetServiceName, bool enableGet)
        {
            m_PersistentAssetServiceName = persistentAssetServiceName;
            m_TemporaryAssetServiceName = temporaryAssetServiceName;
            m_EnableGet = enableGet;
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Log.Info("Initializing handler for asset server");
            m_HttpServer = loader.HttpServer;
            m_HttpServer.StartsWithUriHandlers.Add("/assets", AssetHandler);
            m_HttpServer.StartsWithUriHandlers.Add("/get_assets_exist", GetAssetsExistHandler);
            m_PersistentAssetService = loader.GetService<AssetServiceInterface>(m_PersistentAssetServiceName);
            if (!string.IsNullOrEmpty(m_TemporaryAssetServiceName))
            {
                m_TemporaryAssetService = loader.GetService<AssetServiceInterface>(m_TemporaryAssetServiceName);
            }
            m_ResourceAssetService = loader.GetService<AssetServiceInterface>("ResourceAssetService");
        }

        public void AssetHandler(HttpRequest req)
        {
            if (req.ContainsHeader("X-SecondLife-Shard"))
            {
                req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Request source not allowed");
                return;
            }

            switch (req.Method)
            {
                case "GET":
                    GetAssetHandler(req);
                    break;

                case "POST":
                    PostAssetHandler(req);
                    break;

                case "DELETE":
                    DeleteAssetHandler(req);
                    break;

                default:
                    req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method Not Allowed");
                    break;
            }
        }

        private static int MAX_ASSET_BASE64_CONVERSION_SIZE = 9 * 1024; /* must be an integral multiple of 3 */

        public void GetAssetHandler(HttpRequest req)
        {
            string uri;
            uri = req.RawUrl.Trim(new char[] { '/' });
            string[] parts = uri.Split('/');
            if (parts.Length < 2)
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            UUID id;
            try
            {
                id = new UUID(parts[1]);
            }
            catch
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            if(!m_EnableGet)
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            if(parts.Length == 2)
            {
                AssetData data;
                try
                {
                    data = m_TemporaryAssetService[id];
                }
                catch
                {
                    try
                    {
                        data = m_PersistentAssetService[id];
                    }
                    catch
                    {
                        try
                        {
                            data = m_ResourceAssetService[id];
                        }
                        catch
                        {
                            req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                            return;
                        }
                    }
                }

                string assetbase_header = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<AssetBase>";
                string flags = "";
                if(data.Data.Length != 0)
                {
                    assetbase_header += "<Data>";
                }
                else
                {
                    assetbase_header += "<Data/>";
                }

                if(0 != (data.Flags & AssetFlags.Maptile))
                {
                    flags = "Maptile";
                }

                if (0 != (data.Flags & AssetFlags.Rewritable))
                {
                    if(flags != string.Empty)
                    {
                        flags += ",";
                    }
                    flags += "Rewritable";
                }

                if (0 != (data.Flags & AssetFlags.Collectable))
                {
                    if (flags != string.Empty)
                    {
                        flags += ",";
                    }
                    flags += "Collectable";
                }

                if(flags == "")
                {
                    flags = "Normal";
                }
                string assetbase_footer = String.Format(
                    "<FullID><Guid>{0}</Guid></FullID><ID>{0}</ID><Name>{1}</Name><Description>{2}</Description><Type>{3}</Type><Local>{4}</Local><Temporary>{5}</Temporary><CreatorID>{6}</CreatorID><Flags>{7}</Flags></AssetBase>",
                    data.ID.ToString(),
                    System.Xml.XmlConvert.EncodeName(data.Name),
                    "",
                    (int)data.Type,
                    data.Local.ToString(),
                    data.Temporary.ToString(),
                    data.Creator.ToString(),
                    flags);
                if (data.Data.Length != 0)
                {
                    assetbase_footer = "</Data>" + assetbase_footer;
                }

                byte[] header = UTF8NoBOM.GetBytes(assetbase_header);
                byte[] footer = UTF8NoBOM.GetBytes(assetbase_footer);
                int base64_codegroups = (data.Data.Length + 2) / 3;

                HttpResponse res = req.BeginResponse();
                res.ContentType = "text/xml";
                Stream st = res.GetOutputStream();
                st.Write(header, 0, header.Length);
                int pos = 0;
                while (data.Data.Length - pos >= MAX_ASSET_BASE64_CONVERSION_SIZE)
                {
                    string b = Convert.ToBase64String(data.Data, pos, MAX_ASSET_BASE64_CONVERSION_SIZE);
                    byte[] block = Encoding.UTF8.GetBytes(b);
                    st.Write(block, 0, block.Length);
                    pos += MAX_ASSET_BASE64_CONVERSION_SIZE;
                }
                if(data.Data.Length > pos)
                {
                    string b = Convert.ToBase64String(data.Data, pos, data.Data.Length - pos);
                    byte[] block = Encoding.UTF8.GetBytes(b);
                    st.Write(block, 0, block.Length);
                }
                st.Write(footer, 0, footer.Length);

                res.Close();
            }
            else if(parts[2] == "metadata")
            {
                AssetMetadata data;
                try
                {
                    data = m_TemporaryAssetService.Metadata[id];
                }
                catch
                {
                    try
                    {
                        data = m_PersistentAssetService.Metadata[id];
                    }
                    catch
                    {
                        try
                        {
                            data = m_ResourceAssetService.Metadata[id];
                        }
                        catch
                        {
                            req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                            return;
                        }
                    }
                }

                string assetbase_header = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<AssetMetadata>";
                string flags = "";

                if (0 != (data.Flags & AssetFlags.Maptile))
                {
                    flags = "Maptile";
                }

                if (0 != (data.Flags & AssetFlags.Rewritable))
                {
                    if (flags != string.Empty)
                    {
                        flags += ",";
                    }
                    flags += "Rewritable";
                }

                if (0 != (data.Flags & AssetFlags.Collectable))
                {
                    if (flags != string.Empty)
                    {
                        flags += ",";
                    }
                    flags += "Collectable";
                }

                if (flags == "")
                {
                    flags = "Normal";
                }
                string assetbase_footer = String.Format(
                    "<FullID><Guid>{0}</Guid></FullID><ID>{0}</ID><Name>{1}</Name><Description>{2}</Description><Type>{3}</Type><Local>{4}</Local><Temporary>{5}</Temporary><CreatorID>{6}</CreatorID><Flags>{7}</Flags></AssetMetadata>",
                    data.ID.ToString(),
                    System.Xml.XmlConvert.EncodeName(data.Name),
                    "",
                    (int)data.Type,
                    data.Local.ToString(),
                    data.Temporary.ToString(),
                    data.Creator.ToString(),
                    flags);
                byte[] header = UTF8NoBOM.GetBytes(assetbase_header);
                byte[] footer = UTF8NoBOM.GetBytes(assetbase_footer);

                HttpResponse res = req.BeginResponse();
                res.ContentType = "text/xml";
                Stream st = res.GetOutputStream(header.Length + footer.Length);
                st.Write(header, 0, header.Length);
                st.Write(footer, 0, footer.Length);

                res.Close();
            }
            else if(parts[2] == "data")
            {
                AssetData data;
                try
                {
                    data = m_TemporaryAssetService[id];
                }
                catch
                {
                    try
                    {
                        data = m_PersistentAssetService[id];
                    }
                    catch
                    {
                        try
                        {
                            data = m_ResourceAssetService[id];
                        }
                        catch
                        {
                            req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                            return;
                        }
                    }
                }

                HttpResponse res = req.BeginResponse();
                res.ContentType = data.ContentType;
                bool compressionEnabled = true;
                switch(data.Type)
                {
                    case AssetType.Texture:
                    case AssetType.Sound:
                    case AssetType.ImageJPEG:
                        /* these are well-compressed no need for further compression */
                        compressionEnabled = false;
                        break;

                    default:
                        break;
                }
                Stream st = res.GetOutputStream(compressionEnabled);
                st.Write(data.Data, 0, data.Data.Length);
                res.Close();
            }
            else
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
            }
        }

        public void DeleteAssetHandler(HttpRequest req)
        {
            HttpResponse res = req.BeginResponse();
            using (XmlTextWriter writer = new XmlTextWriter(res.GetOutputStream(), UTF8NoBOM))
            {
                writer.WriteStartElement("boolean");
                writer.WriteValue(false);
                writer.WriteEndElement();
            }
            res.Close();
        }

        public void PostAssetHandler(HttpRequest req)
        {
            AssetData data;
            try
            {
                data = AssetXml.parseAssetData(req.Body);
            }
            catch
            {
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            if (data.Temporary || data.Local)
            {
                data.Local = false;
                if (null != m_TemporaryAssetService)
                {
                    try
                    {
                        m_TemporaryAssetService.Store(data);
                        HttpResponse res = req.BeginResponse();
                        /* DO NOT USE using here, it will close the underlying stream */
                        XmlTextWriter writer = new XmlTextWriter(res.GetOutputStream(), UTF8NoBOM);
                        writer.WriteStartElement("string");
                        writer.WriteValue(data.ID);
                        writer.WriteEndElement();
                        writer.Flush();
                        res.Close();
                    }
                    catch
                    {
                        req.ErrorResponse(HttpStatusCode.InternalServerError, "Internal Server Error");
                    }
                }
            }

            try
            {
                data.Local = false;
                data.Temporary = false;
                m_PersistentAssetService.Store(data);
                HttpResponse res = req.BeginResponse();
                /* DO NOT USE using here, it will close the underlying stream */
                XmlTextWriter writer = new XmlTextWriter(res.GetOutputStream(), UTF8NoBOM);
                writer.WriteStartElement("string");
                writer.WriteValue(data.ID);
                writer.WriteEndElement();
                writer.Flush();
                res.Close();
            }
            catch(HttpResponse.ConnectionCloseException)
            {
                /* pass this one down to HttpServer */
                throw;
            }
            catch(Exception)
            {
                HttpResponse res = req.BeginResponse();
                /* DO NOT USE using here, it will close the underlying stream */
                XmlTextWriter writer = new XmlTextWriter(res.GetOutputStream(), UTF8NoBOM);
                writer.WriteStartElement("string");
                writer.WriteValue(data.ID);
                writer.WriteEndElement();
                writer.Flush();
                res.Close();
            }
        }

        private static UUID parseUUID(XmlTextReader reader)
        {
            while (true)
            {
                if (!reader.Read())
                {
                    throw new Exception();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name != "string")
                        {
                            throw new Exception();
                        }
                        break;

                    case XmlNodeType.Text:
                        return new UUID(reader.ReadContentAsString());

                    case XmlNodeType.EndElement:
                        throw new Exception();
                }
            }
        }
        private static List<UUID> parseArrayOfUUIDs(XmlTextReader reader)
        {
            List<UUID> result = new List<UUID>();
            while (true)
            {
                if (!reader.Read())
                {
                    throw new Exception();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name != "string")
                        {
                            throw new Exception();
                        }
                        result.Add(parseUUID(reader));
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "ArrayOfString")
                        {
                            throw new Exception();
                        }
                        return result;
                }
            }
        }

        public void GetAssetsExistHandler(HttpRequest req)
        {
            if (req.Method != "POST")
            {
                req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            List<UUID> ids;
            try
            {
                using (XmlTextReader reader = new XmlTextReader(req.Body))
                {
                    ids = parseArrayOfUUIDs(reader);
                }
            }
            catch
            {
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            Dictionary<UUID, bool> asset1;
            if (m_TemporaryAssetService != null)
            {
                asset1 = m_TemporaryAssetService.exists(ids);
                foreach (KeyValuePair<UUID, bool> kvp in m_PersistentAssetService.exists(ids))
                {
                    if (kvp.Value)
                    {
                        asset1[kvp.Key] = true;
                    }
                }
            }
            else
            {
                asset1 = m_PersistentAssetService.exists(ids);
            }

            foreach (KeyValuePair<UUID, bool> kvp in m_ResourceAssetService.exists(ids))
            {
                if (kvp.Value)
                {
                    asset1[kvp.Key] = true;
                }
            }

            HttpResponse res = req.BeginResponse();
            res.ContentType = "text/xml";
            using(XmlTextWriter writer = new XmlTextWriter(res.GetOutputStream(), UTF8NoBOM))
            {
                writer.WriteStartElement("ArrayOfBoolean");
                foreach(UUID id in ids)
                {
                    bool found = false;
                    try
                    {
                        found = asset1[id];
                    }
                    catch
                    {

                    }
                    writer.WriteStartElement("boolean");
                    if (found)
                    {
                        writer.WriteValue("true");
                    }
                    else
                    {
                        writer.WriteValue("false");
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            res.Close();
        }
    }
    #endregion

    #region Factory
    [PluginName("AssetHandler")]
    public class RobustAssetHandlerFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("ROBUST ASSET HANDLER");
        public RobustAssetHandlerFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new RobustAssetServerHandler(ownSection.GetString("PersistentAssetService", "AssetService"),
                ownSection.GetString("TemporaryAssetService", ""), 
                ownSection.GetBoolean("IsGetEnabled", true));
        }
    }
    #endregion
}
