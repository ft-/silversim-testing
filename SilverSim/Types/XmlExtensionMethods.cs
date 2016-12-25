// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;

namespace SilverSim.Types
{
    public static class XmlExtensionMethods
    {
        public static void WriteKeyValuePair(this XmlTextWriter writer, string key, string value)
        {
            writer.WriteNamedValue("key", key);
            writer.WriteNamedValue("string", value);
        }

        public static void WriteKeyValuePair(this XmlTextWriter writer, string key, uint value)
        {
            writer.WriteNamedValue("key", key);
            writer.WriteNamedValue("integer", value);
        }

        public static void WriteKeyValuePair(this XmlTextWriter writer, string key, int value)
        {
            writer.WriteNamedValue("key", key);
            writer.WriteNamedValue("integer", value);
        }

        public static void WriteKeyValuePair(this XmlTextWriter writer, string key, float value)
        {
            writer.WriteNamedValue("key", key);
            writer.WriteNamedValue("real", value);
        }

        public static void WriteKeyValuePair(this XmlTextWriter writer, string key, bool value)
        {
            writer.WriteNamedValue("key", key);
            writer.WriteNamedValue("boolean", value ? "1" : "0");
        }

        public static void WriteKeyValuePair(this XmlTextWriter writer, string key, double value)
        {
            writer.WriteNamedValue("key", key);
            writer.WriteNamedValue("real", ((float)value).ToString(CultureInfo.InvariantCulture));
        }

        public static void WriteKeyValuePair(this XmlTextWriter writer, string key, UUID value)
        {
            writer.WriteNamedValue("key", key);
            writer.WriteNamedValue("uuid", (string)value);
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
            writer.WriteValue((string)value);
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
            writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        public static void WriteNamedValue(this XmlTextWriter writer, string name, double value)
        {
            writer.WriteStartElement(name);
            writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
        }

        public static void WriteNamedValue(this XmlTextWriter writer, string name, bool value)
        {
            writer.WriteStartElement(name);
            writer.WriteValue(value.ToString().ToLower());
            writer.WriteEndElement();
        }

        public static void WriteNamedValue(this XmlTextWriter writer, string name, Vector3 value, bool septags = false)
        {
            if (septags)
            {
                writer.WriteNamedValue(name + "X", value.X_String);
                writer.WriteNamedValue(name + "Y", value.Y_String);
                writer.WriteNamedValue(name + "Z", value.Z_String);
            }
            else
            {
                writer.WriteStartElement(name);
                writer.WriteNamedValue("X", value.X_String);
                writer.WriteNamedValue("Y", value.Y_String);
                writer.WriteNamedValue("Z", value.Z_String);
                writer.WriteEndElement();
            }
        }

        public static void WriteNamedValue(this XmlTextWriter writer, string name, Quaternion value)
        {
            writer.WriteStartElement(name);
            writer.WriteNamedValue("X", value.X_String);
            writer.WriteNamedValue("Y", value.Y_String);
            writer.WriteNamedValue("Z", value.Z_String);
            writer.WriteNamedValue("W", value.W_String);
            writer.WriteEndElement();
        }

        public static void WriteNamedValue(this XmlTextWriter writer, string name, ColorAlpha value, bool septags = false)
        {
            if (septags)
            {
                writer.WriteNamedValue(name + "R", value.R_AsByte);
                writer.WriteNamedValue(name + "G", value.G_AsByte);
                writer.WriteNamedValue(name + "B", value.B_AsByte);
                writer.WriteNamedValue(name + "A", value.A_AsByte);
            }
            else
            {
                writer.WriteStartElement(name);
                writer.WriteNamedValue("R", value.R_AsByte);
                writer.WriteNamedValue("G", value.G_AsByte);
                writer.WriteNamedValue("B", value.B_AsByte);
                writer.WriteNamedValue("A", value.A_AsByte);
                writer.WriteEndElement();
            }
        }

        public static void WriteNamedValue(this XmlTextWriter writer, string name, Color value)
        {
            writer.WriteStartElement(name);
            writer.WriteNamedValue("R", value.R_AsByte);
            writer.WriteNamedValue("G", value.G_AsByte);
            writer.WriteNamedValue("B", value.B_AsByte);
            writer.WriteEndElement();
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
            writer.WriteNamedValue("UUID", (string)uuid);
            writer.WriteEndElement();
        }

        public static T ReadContentAsEnum<T>(this XmlTextReader reader)
        {
            string value = reader.ReadElementValueAsString();
            if (value.Contains(" ") && !value.Contains(","))
            {
                value = value.Replace(" ", ", ");
            }

            return (T)Enum.Parse(typeof(T), value);
        }

        public static T ReadContentAsEnumValue<T>(this XmlTextReader reader)
        {
            string value = reader.ReadElementValueAsString();
            return (T)Enum.Parse(typeof(T), value);
        }

        public static UUID ReadContentAsUUID(this XmlTextReader reader)
        {
            string name = reader.Name;
            if(reader.IsEmptyElement)
            {
                return UUID.Zero;
            }

            XmlNodeType nodeType;
            do
            {
                if (!reader.Read())
                {
                    throw new XmlException();
                }
                nodeType = reader.NodeType;
            } while (nodeType == XmlNodeType.Text || nodeType == XmlNodeType.Attribute || nodeType == XmlNodeType.Whitespace);

            if (nodeType != XmlNodeType.Element)
            {
                return new UUID(reader.ReadContentAsString()); /* they did three types of serialization for this and this is the third without inner element */
            }

            UUID res = new UUID(reader.ReadElementValueAsString());
            do
            {
                if (!reader.Read())
                {
                    throw new XmlException();
                }
                nodeType = reader.NodeType;
            } while (nodeType == XmlNodeType.Text || nodeType == XmlNodeType.Attribute || nodeType == XmlNodeType.Whitespace);

            if (nodeType != XmlNodeType.EndElement)
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
            if(reader.IsEmptyElement)
            {
                return new byte[0];
            }
            return Convert.FromBase64String(reader.ReadElementValueAsString());
        }

        public static void ReadToEndElement(this XmlTextReader reader, string tagname = null)
        {
            if (string.IsNullOrEmpty(tagname))
            {
                tagname = reader.Name;
            }
            XmlNodeType nodeType = reader.NodeType;
            if((nodeType == XmlNodeType.Element || nodeType == XmlNodeType.Attribute) && !reader.IsEmptyElement)
            {
                do
                {
                nextelem:
                    if(!reader.Read())
                    {
                        throw new XmlException("Premature end of XML", null, reader.LineNumber, reader.LinePosition);
                    }
                    nodeType = reader.NodeType;
                    if(nodeType == XmlNodeType.Element)
                    {
                        ReadToEndElement(reader);
                        goto nextelem;
                    }
                } while (nodeType != XmlNodeType.EndElement);
                if(tagname != reader.Name)
                {
                    throw new XmlException("Closing tag does not match", null, reader.LineNumber, reader.LinePosition);
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public static int ReadElementValueAsInt(this XmlTextReader reader)
        {
            if(reader.IsEmptyElement)
            {
                return 0;
            }
            return int.Parse(ReadElementValueAsString(reader));
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public static uint ReadElementValueAsUInt(this XmlTextReader reader)
        {
            if (reader.IsEmptyElement)
            {
                return 0;
            }
            return uint.Parse(ReadElementValueAsString(reader));
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public static long ReadElementValueAsLong(this XmlTextReader reader)
        {
            if(reader.IsEmptyElement)
            {
                return 0;
            }
            return long.Parse(ReadElementValueAsString(reader));
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public static ulong ReadElementValueAsULong(this XmlTextReader reader)
        {
            if (reader.IsEmptyElement)
            {
                return 0;
            }
            return ulong.Parse(ReadElementValueAsString(reader));
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public static double ReadElementValueAsFloat(this XmlTextReader reader)
        {
            if (reader.IsEmptyElement)
            {
                return 0;
            }
            return float.Parse(ReadElementValueAsString(reader), CultureInfo.InvariantCulture);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public static double ReadElementValueAsDouble(this XmlTextReader reader)
        {
            if (reader.IsEmptyElement)
            {
                return 0;
            }
            return double.Parse(ReadElementValueAsString(reader), CultureInfo.InvariantCulture);
        }

        public static ColorAlpha ReadElementChildsAsColorAlpha(this XmlTextReader reader)
        {
            string tagname = reader.Name;
            ColorAlpha v = ColorAlpha.White;
            if (reader.IsEmptyElement)
            {
                return v;
            }
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new XmlException("Premature end of XML", null, reader.LineNumber, reader.LinePosition);
                }

                string nodeName = reader.Name;

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (nodeName)
                        {
                            case "R":
                                v.R_AsByte = (byte)reader.ReadElementValueAsUInt();
                                break;

                            case "G":
                                v.G_AsByte = (byte)reader.ReadElementValueAsUInt();
                                break;

                            case "B":
                                v.B_AsByte = (byte)reader.ReadElementValueAsUInt();
                                break;

                            case "A":
                                v.A_AsByte = (byte)reader.ReadElementValueAsUInt();
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (nodeName != tagname)
                        {
                            throw new XmlException("Closing tag does not match", null, reader.LineNumber, reader.LinePosition);
                        }

                        return v;

                    default:
                        break;
                }
            }
        }

        public static Vector3 ReadElementChildsAsVector3(this XmlTextReader reader)
        {
            string tagname = reader.Name;
            Vector3 v = Vector3.Zero;
            if(reader.IsEmptyElement)
            {
                return v;
            }
            for(;;)
            {
                if(!reader.Read())
                {
                    throw new XmlException("Premature end of XML", null, reader.LineNumber, reader.LinePosition);
                }

                string nodeName = reader.Name;

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (nodeName)
                        {
                            case "X":
                                v.X = reader.ReadElementValueAsDouble();
                                break;

                            case "Y":
                                v.Y = reader.ReadElementValueAsDouble();
                                break;

                            case "Z":
                                v.Z = reader.ReadElementValueAsDouble();
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (nodeName != tagname)
                        {
                            throw new XmlException("Closing tag does not match", null, reader.LineNumber, reader.LinePosition);
                        }

                        return v;

                    default:
                        break;
                }
            }
        }

        public static Quaternion ReadElementChildsAsQuaternion(this XmlTextReader reader)
        {
            string tagname = reader.Name;
            Quaternion v = Quaternion.Identity;
            if (reader.IsEmptyElement)
            {
                return v;
            }
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new XmlException("Premature end of XML", null, reader.LineNumber, reader.LinePosition);
                }

                string nodeName = reader.Name;

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (nodeName)
                        {
                            case "X":
                                v.X = reader.ReadElementValueAsDouble();
                                break;

                            case "Y":
                                v.Y = reader.ReadElementValueAsDouble();
                                break;

                            case "Z":
                                v.Z = reader.ReadElementValueAsDouble();
                                break;

                            case "W":
                                v.W = reader.ReadElementValueAsDouble();
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (nodeName != tagname)
                        {
                            throw new XmlException("Closing tag does not match", null, reader.LineNumber, reader.LinePosition);
                        }

                        return v.Normalize();

                    default:
                        break;
                }
            }
        }

        public static string ReadElementValueAsString(this XmlTextReader reader)
        {
            string tagname = reader.Name;
            if(reader.IsEmptyElement)
            {
                return string.Empty;
            }

            for(;;)
            {
                if(!reader.Read())
                {
                    throw new XmlException("Premature end of XML");
                }

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        throw new XmlException("Unexpected child node");

                    case XmlNodeType.Text:
                        return reader.ReadContentAsString();

                    case XmlNodeType.EndElement:
                        if(reader.Name != tagname)
                        {
                            throw new XmlException("closing tag does not match");
                        }
                        return string.Empty;

                    default:
                        break;
                }
            }
        }

        public static string ReadElementValueAsString(this XmlTextReader reader, string tagname)
        {
            for (;;)
            {
                if (!reader.Read())
                {
                    throw new XmlException("Premature end of XML");
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        throw new XmlException("Unexpected child node");

                    case XmlNodeType.Text:
                        return reader.ReadContentAsString();

                    case XmlNodeType.EndElement:
                        if (reader.Name != tagname)
                        {
                            throw new XmlException("closing tag does not match");
                        }
                        return string.Empty;

                    default:
                        break;
                }
            }
        }

        public static bool ReadElementValueAsBoolean(this XmlTextReader reader)
        {
            string tagname = reader.Name;
            if (reader.IsEmptyElement)
            {
                return false;
            }

            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new XmlException("Premature end of XML");
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        throw new XmlException("Unexpected child node");

                    case XmlNodeType.Text:
                        {
                            string val = reader.ReadContentAsString();
                            int ival;
                            if(val.ToLower() == "true")
                            {
                                return true;
                            }
                            else if(val.ToLower() == "false")
                            {
                                return false;
                            }
                            else
                            {
                                return int.TryParse(val, out ival) && ival != 0;
                            }
                        }

                    case XmlNodeType.EndElement:
                        if (reader.Name != tagname)
                        {
                            throw new XmlException("closing tag does not match");
                        }
                        return false;

                    default:
                        break;
                }
            }
        }

    }
}
