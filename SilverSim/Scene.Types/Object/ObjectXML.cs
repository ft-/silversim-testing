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

using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace SilverSim.Scene.Types.Object
{
    public static class ObjectXML
    {
        public static AssetData Asset(this ObjectGroup grp, XmlSerializationOptions options = XmlSerializationOptions.None) =>
            grp.Asset(UGUI.Unknown, Vector3.Zero, options, false);

        public static AssetData Asset(this ObjectGroup grp, UGUI nextOwner, XmlSerializationOptions options = XmlSerializationOptions.None) =>
            grp.Asset(nextOwner, Vector3.Zero, options, false);

        public static AssetData Asset(this ObjectGroup grp, UGUI nextOwner, Vector3 offsetpos, XmlSerializationOptions options = XmlSerializationOptions.None, bool writeOffsetPos = true)
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = ms.UTF8XmlTextWriter())
                {
                    grp.ToXml(writer, nextOwner, offsetpos, options, writeOffsetPos);
                    writer.Flush();
                }

                return new AssetData
                {
                    ID = UUID.Random,
                    Type = AssetType.Object,
                    Data = ms.ToArray()
                };
            }
        }

        public static AssetData Asset(this List<ObjectGroup> objlist, XmlSerializationOptions options) => objlist.Asset(UGUI.Unknown, options);

        public static AssetData Asset(this List<ObjectGroup> objlist, UGUI nextOwner, XmlSerializationOptions options)
        {
            if (objlist.Count == 1)
            {
                return objlist[0].Asset(nextOwner, options);
            }
            Vector3 basepos = objlist[0].Position;

            using (var ms = new MemoryStream())
            {
                using (var writer = ms.UTF8XmlTextWriter())
                {
                    writer.WriteStartElement("CoalescedObject");
                    foreach (var grp in objlist)
                    {
                        grp.ToXml(writer, nextOwner, basepos, options);
                    }
                    writer.WriteEndElement();

                    writer.Flush();
                }

                return new AssetData
                {
                    ID = UUID.Random,
                    Type = AssetType.Object,
                    Data = ms.ToArray()
                };
            }
        }

        public static List<ObjectGroup> FromAsset(AssetData data, UGUI currentOwner, XmlDeserializationOptions options = XmlDeserializationOptions.None)
        {
            if(data.Type != AssetType.Object)
            {
                throw new InvalidObjectXmlException();
            }

            using (var xmlstream = data.InputStream)
            {
                return FromXml(xmlstream, currentOwner, options);
            }
        }

        public static List<ObjectGroup> FromXml(Stream xmlstream, UGUI currentOwner, XmlDeserializationOptions options = XmlDeserializationOptions.None)
        {
            using (XmlTextReader reader = new ObjectXmlStreamFilter(xmlstream).CreateXmlReader())
            {
                return FromXml(reader, currentOwner, options);
            }
        }

        private static List<ObjectGroup> FromXml(XmlTextReader reader, UGUI currentOwner, XmlDeserializationOptions options = XmlDeserializationOptions.None)
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
                            return FromXmlSingleObject(reader, currentOwner, options);

                        case "CoalescedObject":
                            return FromXmlCoalescedObject(reader, currentOwner, options);

                        default:
                            throw new InvalidObjectXmlException();
                    }
                }
            }
        }

        private static List<ObjectGroup> FromXmlSingleObject(XmlTextReader reader, UGUI currentOwner, XmlDeserializationOptions options = XmlDeserializationOptions.None) => new List<ObjectGroup>
        {
            ObjectGroup.FromXml(reader, currentOwner, options)
        };

        private static List<ObjectGroup> FromXmlCoalescedObject(XmlTextReader reader, UGUI currentOwner, XmlDeserializationOptions options = XmlDeserializationOptions.None)
        {
            var list = new List<ObjectGroup>();
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
                                var sogpos = new Vector3();
                                if (reader.MoveToFirstAttribute())
                                {
                                    do
                                    {
                                        switch (reader.Name)
                                        {
                                            case "offsetx":
                                                sogpos.X_String = reader.Value;
                                                break;

                                            case "offsety":
                                                sogpos.Y_String = reader.Value;
                                                break;

                                            case "offsetz":
                                                sogpos.Z_String = reader.Value;
                                                break;

                                            default:
                                                break;
                                        }
                                    }
                                    while (reader.MoveToNextAttribute());
                                }
                                var grp = FromXmlSingleWithinCoalescedObject(reader, currentOwner, options);
                                grp.CoalescedRestoreOffset = sogpos;
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

        private static ObjectGroup FromXmlSingleWithinCoalescedObject(XmlTextReader reader, UGUI currentOwner, XmlDeserializationOptions options = XmlDeserializationOptions.None)
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
                                grp = ObjectGroup.FromXml(reader, currentOwner, options);
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
