using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace SilverSim.Types.Asset
{
    public static class AssetXml
    {

        #region Asset Deserialization
        public class InvalidAssetSerialization : Exception
        {
            public InvalidAssetSerialization()
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
                    throw new InvalidAssetSerialization();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        throw new InvalidAssetSerialization();

                    case XmlNodeType.Text:
                        res = reader.ReadContentAsString();
                        break;

                    case XmlNodeType.EndElement:
                        if (tagname != reader.Name)
                        {
                            throw new InvalidAssetSerialization();
                        }
                        return res;
                }
            }
        }

        private static AssetData parseAssetDataInternal(XmlTextReader reader)
        {
            AssetData asset = new AssetData();
            while (true)
            {
                if (!reader.Read())
                {
                    throw new InvalidAssetSerialization();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "Data":
                                {
                                    List<byte[]> dataList = new List<byte[]>();
                                    byte[] buffer = new byte[MAX_BASE64_READ_LENGTH];
                                    int readBytes;
                                    int totalBytes = 0;
                                    while ((readBytes = reader.ReadElementContentAsBase64(buffer, 0, MAX_BASE64_READ_LENGTH)) == MAX_BASE64_READ_LENGTH)
                                    {
                                        totalBytes += readBytes;
                                        if (readBytes == MAX_BASE64_READ_LENGTH)
                                        {
                                            dataList.Add(buffer);
                                            buffer = new byte[MAX_BASE64_READ_LENGTH];
                                        }
                                    }
                                    if (readBytes > 0)
                                    {
                                        totalBytes += readBytes;
                                        byte[] rebuffer = new byte[readBytes];
                                        Buffer.BlockCopy(buffer, 0, rebuffer, 0, readBytes);
                                        dataList.Add(rebuffer);
                                    }

                                    asset.Data = new byte[totalBytes];
                                    readBytes = 0;
                                    foreach (byte[] data in dataList)
                                    {
                                        Buffer.BlockCopy(data, 0, asset.Data, readBytes, data.Length);
                                        readBytes += data.Length;
                                    }
                                }
                                break;

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
                                throw new InvalidAssetSerialization();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "AssetBase")
                        {
                            throw new InvalidAssetSerialization();
                        }

                        return asset;
                }
            }
        }

        public static AssetData parseAssetData(Stream input)
        {
            using (XmlTextReader reader = new XmlTextReader(input))
            {
                while (true)
                {
                    if (!reader.Read())
                    {
                        throw new InvalidAssetSerialization();
                    }

                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name != "AssetBase")
                            {
                                throw new InvalidAssetSerialization();
                            }

                            return parseAssetDataInternal(reader);

                        case XmlNodeType.EndElement:
                            throw new InvalidAssetSerialization();
                    }
                }
            }
        }

        private static readonly int MAX_BASE64_READ_LENGTH = 10240;
        #endregion

        #region Asset Metadata Deserialization
        public class InvalidAssetMetadataSerialization : Exception
        {
            public InvalidAssetMetadataSerialization()
            {
            }
        }

        private static string getValueMetadata(XmlTextReader reader)
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
                                asset.ID = getValueMetadata(reader);
                                break;

                            case "Name":
                                asset.Name = getValueMetadata(reader);
                                break;

                            case "Description":
                                asset.Description = getValueMetadata(reader);
                                break;

                            case "Type":
                                asset.Type = (AssetType)int.Parse(getValueMetadata(reader));
                                break;

                            case "Local":
                                asset.Local = bool.Parse(getValueMetadata(reader));
                                break;

                            case "Temporary":
                                asset.Temporary = bool.Parse(getValueMetadata(reader));
                                break;

                            case "CreatorID":
                                asset.Creator = new UUI(getValueMetadata(reader));
                                break;

                            case "Flags":
                                asset.Flags = 0;
                                string flags = getValueMetadata(reader);
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

        public static AssetMetadata parseAssetMetadata(Stream input)
        {
            using (XmlTextReader reader = new XmlTextReader(input))
            {
                while (true)
                {
                    if (!reader.Read())
                    {
                        throw new InvalidAssetMetadataSerialization();
                    }

                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name != "AssetMetadata")
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
