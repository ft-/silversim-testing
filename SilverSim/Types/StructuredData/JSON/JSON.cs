// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;

namespace SilverSim.StructuredData.JSON
{
    public static class JSON
    {
        [Serializable]
        public class InvalidJSONSerializationException : Exception 
        {
            public InvalidJSONSerializationException()
            {

            }
        }

        #region Main JSON Deserialization
        private static string ReadString(StreamReader io, char eos)
        {
            char c;
            string s = string.Empty;
            while(eos != (c = (char) io.Read()))
            {
                if(c == '\\')
                {
                    /* next goes straight into string when it is not \n or \r */
                    c = (char)io.Read();
                    if(c == 'r')
                    {
                        c = '\r';
                    }
                    else if(c == 'n')
                    {
                        c = '\n';
                    }
                    s += c.ToString();
                }
                else
                {
                    s += c.ToString();
                }
            }
            return s;
        }

        private static IValue ParseValue(StreamReader io)
        {
            char c;
            for (; ;)
            {
                c = (char)io.Peek();
                if(char.IsWhiteSpace(c))
                {
                    io.Read();
                }
                else
                {
                    break;
                }
            }
            switch(c)
            {
                case '\"':
                    io.Read();
                    return new AString(ReadString(io, '\"'));

                case '\'':
                    io.Read();
                    return new AString(ReadString(io, '\''));

                case '[':
                    io.Read();
                    return ParseArray(io);

                case '{':
                    io.Read();
                    return ParseMap(io);

                default:
                    string input = string.Empty;
                    for (; ;)
                    {
                        c = (char) io.Peek();
                        if(c == ']' || c == ',' | c == '}')
                        {
                            break;
                        }
                        c = (char)io.Read();
                        input += c.ToString();
                    }

                    if(input == "true")
                    {
                        return new ABoolean(true);
                    }
                    else if(input == "false")
                    {
                        return new ABoolean(false);
                    }
                    else if(input == "null")
                    {
                        return new Undef();
                    }
                    else
                    {
                        if(input.IndexOf('.') >= 0)
                        {
                            double f;
                            if(!Double.TryParse(input, out f))
                            {
                                throw new InvalidJSONSerializationException();
                            }
                            return new Real(f);
                        }
                        else
                        {
                            Int32 i;
                            if(!Int32.TryParse(input, out i))
                            {
                                throw new InvalidJSONSerializationException();
                            }
                            return new Integer(i);
                        }
                    }
            }
        }

        private static AnArray ParseArray(StreamReader io)
        {
            AnArray array = new AnArray();
            char c;

            c = (char)io.Peek();
            if(']' == c)
            {
                c = (char)io.Read();
                return array;
            }

            for (; ;)
            {
                array.Add(ParseValue(io));
                do
                {
                    c = (char)io.Read();
                } while (char.IsWhiteSpace(c));

                if(c == ']')
                {
                    return array;
                }
                else if(c==',')
                {
                    int bc = io.Peek();
                    if(bc == -1)
                    {
                        throw new InvalidJSONSerializationException();
                    }
                    c = (char)bc;

                    if(char.IsWhiteSpace(c))
                    {
                        do
                        {
                            c = (char)io.Read();
                            c = (char)io.Peek();
                        } while (char.IsWhiteSpace(c));
                    }
                }
                else
                {
                    throw new InvalidJSONSerializationException();
                }
            }
        }

        private static Map ParseMap(StreamReader io)
        {
            Map map = new Map();
            char c;

            c = (char)io.Peek();
            if('}' == c)
            {
                c = (char)io.Read();
                return map;
            }

            for (; ;)
            {
                IValue key = ParseValue(io);
                c = (char)io.Read();
                if(c != ':')
                {
                    throw new InvalidJSONSerializationException();
                }
                map[key.ToString()] = ParseValue(io);
                do
                {
                    c = (char)io.Read();
                } while(char.IsWhiteSpace(c));

                if(c == '}')
                {
                    return map;
                }
                else if(c == ',')
                {
                    int bc = io.Peek();
                    if(bc == -1)
                    {
                        throw new InvalidJSONSerializationException();
                    }
                    c = (char)bc;

                    if(char.IsWhiteSpace(c))
                    {
                        do
                        {
                            c = (char)io.Read();
                            c = (char)io.Peek();
                        } while (char.IsWhiteSpace(c));
                    }
                }
                else
                {
                    throw new InvalidJSONSerializationException();
                }
            }
        }
        #endregion

        public static IValue Deserialize(Stream io)
        {
            using(StreamReader sr = new StreamReader(io))
            {
                char c = (char)sr.Peek();
                if(c != '{' && c != '[')
                {
                    throw new InvalidJSONSerializationException();
                }
                return ParseValue(sr);
            }
        }

        #region Main JSON Serialization
        public static string SerializeString(string s)
        {
            string o = string.Empty;
            for(int i = 0; i < s.Length; ++i)
            {
                switch(s[i])
                {
                    case '\\': o += "\\\\"; break;
                    case '\n': o += "\\n"; break;
                    case '\r': o += "\\r"; break;
                    case '\"': o += "\\\""; break;
                    case '\'': o += "\\'"; break;
                    default: o += s[i].ToString(); break;
                }
            }
            return o;
        }

        private static void SerializeStruct(TextWriter io, Map map)
        {
            io.Write('{');
            string needcomma = string.Empty;
            foreach(KeyValuePair<string, IValue> kvp in map)
            {
                io.Write(needcomma);
                io.Write("\"" + SerializeString(kvp.Key) + "\":");
                SerializeData(io, kvp.Value);
                needcomma = ",";
            }
            io.Write('}');
        }
        private static void SerializeArray(TextWriter io, AnArray arr)
        {
            io.Write('[');
            string needcomma = string.Empty;
            foreach(IValue val in arr)
            {
                io.Write(needcomma);
                SerializeData(io, val);
                needcomma = ",";
            }
            io.Write("]");
        }

        private static void SerializeData(TextWriter io, IValue val)
        {
            if(val is Map)
            {
                SerializeStruct(io, (Map)val);
            }
            else if(val is URI)
            {
                io.Write("\"" + SerializeString(val.ToString()) + "\"");
            }
            else if(val is UUID)
            {
                io.Write("\"" + SerializeString(val.ToString()) + "\"");
            }
            else if(val is Date)
            {
                DateTime dt = (Date)val;
                io.Write("\"" + dt.ToUniversalTime().ToString() + "\"");
            }
            else if(val is ABoolean)
            {
                if((ABoolean)val)
                {
                    io.Write("true");
                }
                else
                {
                    io.Write("false");
                }
            }
            else if(val is Real)
            {
                io.Write(val.ToString());
            }
            else if(val is Integer)
            {
                io.Write(val.ToString());
            }
            else if(val is Undef)
            {
                io.Write("null");
            }
            else if(val is AString)
            {
                io.Write("\"" + SerializeString(val.ToString()) + "\"");
            }
            else if(val is AnArray)
            {
                SerializeArray(io, (AnArray)val);
            }
            else
            {
                throw new InvalidJSONSerializationException();
            }
        }
        #endregion

        public static void Serialize(IValue val, Stream output)
        {
            using(TextWriter tw = new StreamWriter(output))
            {
                SerializeData(tw, val);
            }
        }

        public static string Serialize(IValue val)
        {
            using(MemoryStream m = new MemoryStream())
            {
                Serialize(val, m);
                return System.Text.Encoding.UTF8.GetString(m.GetBuffer());
            }
        }
    }
}
