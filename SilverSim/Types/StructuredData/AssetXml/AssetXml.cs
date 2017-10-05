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

using SilverSim.Types.Asset;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace SilverSim.Types.StructuredData.AssetXml
{
    public static class AssetXml
    {
        #region Asset Deserialization
        [Serializable]
        public class InvalidAssetSerializationException : Exception
        {
            public InvalidAssetSerializationException()
            {
            }

            public InvalidAssetSerializationException(string message)
                : base(message)
            {
            }

            protected InvalidAssetSerializationException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }

            public InvalidAssetSerializationException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

        private static void ParseAssetFullID(XmlTextReader reader)
        {
            while (true)
            {
                if (!reader.Read())
                {
                    throw new InvalidAssetSerializationException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "Guid":
                                reader.ReadElementValueAsString();
                                break;

                            default:
                                throw new InvalidAssetSerializationException();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "FullID")
                        {
                            throw new InvalidAssetSerializationException();
                        }

                        return;

                    default:
                        break;
                }
            }
        }

        private static AssetData ParseAssetDataInternal(XmlTextReader reader)
        {
            var asset = new AssetData();
            while (true)
            {
                if (!reader.Read())
                {
                    throw new InvalidAssetSerializationException();
                }

                var isEmptyElement = reader.IsEmptyElement;
                var nodeName = reader.Name;

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (nodeName)
                        {
                            case "Data":
                                asset.Data = isEmptyElement ?
                                    new byte[0] :
                                    reader.ReadContentAsBase64();
                                break;

                            case "FullID":
                                ParseAssetFullID(reader);
                                break;

                            case "ID":
                                asset.ID = reader.ReadElementValueAsString();
                                break;

                            case "Name":
                                asset.Name = (isEmptyElement) ?
                                    string.Empty :
                                    reader.ReadElementValueAsString();
                                break;

                            case "Description":
                                reader.ReadToEndElement();
                                break;

                            case "Type":
                                asset.Type = (AssetType)reader.ReadElementValueAsInt();
                                break;

                            case "Local":
                                if (!isEmptyElement)
                                {
                                    asset.Local = reader.ReadElementValueAsBoolean();
                                }
                                break;

                            case "Temporary":
                                if (!isEmptyElement)
                                {
                                    asset.Temporary = reader.ReadElementValueAsBoolean();
                                }
                                break;

                            case "Flags":
                                asset.Flags = AssetFlags.Normal;
                                var flags = reader.ReadElementValueAsString();
                                if (flags != "Normal")
                                {
                                    foreach (var flag in flags.Split(','))
                                    {
                                        if (flag == "Maptile")
                                        {
                                            asset.Flags |= AssetFlags.Maptile;
                                        }
                                        if (flag == "Rewritable")
                                        {
                                            asset.Flags |= AssetFlags.Rewritable;
                                        }
                                        if (flag == "Collectable")
                                        {
                                            asset.Flags |= AssetFlags.Collectable;
                                        }
                                    }
                                }
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (nodeName != "AssetBase")
                        {
                            throw new InvalidAssetSerializationException();
                        }

                        return asset;

                    default:
                        break;
                }
            }
        }

        public static AssetData ParseAssetData(Stream input)
        {
            using (var reader = new XmlTextReader(input))
            {
                while (true)
                {
                    if (!reader.Read())
                    {
                        throw new InvalidAssetSerializationException();
                    }

                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name != "AssetBase")
                            {
                                throw new InvalidAssetSerializationException();
                            }

                            return ParseAssetDataInternal(reader);

                        case XmlNodeType.EndElement:
                            throw new InvalidAssetSerializationException();

                        default:
                            break;
                    }
                }
            }
        }
        #endregion

        #region Asset Metadata Deserialization
        [Serializable]
        public class InvalidAssetMetadataSerializationException : Exception
        {
            public InvalidAssetMetadataSerializationException()
            {
            }

            public InvalidAssetMetadataSerializationException(string message)
                : base(message)
            {
            }

            protected InvalidAssetMetadataSerializationException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }

            public InvalidAssetMetadataSerializationException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

        private static AssetMetadata ParseAssetMetadataInternal(XmlTextReader reader)
        {
            var asset = new AssetMetadata();
            while (true)
            {
                if (!reader.Read())
                {
                    throw new InvalidAssetMetadataSerializationException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "FullID":
                                ParseAssetFullID(reader);
                                break;

                            case "ID":
                                asset.ID = reader.ReadElementValueAsString();
                                break;

                            case "Name":
                                asset.Name = reader.ReadElementValueAsString();
                                break;

                            case "Description":
                                reader.ReadElementValueAsString();
                                break;

                            case "Type":
                                asset.Type = (AssetType)reader.ReadElementValueAsInt();
                                break;

                            case "Local":
                                asset.Local = reader.ReadElementValueAsBoolean();
                                break;

                            case "Temporary":
                                asset.Temporary = reader.ReadElementValueAsBoolean();
                                break;

                            case "Flags":
                                asset.Flags = AssetFlags.Normal;
                                string flags = reader.ReadElementValueAsString();
                                if (flags != "Normal")
                                {
                                    foreach (var flag in flags.Split(','))
                                    {
                                        if (flag == "Maptile")
                                        {
                                            asset.Flags |= AssetFlags.Maptile;
                                        }
                                        if (flag == "Rewritable")
                                        {
                                            asset.Flags |= AssetFlags.Rewritable;
                                        }
                                        if (flag == "Collectable")
                                        {
                                            asset.Flags |= AssetFlags.Collectable;
                                        }
                                    }
                                }
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "AssetMetadata")
                        {
                            throw new InvalidAssetMetadataSerializationException();
                        }

                        return asset;

                    default:
                        break;
                }
            }
        }

        public static AssetMetadata ParseAssetMetadata(Stream input)
        {
            using (var reader = new XmlTextReader(input))
            {
                while (true)
                {
                    if (!reader.Read())
                    {
                        throw new InvalidAssetMetadataSerializationException();
                    }

                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name != "AssetMetadata")
                            {
                                throw new InvalidAssetMetadataSerializationException();
                            }

                            return ParseAssetMetadataInternal(reader);

                        case XmlNodeType.EndElement:
                            throw new InvalidAssetMetadataSerializationException();

                        default:
                            break;
                    }
                }
            }
        }
        #endregion
    }
}
