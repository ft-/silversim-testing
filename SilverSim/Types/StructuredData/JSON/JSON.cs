// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace SilverSim.Types.StructuredData.Json
{
    public static class Json
    {
        [Serializable]
        public class InvalidJsonSerializationException : Exception 
        {
            public InvalidJsonSerializationException()
            {

            }

            public InvalidJsonSerializationException(string message)
                : base(message)
            {

            }

            protected InvalidJsonSerializationException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public InvalidJsonSerializationException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }

        #region Main JSON Deserialization
        private static string ReadString(StreamReader io, char eos)
        {
            char c;
            StringBuilder s = new StringBuilder();
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
                    s.Append(c);
                }
                else
                {
                    s.Append(c);
                }
            }
            return s.ToString();
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
                    StringBuilder inputs = new StringBuilder();
                    for (; ;)
                    {
                        c = (char) io.Peek();
                        if(c == ']' || c == ',' | c == '}')
                        {
                            break;
                        }
                        c = (char)io.Read();
                        inputs.Append(c);
                    }

                    string input = inputs.ToString();
                    if (input == "true")
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
                                throw new InvalidJsonSerializationException();
                            }
                            return new Real(f);
                        }
                        else
                        {
                            Int32 i;
                            if(!Int32.TryParse(input, out i))
                            {
                                throw new InvalidJsonSerializationException();
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
                        throw new InvalidJsonSerializationException();
                    }
                    c = (char)bc;

                    if(char.IsWhiteSpace(c))
                    {
                        do
                        {
                            io.Read();
                            c = (char)io.Peek();
                        } while (char.IsWhiteSpace(c));
                    }
                }
                else
                {
                    throw new InvalidJsonSerializationException();
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
                    throw new InvalidJsonSerializationException();
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
                        throw new InvalidJsonSerializationException();
                    }
                    c = (char)bc;

                    if(char.IsWhiteSpace(c))
                    {
                        do
                        {
                            io.Read();
                            c = (char)io.Peek();
                        } while (char.IsWhiteSpace(c));
                    }
                }
                else
                {
                    throw new InvalidJsonSerializationException();
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
                    throw new InvalidJsonSerializationException();
                }
                return ParseValue(sr);
            }
        }

        #region Main JSON Serialization
        public static string SerializeString(string s)
        {
            StringBuilder o = new StringBuilder();
            for(int i = 0; i < s.Length; ++i)
            {
                switch(s[i])
                {
                    case '\\':
                        o.Append("\\\\");
                        break;
                    case '\n':
                        o.Append("\\n");
                        break;
                    case '\r':
                        o.Append("\\r");
                        break;
                    case '\"':
                        o.Append("\\\"");
                        break;
                    case '\'':
                        o.Append("\\'");
                        break;
                    default:
                        o.Append(s[i]);
                        break;
                }
            }
            return o.ToString();
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
            Type t = val.GetType();

            if(t == typeof(Map))
            {
                SerializeStruct(io, (Map)val);
            }
            else if(t == typeof(URI))
            {
                io.Write("\"" + SerializeString(val.ToString()) + "\"");
            }
            else if(t == typeof(UUID))
            {
                io.Write("\"" + SerializeString(val.ToString()) + "\"");
            }
            else if(t == typeof(Date))
            {
                Date date = (Date)val;
                DateTime dt = date;
                io.Write("\"" + dt.ToUniversalTime().ToString() + "\"");
            }
            else if(t == typeof(ABoolean))
            {
                ABoolean boolean = (ABoolean)val;
                io.Write(boolean ? "true" : "false");
            }
            else if(t == typeof(Real))
            {
                io.Write(val.ToString());
            }
            else if(t == typeof(Integer))
            {
                io.Write(val.ToString());
            }
            else if(t == typeof(Undef))
            {
                io.Write("null");
            }
            else if(t == typeof(AString))
            {
                io.Write("\"" + SerializeString(val.ToString()) + "\"");
            }
            else if(t == typeof(AnArray))
            {
                SerializeArray(io, (AnArray)val);
            }
            else
            {
                throw new InvalidJsonSerializationException();
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
