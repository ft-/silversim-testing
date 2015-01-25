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

using System;
using System.Globalization;
using System.Xml;

namespace SilverSim.Types
{
    public static class XmlExtensionMethods
    {
        public static void WriteKeyValuePair(this XmlTextWriter writer, string key, string value)
        {
            writer.WriteStartElement("key");
            writer.WriteValue(key);
            writer.WriteEndElement();
            writer.WriteStartElement("string");
            writer.WriteValue(value);
            writer.WriteEndElement();
        }

        public static void WriteKeyValuePair(this XmlTextWriter writer, string key, uint value)
        {
            writer.WriteStartElement("key");
            writer.WriteValue(key);
            writer.WriteEndElement();
            writer.WriteStartElement("integer");
            writer.WriteValue(value);
            writer.WriteEndElement();
        }

        public static void WriteKeyValuePair(this XmlTextWriter writer, string key, int value)
        {
            writer.WriteStartElement("key");
            writer.WriteValue(key);
            writer.WriteEndElement();
            writer.WriteStartElement("integer");
            writer.WriteValue(value);
            writer.WriteEndElement();
        }

        public static void WriteKeyValuePair(this XmlTextWriter writer, string key, float value)
        {
            writer.WriteStartElement("key");
            writer.WriteValue(key);
            writer.WriteEndElement();
            writer.WriteStartElement("real");
            writer.WriteValue(value);
            writer.WriteEndElement();
        }

        public static void WriteKeyValuePair(this XmlTextWriter writer, string key, bool value)
        {
            writer.WriteStartElement("key");
            writer.WriteValue(key);
            writer.WriteEndElement();
            writer.WriteStartElement("boolean");
            writer.WriteValue(value ? "1" : "0");
            writer.WriteEndElement();
        }

        public static void WriteKeyValuePair(this XmlTextWriter writer, string key, double value)
        {
            writer.WriteStartElement("key");
            writer.WriteValue(key);
            writer.WriteEndElement();
            writer.WriteStartElement("real");
            writer.WriteValue(((float)value).ToString(EnUsCulture));
            writer.WriteEndElement();
        }

        public static void WriteKeyValuePair(this XmlTextWriter writer, string key, UUID value)
        {
            writer.WriteStartElement("key");
            writer.WriteValue(key);
            writer.WriteEndElement();
            writer.WriteStartElement("uuid");
            writer.WriteValue(value);
            writer.WriteEndElement();
        }

        public static void WriteNamedValue(this XmlTextWriter writer, string name, string value)
        {
            writer.WriteStartElement(name);
            writer.WriteValue(value);
            writer.WriteEndElement();
        }

        public static void WriteNamedValue(this XmlTextWriter writer, string name, UUID value)
        {
            writer.WriteStartElement(name);
            writer.WriteValue(value);
            writer.WriteEndElement();
        }

        public static void WriteNamedValue(this XmlTextWriter writer, string name, int value)
        {
            writer.WriteStartElement(name);
            writer.WriteValue(value);
            writer.WriteEndElement();
        }

        public static void WriteNamedValue(this XmlTextWriter writer, string name, uint value)
        {
            writer.WriteStartElement(name);
            writer.WriteValue(value);
            writer.WriteEndElement();
        }

        public static void WriteNamedValue(this XmlTextWriter writer, string name, float value)
        {
            writer.WriteStartElement(name);
            writer.WriteValue(value.ToString(EnUsCulture));
            writer.WriteEndElement();
        }

        public static void WriteNamedValue(this XmlTextWriter writer, string name, double value)
        {
            writer.WriteStartElement(name);
            writer.WriteValue(value.ToString(EnUsCulture));
            writer.WriteEndElement();
        }

        public static void WriteNamedValue(this XmlTextWriter writer, string name, bool value)
        {
            writer.WriteStartElement(name);
            writer.WriteValue(value.ToString().ToLower());
            writer.WriteEndElement();
        }

        public static void WriteNamedValue(this XmlTextWriter writer, string name, Vector3 value)
        {
            writer.WriteNamedValue(name + "X", value.X_String);
            writer.WriteNamedValue(name + "Y", value.Y_String);
            writer.WriteNamedValue(name + "Z", value.Z_String);
        }

        public static void WriteNamedValue(this XmlTextWriter writer, string name, Quaternion value)
        {
            writer.WriteNamedValue(name + "X", value.X_String);
            writer.WriteNamedValue(name + "Y", value.Y_String);
            writer.WriteNamedValue(name + "Z", value.Z_String);
            writer.WriteNamedValue(name + "W", value.W_String);
        }

        public static void WriteNamedValue(this XmlTextWriter writer, string name, ColorAlpha value)
        {
            writer.WriteNamedValue(name + "R", value.R_AsByte);
            writer.WriteNamedValue(name + "G", value.G_AsByte);
            writer.WriteNamedValue(name + "B", value.B_AsByte);
            writer.WriteNamedValue(name + "A", value.A_AsByte);
        }

        public static void WriteNamedValue(this XmlTextWriter writer, string name, Color value)
        {
            writer.WriteNamedValue(name + "R", value.R_AsByte);
            writer.WriteNamedValue(name + "G", value.G_AsByte);
            writer.WriteNamedValue(name + "B", value.B_AsByte);
        }

        public static void WriteNamedValue(this XmlTextWriter writer, string name, byte[] value)
        {
            writer.WriteStartElement(name);
            writer.WriteBase64(value, 0, value.Length);
            writer.WriteEndElement();
        }

        public static void WriteUUID(this XmlTextWriter writer, string name, UUID uuid)
        {
            writer.WriteStartElement(name);
            writer.WriteNamedValue("UUID", uuid);
            writer.WriteEndElement();
        }

        public static T ReadContentAsEnum<T>(this XmlTextReader reader)
        {
            string value = reader.ReadElementContentAsString();
            if (value.Contains(" ") && !value.Contains(","))
                value = value.Replace(" ", ", ");

            return (T)Enum.Parse(typeof(T), value);
        }

        public static UUID ReadContentAsUUID(this XmlTextReader reader)
        {
            string name = reader.Name;
            if(reader.IsEmptyElement)
            {
                return UUID.Zero;
            }

            do
            {
                if (!reader.Read())
                {
                    throw new XmlException();
                }
            } while (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.Attribute);
            
            if(reader.NodeType != XmlNodeType.Element)
            {
                return new UUID(reader.ReadContentAsString()); /* they did three types of serialization for this and this is the third without inner element */
            }

            UUID res = new UUID(reader.ReadContentAsString());
            do
            {
                if (!reader.Read())
                {
                    throw new XmlException();
                }
            } while (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.Attribute);

            if (reader.NodeType != XmlNodeType.EndElement)
            {
                throw new XmlException();
            }
            if(reader.Name != name)
            {
                throw new XmlException();
            }
            return res;

        }

        public static byte[] ReadContentAsBase64(this XmlTextReader reader)
        {
            return Convert.FromBase64String(reader.ReadContentAsString());
        }

        private readonly static CultureInfo EnUsCulture = new CultureInfo("en-us");
    }
}
