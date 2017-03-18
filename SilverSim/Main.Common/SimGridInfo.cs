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

using Nini.Config;
using SilverSim.Types;
using SilverSim.Updater;
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

        static bool TryInstallingFromPackage(string gridId)
        {
            try
            {
                CoreUpdater.Instance.UpdatePackageFeed();
            }
            catch
            {
                return false;
            }

            /* check if we can get one from package feed */
            try
            {
                CoreUpdater.Instance.InstallPackage("SilverSim.GridInfo." + gridId);
                return true;
            }
            catch
            {
                return false;
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

retry:
            if(!File.Exists(filename))
            {
                if(TryInstallingFromPackage(gridId) && filename != gridspecificfile)
                {
                    filename = gridspecificfile;
                }
                else
                {
                    return false;
                }
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
                                bool success = LoadFromGridsXml_Root(config, reader, gridId);
                                if(!success && filename != gridspecificfile)
                                {
                                    if(TryInstallingFromPackage(gridId))
                                    {
                                        filename = gridspecificfile;
                                        goto retry;
                                    }
                                }
                                return success;

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
