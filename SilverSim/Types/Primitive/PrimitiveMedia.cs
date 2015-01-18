using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreadedClasses;
using SilverSim.Types;
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

            public bool IsAutoLoop = false;
            public bool IsAutoPlay = false;
            public bool IsAutoScale = false;
            public bool IsAutoZoom = false;
            public int Width;
            public int Height;
            public PrimitiveMediaPermission ControlPermissions = PrimitiveMediaPermission.All;
            public PrimitiveMediaControls Controls = PrimitiveMediaControls.Standard;
            public string CurrentURL = string.Empty;
            public bool IsAlternativeImageEnabled = false;
            public bool IsWhiteListEnabled = false;
            public string HomeURL = string.Empty;
            public bool IsInteractOnFirstClick = true;
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
                {
                    Map m = new Map();
                    m.Add("alt_image_enable", e.IsAlternativeImageEnabled);
                    m.Add("auto_loop", e.IsAutoLoop);
                    m.Add("auto_play", e.IsAutoPlay);
                    m.Add("auto_scale", e.IsAutoScale);
                    m.Add("auto_zoom", e.IsAutoZoom);
                    m.Add("controls", (int)e.Controls);
                    m.Add("current_url", e.CurrentURL);
                    m.Add("first_click_interact", e.IsInteractOnFirstClick);
                    m.Add("width_pixels", e.Width);
                    m.Add("height_pixels", e.Height);
                    m.Add("home_url", e.HomeURL);
                    m.Add("perms_control", (int)e.ControlPermissions);
                    m.Add("perms_interact", (int)e.InteractPermissions);
                    AnArray whiteList = new AnArray();
                    if (e.WhiteList != null && e.WhiteList.Length > 0)
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
            }

            public void ToXml(XmlTextWriter writer)
            {
                writer.WriteStartElement("map");
                lock(this)
                {
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
                    if (WhiteList != null && WhiteList.Length > 0)
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
                }
                writer.WriteEndElement();
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
