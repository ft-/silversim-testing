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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace SilverSim.Types.StructuredData.Llsd
{
    public static class LlsdXml
    {
        [Serializable]
        public class InvalidLlsdXmlSerializationException : Exception
        {
            public InvalidLlsdXmlSerializationException()
            {
            }

            public InvalidLlsdXmlSerializationException(string message)
                : base(message)
            {
            }

            protected InvalidLlsdXmlSerializationException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }

            public InvalidLlsdXmlSerializationException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

        #region Main LLSD+XML Deserialization
        private static string GetTextNode(XmlTextReader input)
        {
            if(input.IsEmptyElement)
            {
                return string.Empty;
            }
            string elementName = input.Name;
            string data = string.Empty;
            while (true)
            {
                if(!input.Read())
                {
                    throw new InvalidLlsdXmlSerializationException();
                }
                switch(input.NodeType)
                {
                    case XmlNodeType.Element:
                        input.ReadToEndElement();
                        break;

                    case XmlNodeType.Text:
                        return input.ReadContentAsString();

                    case XmlNodeType.EndElement:
                        if(input.Name != elementName)
                        {
                            throw new InvalidLlsdXmlSerializationException();
                        }
                        else
                        {
                            return data;
                        }

                    default:
                        break;
                }
            }
        }

        private static IValue DeserializeBinary(XmlTextReader reader)
        {
            var attrname = string.Empty;
            var encoding = "base64";
            while (reader.ReadAttributeValue())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Attribute:
                        attrname = reader.Value;
                        break;

                    case XmlNodeType.Text:
                        switch (attrname)
                        {
                            case "encoding":
                                encoding = reader.Value;
                                break;

                            default:
                                break;
                        }
                        break;

                    default:
                        break;
                }
            }

            if(encoding != "base64")
            {
                throw new InvalidLlsdXmlSerializationException("Unsupported binary encoding " + encoding);
            }

            while (true)
            {
                if (!reader.Read())
                {
                    throw new InvalidLlsdXmlSerializationException();
                }
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        reader.ReadToEndElement();
                        break;

                    case XmlNodeType.Text:
                        return new BinaryData(Convert.FromBase64String(reader.ReadContentAsString()));

                    case XmlNodeType.EndElement:
                        if (reader.Name != "binary")
                        {
                            throw new InvalidLlsdXmlSerializationException();
                        }
                        else
                        {
                            return new BinaryData();
                        }

                    default:
                        break;
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        private static IValue DeserializeInternal(XmlTextReader input)
        {
            switch(input.Name)
            {
                case "array":
                    if(input.IsEmptyElement)
                    {
                        return new AnArray();
                    }
                    var array = new AnArray();
                    while(true)
                    {
                        if(!input.Read())
                        {
                            throw new InvalidLlsdXmlSerializationException();
                        }

                        switch(input.NodeType)
                        {
                            case XmlNodeType.EndElement:
                                if(input.Name == "array")
                                {
                                    return array;
                                }
                                else
                                {
                                    throw new InvalidLlsdXmlSerializationException();
                                }

                            case XmlNodeType.Element:
                                array.Add(DeserializeInternal(input));
                                break;

                            default:
                                break;
                        }
                    }

                case "binary":
                    if(input.IsEmptyElement)
                    {
                        return new BinaryData();
                    }
                    return DeserializeBinary(input);

                case "boolean":
                    if(input.IsEmptyElement)
                    {
                        return new ABoolean(false);
                    }
                    string boolstr = GetTextNode(input);
                    if (boolstr == "1")
                    {
                        return new ABoolean(true);
                    }
                    else if(boolstr == "0")
                    {
                        return new ABoolean(false);
                    }
                    else
                    {
                        throw new InvalidLlsdXmlSerializationException();
                    }

                case "date":
                    return new Date(GetTextNode(input));

                case "integer":
                    if(input.IsEmptyElement)
                    {
                        return new Integer();
                    }
                    Int32 inp32val;
                    if(!Int32.TryParse(GetTextNode(input), out inp32val))
                    {
                        throw new InvalidLlsdXmlSerializationException();
                    }
                    return new Integer(inp32val);

                case "map":
                    if(input.IsEmptyElement)
                    {
                        return new Map();
                    }
                    var map = new Map();
                    var in_entity = false;
                    var key = string.Empty;
                    while(true)
                    {
                        if(!input.Read())
                        {
                            throw new InvalidLlsdXmlSerializationException();
                        }

                        switch(input.NodeType)
                        {
                            case XmlNodeType.EndElement:
                                if(input.Name == "map")
                                {
                                    if(in_entity)
                                    {
                                        throw new InvalidLlsdXmlSerializationException();
                                    }
                                    return map;
                                }
                                else
                                {
                                    throw new InvalidLlsdXmlSerializationException();
                                }

                            case XmlNodeType.Element:
                                if (in_entity)
                                {
                                    map[key] = DeserializeInternal(input);
                                    in_entity = false;
                                }
                                else
                                {
                                    key = GetTextNode(input);
                                    in_entity = true;
                                }
                                break;

                            default:
                                break;
                        }
                    }

                case "real":
                    if (input.IsEmptyElement)
                    {
                        return new Real();
                    }
                    {
                        Real r_val;
                        if(!Real.TryParse(GetTextNode(input), out r_val))
                        {
                            throw new InvalidLlsdXmlSerializationException();
                        }
                        return r_val;
                    }
                case "string":
                    if(input.IsEmptyElement)
                    {
                        return new AString();
                    }
                    return new AString(GetTextNode(input));

                case "undef":
                    input.ReadToEndElement();
                    return new Undef();

                case "uri":
                    if(input.IsEmptyElement)
                    {
                        return new URI(string.Empty);
                    }
                    return new URI(GetTextNode(input));

                case "uuid":
                    if(input.IsEmptyElement)
                    {
                        return UUID.Zero;
                    }
                    return new UUID(GetTextNode(input));

                default:
                    throw new InvalidLlsdXmlSerializationException();
            }
        }

        public static IValue DeserializeLLSDNode(XmlTextReader input)
        {
            IValue value = null;

            while(true)
            {
                if(!input.Read())
                {
                    throw new InvalidLlsdXmlSerializationException();
                }

                switch(input.NodeType)
                {
                    case XmlNodeType.Element:
                        if(value != null)
                        {
                            throw new InvalidLlsdXmlSerializationException();
                        }
                        value = DeserializeInternal(input);
                        break;

                    case XmlNodeType.EndElement:
                        if(input.Name != "llsd")
                        {
                            throw new InvalidLlsdXmlSerializationException();
                        }
                        if(value == null)
                        {
                            throw new InvalidLlsdXmlSerializationException();
                        }
                        return value;

                    default:
                        break;
                }
            }
        }
        #endregion

        public static IValue Deserialize(Stream input)
        {
            using (var r = new XmlTextReader(input))
            {
                return Deserialize(r);
            }
        }

        public static IValue Deserialize(XmlTextReader inp)
        {
            while (true)
            {
                if (!inp.Read())
                {
                    throw new InvalidLlsdXmlSerializationException();
                }

                switch (inp.NodeType)
                {
                    case XmlNodeType.Element:
                        if (inp.Name == "llsd")
                        {
                            return DeserializeLLSDNode(inp);
                        }
                        else
                        {
                            throw new InvalidLlsdXmlSerializationException();
                        }

                    default:
                        break;
                }
            }
        }

        #region Main LLSD+XML Serialization

        private static void SerializeInternal(IValue input, XmlTextWriter output)
        {
            var t = input.GetType();

            if (t == typeof(Map))
            {
                var i_m = (Map)input;

                output.WriteStartElement("map");

                foreach (var kvp in i_m)
                {
                    output.WriteStartElement("key");
                    output.WriteString(kvp.Key);
                    output.WriteEndElement();

                    SerializeInternal(kvp.Value, output);
                }
                output.WriteEndElement();
            }
            else if (t == typeof(AnArray))
            {
                var i_a = (AnArray)input;
                output.WriteStartElement("array");

                foreach (var v in i_a)
                {
                    SerializeInternal(v, output);
                }

                output.WriteEndElement();
            }
            else if (t == typeof(ABoolean))
            {
                var i_bool = (ABoolean)input;
                output.WriteStartElement("boolean");
                if (i_bool)
                {
                    output.WriteValue("1");
                }
                else
                {
                    output.WriteValue("0");
                }
                output.WriteEndElement();
            }
            else if (t == typeof(Date))
            {
                var i_d = (Date)input;
                output.WriteStartElement("date");
                output.WriteValue(i_d.ToString());
                output.WriteEndElement();
            }
            else if (t == typeof(Integer))
            {
                output.WriteStartElement("integer");
                output.WriteValue(input.AsInt);
                output.WriteEndElement();
            }
            else if (t == typeof(Quaternion))
            {
                var i = (Quaternion)input;
                output.WriteStartElement("array");
                output.WriteStartElement("real");
                output.WriteValue(i.X);
                output.WriteEndElement();
                output.WriteStartElement("real");
                output.WriteValue(i.Y);
                output.WriteEndElement();
                output.WriteStartElement("real");
                output.WriteValue(i.Z);
                output.WriteEndElement();
                output.WriteStartElement("real");
                output.WriteValue(i.W);
                output.WriteEndElement();
                output.WriteEndElement();
            }
            else if (t == typeof(Real))
            {
                output.WriteStartElement("real");
                output.WriteValue(input.AsReal);
                output.WriteEndElement();
            }
            else if (t == typeof(Undef))
            {
                output.WriteStartElement("undef");
                output.WriteEndElement();
            }
            else if (t == typeof(AString))
            {
                output.WriteStartElement("string");
                output.WriteValue(input.ToString());
                output.WriteEndElement();
            }
            else if (t == typeof(URI))
            {
                output.WriteStartElement("uri");
                output.WriteValue(input.ToString());
                output.WriteEndElement();
            }
            else if (t == typeof(UUID))
            {
                output.WriteStartElement("uuid");
                output.WriteValue(((UUID)input).ToString());
                output.WriteEndElement();
            }
            else if (t == typeof(Vector3))
            {
                var i = (Vector3)input;
                output.WriteStartElement("array");
                output.WriteStartElement("real");
                output.WriteValue(i.X);
                output.WriteEndElement();
                output.WriteStartElement("real");
                output.WriteValue(i.Y);
                output.WriteEndElement();
                output.WriteStartElement("real");
                output.WriteValue(i.Z);
                output.WriteEndElement();
                output.WriteEndElement();
            }
            else if(t == typeof(BinaryData))
            {
                byte[] data = (BinaryData)input;
                output.WriteStartElement("binary");
                if (data.Length != 0)
                {
                    output.WriteBase64(data, 0, data.Length);
                }
                output.WriteEndElement();
            }
            else
            {
                throw new ArgumentException("Failed to serialize LLSD+XML");
            }
        }
        #endregion Main LLSD+XML Serialization

        public static void Serialize(IValue value, Stream output)
        {
            using (var text = output.UTF8XmlTextWriter())
            {
                Serialize(value, text);
            }
        }

        public static void Serialize(IValue value, XmlTextWriter text)
        {
            text.WriteStartElement("llsd");
            SerializeInternal(value, text);
            text.WriteEndElement();
        }
    }
}
