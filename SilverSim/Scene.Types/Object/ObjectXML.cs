// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace SilverSim.Scene.Types.Object
{
    public static class ObjectXML
    {
        private static UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);

        public static AssetData Asset(this ObjectGroup grp, XmlSerializationOptions options = XmlSerializationOptions.None)
        {
            return grp.Asset(UUI.Unknown, Vector3.Zero, options, false);
        }

        public static AssetData Asset(this ObjectGroup grp, UUI nextOwner, XmlSerializationOptions options = XmlSerializationOptions.None)
        {
            return grp.Asset(nextOwner, Vector3.Zero, options, false);
        }

        public static AssetData Asset(this ObjectGroup grp, UUI nextOwner, Vector3 offsetpos, XmlSerializationOptions options = XmlSerializationOptions.None, bool writeOffsetPos = true)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlTextWriter writer = new XmlTextWriter(ms, UTF8NoBOM))
                {
                    grp.ToXml(writer, nextOwner, offsetpos, options, writeOffsetPos);
                    writer.Flush();
                }

                AssetData asset = new AssetData();
                asset.Type = AssetType.Object;
                asset.Data = ms.GetBuffer();
                return asset;
            }
        }

        public static AssetData Asset(this List<ObjectGroup> objlist, XmlSerializationOptions options)
        {
            return objlist.Asset(UUI.Unknown, options);
        }

        public static AssetData Asset(this List<ObjectGroup> objlist, UUI nextOwner, XmlSerializationOptions options)
        {
            if (objlist.Count == 1)
            {
                return objlist[0].Asset(nextOwner, options);
            }
            Vector3 basepos = objlist[0].Position;

            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlTextWriter writer = new XmlTextWriter(ms, UTF8NoBOM))
                {
                    writer.WriteStartElement("CoalescedObject");
                    foreach (ObjectGroup grp in objlist)
                    {
                        grp.ToXml(writer, nextOwner, basepos, options);
                    }
                    writer.WriteEndElement();

                    writer.Flush();
                }

                AssetData asset = new AssetData();
                asset.Type = AssetType.Object;
                asset.Data = ms.GetBuffer();
                return asset;
            }
        }

        /* OpenSim brain-deadness filter */
        static string FilterBrokenTags(string xmlin)
        {
            return xmlin.Replace("<SceneObjectPart xmlns:xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">", "<SceneObjectPart xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">");
        }

        public static List<ObjectGroup> fromAsset(AssetData data, UUI currentOwner)
        {
            if(data.Type != AssetType.Object)
            {
                throw new InvalidObjectXmlException();
            }

            using (Stream xmlstream = data.InputStream)
            {
                return fromXml(xmlstream, currentOwner);
            }
        }

        public static List<ObjectGroup> fromXml(Stream xmlstream, UUI currentOwner)
        {
            using (XmlTextReader reader = new XmlTextReader(new ObjectXmlStreamFilter(xmlstream)))
            {
                return fromXml(reader, currentOwner);
            }
        }

        static List<ObjectGroup> fromXml(XmlTextReader reader, UUI currentOwner)
        {
            for(;;)
            {
                if(!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                if(reader.NodeType == XmlNodeType.Element)
                {
                    switch(reader.Name)
                    {
                        case "SceneObjectGroup":
                            return fromXmlSingleObject(reader, currentOwner);

                        case "CoalescedObject":
                            return fromXmlCoalescedObject(reader, currentOwner);

                        default:
                            throw new InvalidObjectXmlException();
                    }
                }
            }
        }

        static List<ObjectGroup> fromXmlSingleObject(XmlTextReader reader, UUI currentOwner)
        {
            List<ObjectGroup> list = new List<ObjectGroup>();

            list.Add(ObjectGroup.FromXml(reader, currentOwner));
            return list;
        }

        static List<ObjectGroup> fromXmlCoalescedObject(XmlTextReader reader, UUI currentOwner)
        {
            List<ObjectGroup> list = new List<ObjectGroup>();
            for(;;)
            {
                if(!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "SceneObjectGroup":
                                if (reader.IsEmptyElement)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                Vector3 sogpos = new Vector3();
                                if (reader.MoveToFirstAttribute())
                                {
                                    do
                                    {
                                        switch (reader.Name)
                                        {
                                            case "x":
                                                sogpos.X_String = reader.Value;
                                                break;

                                            case "y":
                                                sogpos.Y_String = reader.Value;
                                                break;

                                            case "z":
                                                sogpos.Z_String = reader.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                    }
                                    while (reader.MoveToNextAttribute());
                                }
                                ObjectGroup grp = fromXmlSingleWithinCoalescedObject(reader, currentOwner);
                                grp.Position = sogpos;
                                list.Add(grp);
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if(reader.Name != "CoalescedObject")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return list;

                    default:
                        break;
                }
            }
        }

        static ObjectGroup fromXmlSingleWithinCoalescedObject(XmlTextReader reader, UUI currentOwner)
        {
            ObjectGroup grp = null;
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "SceneObjectGroup":
                                if (reader.IsEmptyElement)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                grp = ObjectGroup.FromXml(reader, currentOwner);
                                break;

                            case "RootPart":
                                /* XML format mess, two different serializations just for that */
                                return ObjectGroup.FromXml(reader, currentOwner, true);

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "SceneObjectGroup")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return grp;

                    default:
                        break;
                }
            }
        }
    }
}
