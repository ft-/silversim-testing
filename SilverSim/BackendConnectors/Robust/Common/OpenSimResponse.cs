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
using System;
using System.IO;
using System.Xml;

namespace SilverSim.BackendConnectors.Robust.Common
{
    public static class OpenSimResponse
    {
        public class InvalidOpenSimResponseSerialization : Exception
        {
            public InvalidOpenSimResponseSerialization()
            {

            }
        }

        private static AString parseValue(XmlTextReader reader)
        {
            AString astring = new AString();
            string tagname = reader.Name;
            while(true)
            {
                if(!reader.Read())
                {
                    throw new InvalidOpenSimResponseSerialization();
                }

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        throw new InvalidOpenSimResponseSerialization();

                    case XmlNodeType.Text:
                        return new AString(reader.ReadContentAsString());

                    case XmlNodeType.EndElement:
                        if(reader.Name != tagname)
                        {
                            throw new InvalidOpenSimResponseSerialization();
                        }
                        return astring;
                }
            }
        }

        private static Map parseMap(XmlTextReader reader)
        {
            string tagname = reader.Name;
            Map map = new Map();
            while(true)
            {
                if (!reader.Read())
                {
                    throw new InvalidOpenSimResponseSerialization();
                }

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if(reader.GetAttribute("type") == "List")
                        {
                            map[reader.Name] = parseMap(reader);
                        }
                        else
                        {
                            map[reader.Name] = parseValue(reader);
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != tagname)
                        {
                            throw new InvalidOpenSimResponseSerialization();
                        }

                        return map;
                }
            }
        }

        public static Map Deserialize(Stream input)
        {
            using(XmlTextReader reader = new XmlTextReader(input))
            {
                while(true)
                {
                    if(!reader.Read())
                    {
                        throw new InvalidOpenSimResponseSerialization();
                    }

                    if(reader.NodeType == XmlNodeType.Element)
                    {
                        if(reader.Name != "ServerResponse")
                        {
                            throw new InvalidOpenSimResponseSerialization();
                        }
                        else
                        {
                            return parseMap(reader);
                        }
                    }
                }
            }
        }
    }
}
