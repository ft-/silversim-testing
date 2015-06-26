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

using System.IO;
using System.Xml;

namespace SilverSim.Archiver.IAR
{
    public static partial class IAR
    {
        static class ArchiveXmlLoader
        {
            static void LoadArchiveXml(
                XmlTextReader reader)
            {
                uint majorVersion = 0;
                uint minorVersion = 0;

                for (; ; )
                {
                    if (!reader.Read())
                    {
                        throw new IARFormatException();
                    }

                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.IsEmptyElement)
                            {
                                throw new IARFormatException();
                            }
                            if (reader.Name != "archive")
                            {
                                throw new IARFormatException();
                            }

                            if (reader.MoveToFirstAttribute())
                            {
                                do
                                {
                                    switch (reader.Name)
                                    {
                                        case "major_version":
                                            majorVersion = uint.Parse(reader.Value);
                                            break;

                                        case "minor_version":
                                            minorVersion = uint.Parse(reader.Value);
                                            break;

                                        default:
                                            break;
                                    }
                                }
                                while (reader.MoveToNextAttribute());
                            }

                            if (majorVersion == 0 && minorVersion == 0)
                            {
                                throw new IARFormatException();
                            }
                            else if (majorVersion == 0)
                            {
                                if(!reader.IsEmptyElement)
                                {
                                    reader.Skip();
                                }
                                return;
                            }
                            else
                            {
                                throw new IARFormatException();
                            }
                    }
                }
            }

            public static void LoadArchiveXml(
                Stream s)
            {
                using (XmlTextReader reader = new XmlTextReader(s))
                {
                    LoadArchiveXml(reader);
                }
            }
        }
    }
}
