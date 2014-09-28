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

using HttpClasses;
using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.StructuredData.AssetXml;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace SilverSim.BackendConnectors.Robust.Asset
{
    #region Service Implementation
    public class RobustAssetConnector : AssetServiceInterface, IPlugin
    {
        public class RobustAssetProtocolError : Exception
        {
            public RobustAssetProtocolError(string msg) : base(msg) {}
        }

        private static int MAX_ASSET_BASE64_CONVERSION_SIZE = 9 * 1024; /* must be an integral multiple of 3 */
        private int m_TimeoutMs = 20000;
        public int TimeoutMs
        {
            get
            {
                return m_TimeoutMs;
            }
            set
            {
                m_MetadataService.TimeoutMs = value;
                m_TimeoutMs = value;
            }
        }
        private string m_AssetURI;
        private RobustAssetMetadataConnector m_MetadataService;
        private DefaultAssetReferencesService m_ReferencesService;

        #region Constructor
        public RobustAssetConnector(string uri)
        {
            if(!uri.EndsWith("/"))
            {
                uri += "/";
            }
            uri += "";

            m_AssetURI = uri;
            m_MetadataService = new RobustAssetMetadataConnector(uri);
            m_ReferencesService = new DefaultAssetReferencesService(this);
            m_MetadataService.TimeoutMs = m_TimeoutMs;
        }

        public void Startup(ConfigurationLoader loader)
        {

        }
        #endregion

        #region Exists methods
        public override void exists(UUID key)
        {
            try
            {
                HttpRequestHandler.DoGetRequest(m_AssetURI + "assets/" + key.ToString() + "/metadata", null, TimeoutMs);
            }
            catch
            {
                throw new AssetNotFound(key);
            }
        }

        private static bool parseBoolean(XmlTextReader reader)
        {
            bool result = false;
            while(true)
            {
                if(!reader.Read())
                {
                    throw new Exception();
                }

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if(reader.Name != "boolean")
                        {
                            throw new Exception();
                        }
                        break;

                    case XmlNodeType.Text:
                        result = reader.ReadContentAsBoolean();
                        break;

                    case XmlNodeType.EndElement:
                        if(reader.Name != "boolean")
                        {
                            throw new Exception();
                        }
                        return result;
                }
            }
        }
        private static List<bool> parseArrayOfBoolean(XmlTextReader reader)
        {
            List<bool> result = new List<bool>();
            while(true)
            {
                if(!reader.Read())
                {
                    throw new Exception();
                }

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if(reader.Name != "boolean")
                        {
                            throw new Exception();
                        }
                        result.Add(parseBoolean(reader));
                        break;

                    case XmlNodeType.EndElement:
                        if(reader.Name != "ArrayOfBoolean")
                        {
                            throw new Exception();
                        }
                        return result;
                }
            }
        }

        public static List<bool> parseAssetsExistResponse(XmlTextReader reader)
        {
            while(true)
            {
                if(!reader.Read())
                {
                    throw new Exception();
                }

                if(reader.NodeType == XmlNodeType.Element)
                {
                    if(reader.Name != "ArrayOfBoolean")
                    {
                        throw new Exception();
                    }

                    return parseArrayOfBoolean(reader);
                }
            }
        }

        public override Dictionary<UUID, bool> exists(List<UUID> assets)
        {
            Dictionary<UUID, bool> res = new Dictionary<UUID,bool>();
            string xmlreq = "<?xml version=\"1.0\"?>";
            xmlreq += "<ArrayOfString xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">";
            foreach(UUID asset in assets)
            {
                xmlreq += "<string>" + asset.ToString() + "</string>";
            }
            xmlreq += "</ArrayOfString>";

            Stream xmlres;
            try
            {
                xmlres = HttpRequestHandler.DoStreamRequest("POST", m_AssetURI + "get_assets_exist", null, "text/xml", xmlreq, false, TimeoutMs);
            }
            catch
            {
                foreach(UUID asset in assets)
                {
                    res[asset] = false;
                }
                return res;
            }

            try
            {
                using(XmlTextReader xmlreader = new XmlTextReader(xmlres))
                {
                    List<bool> response = parseAssetsExistResponse(xmlreader);
                    if (response.Count != assets.Count)
                    {
                        throw new RobustAssetProtocolError("Invalid response for get_assets_exist received");
                    }
                    for(int i = 0; i < res.Count; ++i)
                    {
                        res.Add(assets[i], response[i]);
                    }
                }
            }
            catch
            {
                throw new RobustAssetProtocolError("Invalid response for get_assets_exist received");
            }

            return res;
        }
        #endregion

        #region Accessors
        public override AssetData this[UUID key]
        {
            get
            {
                Stream stream;
                try
                {
                    stream = HttpRequestHandler.DoStreamGetRequest(m_AssetURI + "assets/" + key.ToString(), null, TimeoutMs);
                }
                catch
                {
                    throw new AssetNotFound(key);
                }
                return AssetXml.parseAssetData(stream);
            }
        }
        #endregion

        #region Metadata interface
        public override AssetMetadataServiceInterface Metadata
        {
            get
            {
                return m_MetadataService;
            }
        }
        #endregion

        #region References interface
        public override AssetReferencesServiceInterface References
        {
            get
            {
                return m_ReferencesService;
            }
        }
        #endregion

        #region Store asset method
        public override void Store(AssetData asset)
        {
            if(asset.Temporary || asset.Local)
            {
                /* Do not store temporary or local assets on grid */
                return;
            }
            string assetbase_header = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<AssetBase>";
            string flags = "";

            if(0 != (asset.Flags & (uint)AssetFlags.Maptile))
            {
                flags = "Maptile";
            }

            if (0 != (asset.Flags & (uint)AssetFlags.Rewritable))
            {
                if(flags != string.Empty)
                {
                    flags += ",";
                }
                flags += "Rewritable";
            }

            if (0 != (asset.Flags & (uint)AssetFlags.Collectable))
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
                asset.ID.ToString(),
                System.Xml.XmlConvert.EncodeName(asset.Name),
                System.Xml.XmlConvert.EncodeName(asset.Description),
                (int)asset.Type,
                asset.Local.ToString(),
                asset.Temporary.ToString(),
                asset.Creator.ToString(),
                flags);
            byte[] header = Encoding.UTF8.GetBytes(assetbase_header);
            byte[] footer = Encoding.UTF8.GetBytes(assetbase_footer);
            int base64_codegroups = (asset.Data.Length + 2) / 3;
            HttpRequestHandler.DoRequest("POST", m_AssetURI, 
                null, "text/xml", 4 * base64_codegroups + header.Length + footer.Length, delegate(Stream st)
            {
                /* Stream based asset conversion method here */
                st.Write(header, 0, footer.Length);
                int pos = 0;
                while (asset.Data.Length - pos >= MAX_ASSET_BASE64_CONVERSION_SIZE)
                {
                    string b = Convert.ToBase64String(asset.Data, pos, MAX_ASSET_BASE64_CONVERSION_SIZE);
                    byte[] block = Encoding.UTF8.GetBytes(b);
                    st.Write(block, 0, block.Length);
                    pos += MAX_ASSET_BASE64_CONVERSION_SIZE;
                }
                if(asset.Data.Length > pos)
                {
                    string b = Convert.ToBase64String(asset.Data, pos, asset.Data.Length - pos);
                    byte[] block = Encoding.UTF8.GetBytes(b);
                    st.Write(block, 0, block.Length);
                }
                st.Write(footer, 0, footer.Length);
            }, false, TimeoutMs);
        }
        #endregion

        #region Delete asset method
        public override void Delete(UUID id)
        {
            try
            {
                HttpRequestHandler.DoRequest("DELETE", m_AssetURI + "/" + id.ToString(), null, "", null, false, TimeoutMs);
            }
            catch
            {
                throw new AssetNotFound(id);
            }
        }
        #endregion
    }
    #endregion

    #region Factory
    public class RobustAssetConnectorFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("ROBUST ASSET CONNECTOR");
        public RobustAssetConnectorFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            if (!ownSection.Contains("URI"))
            {
                m_Log.FatalFormat("Missing 'URI' in section {0}", ownSection.Name);
                throw new ConfigurationLoader.ConfigurationError();
            }
            return new RobustAssetConnector(ownSection.GetString("URI"));
        }
    }
    #endregion
}
