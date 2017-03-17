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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;

namespace SilverSim.Scene.Types.Object
{
    public static class ObjectXML
    {
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
                using (XmlTextWriter writer = ms.UTF8XmlTextWriter())
                {
                    grp.ToXml(writer, nextOwner, offsetpos, options, writeOffsetPos);
                    writer.Flush();
                }

                AssetData asset = new AssetData();
                asset.Type = AssetType.Object;
                asset.Data = ms.ToArray();
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
                using (XmlTextWriter writer = ms.UTF8XmlTextWriter())
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
                asset.Data = ms.ToArray();
                return asset;
            }
        }

        public static List<ObjectGroup> FromAsset(AssetData data, UUI currentOwner)
        {
            if(data.Type != AssetType.Object)
            {
                throw new InvalidObjectXmlException();
            }

            using (Stream xmlstream = data.InputStream)
            {
                return FromXml(xmlstream, currentOwner);
            }
        }

        public static List<ObjectGroup> FromXml(Stream xmlstream, UUI currentOwner)
        {
            using (XmlTextReader reader = new XmlTextReader(new ObjectXmlStreamFilter(xmlstream)))
            {
                return FromXml(reader, currentOwner);
            }
        }

        static List<ObjectGroup> FromXml(XmlTextReader reader, UUI currentOwner)
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
                            return FromXmlSingleObject(reader, currentOwner);

                        case "CoalescedObject":
                            return FromXmlCoalescedObject(reader, currentOwner);

                        default:
                            throw new InvalidObjectXmlException();
                    }
                }
            }
        }

        static List<ObjectGroup> FromXmlSingleObject(XmlTextReader reader, UUI currentOwner)
        {
            List<ObjectGroup> list = new List<ObjectGroup>();

            list.Add(ObjectGroup.FromXml(reader, currentOwner));
            return list;
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        static List<ObjectGroup> FromXmlCoalescedObject(XmlTextReader reader, UUI currentOwner)
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
                                ObjectGroup grp = FromXmlSingleWithinCoalescedObject(reader, currentOwner);
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

        static ObjectGroup FromXmlSingleWithinCoalescedObject(XmlTextReader reader, UUI currentOwner)
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
