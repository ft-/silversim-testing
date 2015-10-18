// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.StructuredData.LLSD;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using ThreadedClasses;

namespace SilverSim.Scene.Types.WindLight
{
    public class EnvironmentSettings
    {
        public readonly RwLockedList<KeyValuePair<double, string>> DayCycle = new RwLockedList<KeyValuePair<double, string>>();
        public readonly RwLockedDictionary<string, SkyEntry> SkySettings = new RwLockedDictionary<string, SkyEntry>();
        WaterEntry m_WaterSettings;

        public WaterEntry WaterSettings
        {
            get
            {
                lock(this)
                {
                    return m_WaterSettings;
                }
            }
            set
            {
                lock(this)
                {
                    m_WaterSettings = value;
                }
            }
        }

        public EnvironmentSettings()
        {

        }

        public EnvironmentSettings(EnvironmentSettings env)
        {

        }

        static UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);

        public void Serialize(Stream s, UUID regionID)
        {
            using(XmlTextWriter writer = new XmlTextWriter(s, UTF8NoBOM))
            {
                Serialize(writer, regionID);
            }
        }

        public void Serialize(XmlTextWriter writer, UUID regionID)
        {
            writer.WriteStartElement("llsd");
            {
                writer.WriteStartElement("array");
                {
                    writer.WriteStartElement("map");
                    {
                        writer.WriteNamedValue("key", "messageID");
                        writer.WriteNamedValue("uuid", UUID.Zero);
                        writer.WriteNamedValue("key", "regionID");
                        writer.WriteNamedValue("uuuid", regionID);
                    }
                    writer.WriteEndElement();

                    writer.WriteStartElement("array");
                    foreach(KeyValuePair<double, string> kvp in DayCycle)
                    {
                        writer.WriteNamedValue("real", kvp.Key);
                        writer.WriteNamedValue("string", kvp.Value);
                    }
                    writer.WriteEndElement();

                    writer.WriteStartElement("map");
                    foreach (KeyValuePair<string, SkyEntry> entry in SkySettings)
                    {
                        writer.WriteNamedValue("key", entry.Key);
                        entry.Value.Serialize(writer);
                    }
                    writer.WriteEndElement();

                    WaterSettings.Serialize(writer);
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        [Serializable]
        public class EnvironmentSettingsSerializationException : Exception
        {
            public EnvironmentSettingsSerializationException()
            {

            }
        }

        public static EnvironmentSettings Deserialize(Stream input)
        {
            EnvironmentSettings env = new EnvironmentSettings();
            IValue iv;
            iv = LLSD_XML.Deserialize(input);
            if(!(iv is AnArray))
            {
                throw new EnvironmentSettingsSerializationException();
            }
            AnArray a = (AnArray)iv;

            AnArray dayCycleArray = (AnArray)a[1];
            Map skyArray = (Map)a[2];
            Map waterSettings = (Map)a[3];

            for (int i = 0; i < dayCycleArray.Count - 1; i += 2 )
            {
                env.DayCycle.Add(new KeyValuePair<double, string>(dayCycleArray[i + 0].AsReal, dayCycleArray[i + 1].ToString()));
            }

            foreach (KeyValuePair<string, IValue> kvp in skyArray)
            {
                if (kvp.Value is Map)
                {
                    env.SkySettings.Add(kvp.Key, new SkyEntry((Map)kvp.Value));
                }
            }

            env.WaterSettings = new WaterEntry(waterSettings);

            return env;
        }
    }
}
