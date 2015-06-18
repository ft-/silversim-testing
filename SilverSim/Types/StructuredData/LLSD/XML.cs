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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace SilverSim.StructuredData.LLSD
{
    public static class LLSD_XML
    {
        class InvalidLLSDXmlSerialization : Exception { }

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
                    throw new InvalidLLSDXmlSerialization();
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
                            throw new InvalidLLSDXmlSerialization();
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

        private static IValue DeserializeInternal(XmlTextReader input)
        {
            string element = input.Name;
            switch(input.Name)
            {
                case "array":
                    if(input.IsEmptyElement)
                    {
                        return new AnArray();
                    }
                    AnArray array = new AnArray();
                    while(true)
                    {
                        if(!input.Read())
                        {
                            throw new InvalidLLSDXmlSerialization();
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
                                    throw new InvalidLLSDXmlSerialization();
                                }

                            case XmlNodeType.Element:
                                array.Add(DeserializeInternal(input));
                                break;

                            default:
                                break;
                        }
                    }

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
                        throw new InvalidLLSDXmlSerialization();
                    }

                case "date":
                    return new Date(GetTextNode(input));

                case "integer":
                    if(input.IsEmptyElement)
                    {
                        return new Integer();
                    }
                    return new Integer(Int32.Parse(GetTextNode(input)));

                case "map":
                    if(input.IsEmptyElement)
                    {
                        return new Map();
                    }
                    Map map = new Map();
                    bool in_entity = false;
                    string key = string.Empty;
                    while(true)
                    {
                        if(!input.Read())
                        {
                            throw new InvalidLLSDXmlSerialization();
                        }

                        switch(input.NodeType)
                        {
                            case XmlNodeType.EndElement:
                                if(input.Name == "map")
                                {
                                    if(in_entity)
                                    {
                                        throw new InvalidLLSDXmlSerialization();
                                    }
                                    return map;
                                }
                                else
                                {
                                    throw new InvalidLLSDXmlSerialization();
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
                    if(input.IsEmptyElement)
                    {
                        return new Real();
                    }
                    return Real.Parse(GetTextNode(input));

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
                    throw new InvalidLLSDXmlSerialization();
            }
        }

        public static IValue DeserializeLLSDNode(XmlTextReader input)
        {
            IValue value = null;

            while(true)
            {
                if(!input.Read())
                {
                    throw new InvalidLLSDXmlSerialization();
                }

                switch(input.NodeType)
                {
                    case XmlNodeType.Element:
                        if(value != null)
                        {
                            throw new InvalidLLSDXmlSerialization();
                        }
                        value = DeserializeInternal(input);
                        break;

                    case XmlNodeType.EndElement:
                        if(input.Name != "llsd")
                        {
                            throw new InvalidLLSDXmlSerialization();
                        }
                        if(value == null)
                        {
                            throw new InvalidLLSDXmlSerialization();
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
            return Deserialize(new XmlTextReader(input));
        }

        public static IValue Deserialize(XmlTextReader inp)
        {
            while (true)
            {
                if (!inp.Read())
                {
                    throw new InvalidLLSDXmlSerialization();
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
                            throw new InvalidLLSDXmlSerialization();
                        }
                }
            }
        }

        #region Main LLSD+XML Serialization

        private static void SerializeInternal(IValue input, XmlTextWriter output)
        {
            if (input is SilverSim.Types.Map)
            {
                output.WriteStartElement("map");

                foreach (KeyValuePair<string, IValue> kvp in (Map)input)
                {
                    output.WriteStartElement("key");
                    output.WriteString(kvp.Key);
                    output.WriteEndElement();

                    SerializeInternal(kvp.Value, output);
                }
                output.WriteEndElement();
            }
            else if (input is AnArray)
            {
                output.WriteStartElement("array");
                AnArray i = (AnArray)input;

                foreach (IValue v in i)
                {
                    SerializeInternal(v, output);
                }

                output.WriteEndElement();
            }
            else if (input is ABoolean)
            {
                output.WriteStartElement("boolean");
                if ((ABoolean)input)
                {
                    output.WriteValue("1");
                }
                else
                {
                    output.WriteValue("0");
                }
                output.WriteEndElement();
            }
            else if (input is Date)
            {
                output.WriteStartElement("date");
                output.WriteValue(((Date)input).ToString());
                output.WriteEndElement();
            }
            else if (input is Integer)
            {
                Integer i = (Integer)input;
                output.WriteStartElement("integer");
                output.WriteValue((Int32)i);
                output.WriteEndElement();
            }
            else if (input is Quaternion)
            {
                Quaternion i = (Quaternion)input;
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
            else if (input is Real)
            {
                output.WriteStartElement("real");
                output.WriteValue(input.AsReal);
                output.WriteEndElement();
            }
            else if (input is AString)
            {
                output.WriteStartElement("string");
                output.WriteValue(input.ToString());
                output.WriteEndElement();
            }
            else if (input is Undef)
            {
                output.WriteStartElement("undef");
                output.WriteEndElement();
            }
            else if (input is URI)
            {
                output.WriteStartElement("uri");
                output.WriteValue(input.ToString());
                output.WriteEndElement();
            }
            else if (input is UUID)
            {
                output.WriteStartElement("uuid");
                output.WriteValue(((UUID)input).ToString());
                output.WriteEndElement();
            }
            else if (input is Vector3)
            {
                Vector3 i = (Vector3)input;
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
            else if(input is BinaryData)
            {
                byte[] data = ((BinaryData)input);
                output.WriteStartElement("binary");
                if (data.Length != 0)
                {
                    output.WriteBase64(data, 0, data.Length);
                }
                output.WriteEndElement();
            }
            else
            {
                throw new ArgumentException("Failed to serialize LLSD+Binary");
            }
        }
        #endregion Main LLSD+XML Serialization

        public static void Serialize(IValue value, Stream output)
        {
            XmlTextWriter text = new XmlTextWriter(output, UTF8NoBOM);
            Serialize(value, text);
            text.Flush();
        }

        public static void Serialize(IValue value, XmlTextWriter text)
        {
            text.WriteStartElement("llsd");
            SerializeInternal(value, text);
            text.WriteEndElement();
        }

        private static Encoding UTF8NoBOM = new System.Text.UTF8Encoding(false);
    }
}
