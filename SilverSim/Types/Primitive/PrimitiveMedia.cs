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

using SilverSim.Threading;
using SilverSim.Types.StructuredData.Llsd;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace SilverSim.Types.Primitive
{
    public class PrimitiveMedia : RwLockedList<PrimitiveMedia.Entry>
    {
        public class Entry
        {
            public Entry()
            {
            }

            public bool IsAutoLoop;
            public bool IsAutoPlay;
            public bool IsAutoScale;
            public bool IsAutoZoom;
            public int Width;
            public int Height;
            public PrimitiveMediaPermission ControlPermissions = PrimitiveMediaPermission.All;
            public PrimitiveMediaControls Controls;
            public string CurrentURL = string.Empty;
            public bool IsAlternativeImageEnabled;
            public bool IsWhiteListEnabled;
            public string HomeURL = string.Empty;
            public bool IsInteractOnFirstClick;
            public PrimitiveMediaPermission InteractPermissions = PrimitiveMediaPermission.All;
            public string[] WhiteList = new string[0];

            public Entry(Map m)
            {
                IsAlternativeImageEnabled = m["alt_image_enable"].AsBoolean;
                IsAutoLoop = m["auto_loop"].AsBoolean;
                IsAutoPlay = m["auto_play"].AsBoolean;
                IsAutoScale = m["auto_scale"].AsBoolean;
                IsAutoZoom = m["auto_zoom"].AsBoolean;
                Controls = (PrimitiveMediaControls)m["controls"].AsInt;
                CurrentURL = m["current_url"].ToString();
                IsInteractOnFirstClick = m["first_click_interact"].AsBoolean;
                Height = m["height_pixels"].AsInt;
                HomeURL = m["home_url"].ToString();
                ControlPermissions = (PrimitiveMediaPermission)m["perms_control"].AsInt;
                InteractPermissions = (PrimitiveMediaPermission)m["perms_interact"].AsInt;
                if(m["whitelist"] is AnArray)
                {
                    AnArray a = (AnArray)m["whitelist"];
                    WhiteList = new string[a.Count];
                    for(int i = 0; i <a.Count; ++i)
                    {
                        WhiteList[i] = a[i].ToString();
                    }
                }
                IsWhiteListEnabled = m["whitelist_enable"].AsBoolean;
                Width = m["width_pixels"].AsInt;
            }

            public static explicit operator Map(Entry e)
            {
                var m = new Map
                {
                    { "alt_image_enable", e.IsAlternativeImageEnabled },
                    { "auto_loop", e.IsAutoLoop },
                    { "auto_play", e.IsAutoPlay },
                    { "auto_scale", e.IsAutoScale },
                    { "auto_zoom", e.IsAutoZoom },
                    { "controls", (int)e.Controls },
                    { "current_url", e.CurrentURL },
                    { "first_click_interact", e.IsInteractOnFirstClick },
                    { "width_pixels", e.Width },
                    { "height_pixels", e.Height },
                    { "home_url", e.HomeURL },
                    { "perms_control", (int)e.ControlPermissions },
                    { "perms_interact", (int)e.InteractPermissions }
                };
                var whiteList = new AnArray();
                if (e.WhiteList?.Length > 0)
                {
                    foreach (string v in e.WhiteList)
                    {
                        whiteList.Add(v);
                    }
                }
                m.Add("whitelist", whiteList);
                m.Add("whitelist_enable", e.IsWhiteListEnabled);
                return m;
            }

            public static explicit operator Entry(Map m)
            {
                var e = new Entry
                {
                    IsAlternativeImageEnabled = m["alt_image_enable"].AsBoolean,
                    IsAutoLoop = m["auto_loop"].AsBoolean,
                    IsAutoPlay = m["auto_play"].AsBoolean,
                    IsAutoScale = m["auto_scale"].AsBoolean,
                    IsAutoZoom = m["auto_zoom"].AsBoolean,
                    Controls = (PrimitiveMediaControls)m["controls"].AsInt,
                    CurrentURL = m["current_url"].ToString(),
                    IsInteractOnFirstClick = m["first_click_interfact"].AsBoolean,
                    Width = m["width_pixels"].AsInt,
                    Height = m["height_pixels"].AsInt,
                    HomeURL = m["home_url"].ToString(),
                    ControlPermissions = (PrimitiveMediaPermission)m["perms_control"].AsInt,
                    InteractPermissions = (PrimitiveMediaPermission)m["perms_interact"].AsInt
                };
                if (m.ContainsKey("whitelist") && m["whitelist"] is AnArray)
                {
                    var whiteList = new List<string>();
                    foreach (IValue iv in (AnArray)m["whitelist"])
                    {
                        whiteList.Add(iv.ToString());
                    }
                    e.WhiteList = whiteList.ToArray();
                }
                e.IsWhiteListEnabled = m["whitelist_enable"].AsBoolean;
                return e;
            }

            public void ToXml(XmlTextWriter writer)
            {
                writer.WriteStartElement("map");
                writer.WriteKeyValuePair("alt_image_enable", IsAlternativeImageEnabled);
                writer.WriteKeyValuePair("auto_loop", IsAutoLoop);
                writer.WriteKeyValuePair("auto_play", IsAutoPlay);
                writer.WriteKeyValuePair("auto_scale", IsAutoScale);
                writer.WriteKeyValuePair("auto_zoom", IsAutoZoom);
                writer.WriteKeyValuePair("controls", (int)Controls);
                writer.WriteKeyValuePair("current_url", CurrentURL);
                writer.WriteKeyValuePair("first_click_interact", IsInteractOnFirstClick);
                writer.WriteKeyValuePair("width_pixels", Width);
                writer.WriteKeyValuePair("height_pixels", Height);
                writer.WriteKeyValuePair("home_url", HomeURL);
                writer.WriteKeyValuePair("perms_control", (int)ControlPermissions);
                writer.WriteKeyValuePair("perms_interact", (int)InteractPermissions);
                if (WhiteList?.Length > 0)
                {
                    bool haveWhitelistEntry = false;
                    foreach (string v in WhiteList)
                    {
                        if(!haveWhitelistEntry)
                        {
                            haveWhitelistEntry = true;
                            writer.WriteNamedValue("key", "whitelist");
                            writer.WriteStartElement("array");
                        }
                        writer.WriteNamedValue("string", v);
                    }
                    if (haveWhitelistEntry)
                    {
                        writer.WriteEndElement();
                    }
                }
                writer.WriteKeyValuePair("whitelist_enable", IsWhiteListEnabled);
                writer.WriteEndElement();
            }
        }

        private static void FromXmlOSData(PrimitiveMedia media, XmlTextReader reader)
        {
            for(;;)
            {
                if(!reader.Read())
                {
                    throw new XmlException();
                }

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == "llsd")
                        {
                            AnArray entries = LlsdXml.DeserializeLLSDNode(reader) as AnArray;
                            foreach (IValue iv in entries)
                            {
                                Map m = iv as Map;
                                if (m != null)
                                {
                                    media.Add(new Entry(m));
                                }
                                else
                                {
                                    media.Add(null);
                                }
                            }
                        }
                        else
                        {
                            reader.ReadToEndElement();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if(reader.Name != "OSData")
                        {
                            throw new XmlException();
                        }
                        return;
                }
            }
        }

        private static void FromXmlOSMedia(PrimitiveMedia media, XmlTextReader reader)
        {
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new XmlException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.IsEmptyElement)
                        {
                            break;
                        }
                        if (reader.Name == "OSData")
                        {
                            FromXmlOSData(media, reader);
                        }
                        else
                        {
                            reader.ReadToEndElement();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "OSMedia")
                        {
                            throw new XmlException();
                        }
                        return;

                    default:
                        break;
                }
            }
        }

        private static void FromStringifiedXmlOSMedia(PrimitiveMedia media, XmlTextReader reader)
        {
            for(;;)
            {
                if(!reader.Read())
                {
                    throw new XmlException();
                }

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if(reader.Name == "OSMedia")
                        {
                            FromXmlOSMedia(media, reader);
                            return;
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        public static PrimitiveMedia FromXml(XmlTextReader reader)
        {
            if (reader.IsEmptyElement)
            {
                return null;
            }

            PrimitiveMedia media = new PrimitiveMedia();
            StringBuilder textNode = new StringBuilder();
            bool haveNodeInside = false;

            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new XmlException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.IsEmptyElement)
                        {
                            break;
                        }
                        switch(reader.Name)
                        {
                            case "OSMedia":
                                haveNodeInside = true;
                                FromXmlOSMedia(media, reader);
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.Text:
                        textNode.Append(reader.Value);
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "Media")
                        {
                            throw new XmlException();
                        }
                        if(!haveNodeInside)
                        {
                            /* this is stringified version */
                            using (MemoryStream ms = new MemoryStream(textNode.ToString().ToUTF8Bytes()))
                            {
                                using (XmlTextReader insetReader = new XmlTextReader(ms))
                                {
                                    FromStringifiedXmlOSMedia(media, insetReader);
                                }
                            }
                        }
                        return media;

                    default:
                        break;
                }
            }
        }

        public void ToXml(XmlTextWriter writer)
        {
            writer.WriteStartElement("OSMedia");
            {
                writer.WriteAttributeString("type", "sl");
                writer.WriteAttributeString("version", "0.1");
                writer.WriteStartElement("OSData");
                {
                    writer.WriteStartElement("llsd");
                    {
                        writer.WriteStartElement("array");
                        {
                            foreach (Entry e in this)
                            {
                                e.ToXml(writer);
                            }
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }
}
