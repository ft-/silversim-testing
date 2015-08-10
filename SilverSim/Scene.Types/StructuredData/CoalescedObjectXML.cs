// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Scene.Types.Object;
using System.Xml;
using SilverSim.Types;
using System.Globalization;

namespace SilverSim.Scene.Types.StructuredData
{
    public static partial class ObjectXML2
    {
        public static void Serialize(XmlTextWriter writer, List<ObjectGroup> objects, Vector3 basePosition)
        {
            writer.WriteStartElement("CoalescedObject");
            writer.WriteAttributeString("x", basePosition.X_String);
            writer.WriteAttributeString("y", basePosition.Y_String);
            writer.WriteAttributeString("z", basePosition.Z_String);

            foreach(ObjectGroup obj in objects)
            {
                Vector3 objpos = obj.Position - basePosition;
                writer.WriteStartElement("SceneObjectGroup");
                writer.WriteAttributeString("offsetx", objpos.X_String);
                writer.WriteAttributeString("offsety", objpos.Y_String);
                writer.WriteAttributeString("offsetz", objpos.Z_String);

                Serialize(writer, obj);

                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }
}
