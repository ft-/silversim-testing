// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Types;
using System;
using System.IO;
using System.Xml;

namespace SilverSim.Main.Common
{
    public static class SimGridInfo
    {
        const string GridsXml = "../data/SilverSim.gridinfo.xml";

        public class SimGridInfoXmlException : Exception
        {
        }

        static void LoadFromGridsXml_Grid(IConfigSource config, XmlTextReader reader, string tagname)
        {
            for (;;)
            {
                if (!reader.Read())
                {
                    throw new SimGridInfoXmlException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if(reader.Name == "parameter")
                        {
                            bool isEmptyElement = reader.IsEmptyElement;
                            string section = reader.GetAttribute("section");
                            string name = reader.GetAttribute("name");
                            string value = reader.GetAttribute("value");
                            if(section.Length == 0 || name.Length == 0)
                            {
                                throw new SimGridInfoXmlException();
                            }
                            if(config.Configs[section] == null)
                            {
                                config.Configs.Add(section);
                            }
                            IConfig cfg = config.Configs[section];
                            cfg.Set(name, value);
                            if (!isEmptyElement)
                            {
                                reader.ReadToEndElement("parameter");
                            }
                        }
                        else if(!reader.IsEmptyElement)
                        {
                            reader.ReadToEndElement();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != tagname)
                        {
                            throw new SimGridInfoXmlException();
                        }
                        return;

                    default:
                        break;
                }
            }
        }

        static bool LoadFromGridsXml_Root(IConfigSource config, XmlTextReader reader, string gridId)
        {
            for(;;)
            {
                if(!reader.Read())
                {
                    throw new SimGridInfoXmlException();
                }

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if(reader.IsEmptyElement)
                        {
                            break;
                        }
                        if(reader.Name == gridId)
                        {
                            LoadFromGridsXml_Grid(config, reader, reader.Name);
                            return true;
                        }
                        else
                        {
                            reader.ReadToEndElement(reader.Name);
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if(reader.Name != "grids")
                        {
                            throw new SimGridInfoXmlException();
                        }
                        return false;

                    default:
                        break;
                }
            }
        }

        public static bool LoadFromGridsXml(IConfigSource config, string gridId)
        {
            string filename = GridsXml;
            string gridspecificfile = "../data/SilverSim.gridinfo." + gridId + ".xml";
            if (File.Exists(gridspecificfile))
            {
                filename = gridspecificfile;
            }
            if(!File.Exists(filename))
            {
                return false;
            }

            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                using (XmlTextReader reader = new XmlTextReader(fs))
                {
                    for (;;)
                    {
                        if (!reader.Read())
                        {
                            throw new SimGridInfoXmlException();
                        }

                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                if(reader.Name != "grids" || reader.IsEmptyElement)
                                {
                                    throw new SimGridInfoXmlException();
                                }
                                return LoadFromGridsXml_Root(config, reader, gridId);

                            case XmlNodeType.EndElement:
                                throw new SimGridInfoXmlException();

                            default:
                                break;
                        }
                    }
                }
            }
        }
    }
}
