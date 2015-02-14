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

using SilverSim.Types;
using SilverSim.Types.Asset;
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
            return grp.Asset(UUI.Unknown, null, options);
        }

        public static AssetData Asset(this ObjectGroup grp, UUI nextOwner, XmlSerializationOptions options = XmlSerializationOptions.None)
        {
            return grp.Asset(nextOwner, null, options);
        }

        public static AssetData Asset(this ObjectGroup grp, UUI nextOwner, Vector3 offsetpos, XmlSerializationOptions options = XmlSerializationOptions.None)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlTextWriter writer = new XmlTextWriter(ms, UTF8NoBOM))
                {
                    grp.ToXml(writer, options);
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

        public static List<ObjectGroup> fromAsset(AssetData data, UUI currentOwner)
        {
            if(data.Type != AssetType.Object)
            {
                throw new InvalidObjectXmlException();
            }

            using(XmlTextReader reader = new XmlTextReader(data.InputStream))
            {
                return fromXml(reader, currentOwner);
            }
        }

        public static List<ObjectGroup> fromXml(XmlTextReader reader, UUI currentOwner)
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
                                string attrname = "";
                                while (reader.ReadAttributeValue())
                                {
                                    switch(reader.NodeType)
                                    {
                                        case XmlNodeType.Attribute:
                                            attrname = reader.Value;
                                            break;

                                        case XmlNodeType.Text:
                                            switch(attrname)
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
                                            break;

                                        default:
                                            break;
                                    }
                                }
                                ObjectGroup grp = ObjectGroup.FromXml(reader, currentOwner);
                                grp.Position = sogpos;
                                list.Add(grp);
                                break;

                            default:
                                if (!reader.IsEmptyElement)
                                {
                                    reader.Skip();
                                }
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
    }
}
