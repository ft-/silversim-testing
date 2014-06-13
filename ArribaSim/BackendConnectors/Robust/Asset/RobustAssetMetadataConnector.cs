/*

ArribaSim is distributed under the terms of the
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

using ArribaSim.ServiceInterfaces.Asset;
using ArribaSim.Types;
using ArribaSim.Types.Asset;
using HttpClasses;
using System;
using System.IO;
using System.Xml;

namespace ArribaSim.BackendConnectors.Robust.Asset
{
    public class RobustAssetMetadataConnector : AssetMetadataServiceInterface
    {
        public int TimeoutMs = 20000;
        private string m_AssetURI;

        #region Constructor
        public RobustAssetMetadataConnector(string uri)
        {
            m_AssetURI = uri;
        }
        #endregion

        #region Metadata accessors
        public override AssetMetadata this[UUID key]
        {
            get
            {
                try
                {
                    return parseAssetMetadata(HttpRequestHandler.DoStreamGetRequest(m_AssetURI + "assets/" + key.ToString() + "/metadata", null, TimeoutMs));
                }
                catch
                {
                    throw new AssetNotFound(key);
                }
            }
        }
        #endregion

        #region Asset Metadata Deserialization
        public class InvalidAssetMetadataSerialization : Exception
        {
            public InvalidAssetMetadataSerialization()
            {
            }
        }

        private static string getValue(XmlTextReader reader)
        {
            string tagname = reader.Name;
            string res = string.Empty;
            while (true)
            {
                if (!reader.Read())
                {
                    throw new InvalidAssetMetadataSerialization();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        throw new InvalidAssetMetadataSerialization();

                    case XmlNodeType.Text:
                        res = reader.ReadContentAsString();
                        break;

                    case XmlNodeType.EndElement:
                        if (tagname != reader.Name)
                        {
                            throw new InvalidAssetMetadataSerialization();
                        }
                        return res;
                }
            }
        }

        private static AssetMetadata parseAssetMetadataInternal(XmlTextReader reader)
        {
            AssetMetadata asset = new AssetMetadata();
            while (true)
            {
                if (!reader.Read())
                {
                    throw new InvalidAssetMetadataSerialization();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "FullID":
                                reader.Skip();
                                break;

                            case "ID":
                                asset.ID = getValue(reader);
                                break;

                            case "Name":
                                asset.Name = getValue(reader);
                                break;

                            case "Description":
                                asset.Description = getValue(reader);
                                break;

                            case "Type":
                                asset.Type = (AssetType)int.Parse(getValue(reader));
                                break;

                            case "Local":
                                asset.Local = bool.Parse(getValue(reader));
                                break;

                            case "Temporary":
                                asset.Temporary = bool.Parse(getValue(reader));
                                break;

                            case "CreatorID":
                                asset.Creator = new UUI(getValue(reader));
                                break;

                            case "Flags":
                                asset.Flags = 0;
                                string flags = getValue(reader);
                                if (flags != "Normal")
                                {
                                    string[] flaglist = flags.Split(',');
                                    foreach (string flag in flaglist)
                                    {
                                        if (flag == "Maptile")
                                        {
                                            asset.Flags |= (uint)AssetFlags.Maptile;
                                        }
                                        if (flag == "Rewritable")
                                        {
                                            asset.Flags |= (uint)AssetFlags.Rewritable;
                                        }
                                        if (flag == "Collectable")
                                        {
                                            asset.Flags |= (uint)AssetFlags.Collectable;
                                        }
                                    }
                                }
                                break;

                            default:
                                throw new InvalidAssetMetadataSerialization();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "AssetMetadata")
                        {
                            throw new InvalidAssetMetadataSerialization();
                        }

                        return asset;
                }
            }
        }

        private static AssetMetadata parseAssetMetadata(Stream input)
        {
            using(XmlTextReader reader = new XmlTextReader(input))
            {
                while(true)
                {
                    if(!reader.Read())
                    {
                        throw new InvalidAssetMetadataSerialization();
                    }

                    switch(reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if(reader.Name != "AssetMetadata")
                            {
                                throw new InvalidAssetMetadataSerialization();
                            }

                            return parseAssetMetadataInternal(reader);

                        case XmlNodeType.EndElement:
                            throw new InvalidAssetMetadataSerialization();
                    }
                }
            }
        }

        #endregion
    }
}
