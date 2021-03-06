﻿// SilverSim is distributed under the terms of the
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
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace SilverSim.Types.StructuredData.Json
{
    public static class Json
    {
        private static readonly Regex m_NumberRegex = new Regex("^-{0,1}(0|[1-9][,0-9]*)(|(|\\.[0-9]*)(|[eE][\\-+]{0,1}[0-9]+))$");
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
            var s = new StringBuilder();
            while(eos != (c = (char) io.Read()))
            {
                if(c == '\\')
                {
                    /* next goes straight into string when it is not \n or \r */
                    c = (char)io.Read();
                    switch(c)
                    {
                        case 'r':
                            c = '\r';
                            break;
                        case 'n':
                            c = '\n';
                            break;
                        case 'b':
                            c = '\b';
                            break;
                        case 'f':
                            c = '\f';
                            break;
                        case 't':
                            c = '\t';
                            break;
                        case '\"':
                        case '\\':
                        case '/':
                            break;
                        case 'u':
                            char[] b = new char[4];
                            if(4 != io.Read(b, 0, 4))
                            {
                                throw new InvalidJsonSerializationException();
                            }
                            uint cvalue;
                            if(!uint.TryParse(new string(b), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out cvalue))
                            {
                                throw new InvalidJsonSerializationException();
                            }
                            c = (char)cvalue;
                            break;
                        default:
                            throw new InvalidJsonSerializationException();
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
                int ci;
                ci = io.Peek();
                if(ci == -1)
                {
                    throw new InvalidJsonSerializationException();
                }
                c = (char)ci;
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
                    var inputs = new StringBuilder();
                    for (; ;)
                    {
                        int ci = io.Peek();
                        c = (char) ci;
                        if(c == ']' || c == ',' || c == '}' || ci == -1)
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
                        int i;
                        double f;
                        if(!m_NumberRegex.IsMatch(input))
                        {
                            throw new InvalidJsonSerializationException();
                        }
                        else if (int.TryParse(input, out i))
                        {
                            return new Integer(i);
                        }
                        else if (double.TryParse(input, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out f))
                        {
                            return new Real(f);
                        }
                        else
                        {
                            throw new InvalidJsonSerializationException();
                        }
                    }
            }
        }

        private static AnArray ParseArray(StreamReader io)
        {
            var array = new AnArray();
            var c = (char)io.Peek();
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
            var map = new Map();
            var c = (char)io.Peek();
            if('}' == c)
            {
                c = (char)io.Read();
                return map;
            }

            for (; ;)
            {
                var key = ParseValue(io);
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
            using(var sr = new StreamReader(io))
            {
                var c = (char)sr.Peek();
                return ParseValue(sr);
            }
        }

        #region Main JSON Serialization
        public static string SerializeString(string s)
        {
            var o = new StringBuilder();
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
                    case '\f':
                        o.Append("\\f");
                        break;
                    case '\b':
                        o.Append("\\b");
                        break;
                    case '\r':
                        o.Append("\\r");
                        break;
                    case '\"':
                        o.Append("\\\"");
                        break;
                    case '\t':
                        o.Append("\\t");
                        break;
                    case '/':
                        o.Append("\\/");
                        break;
                    default:
                        if (s[i] < 0x20)
                        {
                            o.Append("\\u");
                            o.Append(((int)s[i]).ToString("{0:x4}"));
                        }
                        else
                        {
                            o.Append(s[i]);
                        }
                        break;
                }
            }
            return o.ToString();
        }

        private static void SerializeStruct(TextWriter io, Map map)
        {
            io.Write('{');
            string needcomma = string.Empty;
            foreach(var kvp in map)
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
            foreach(var val in arr)
            {
                io.Write(needcomma);
                SerializeData(io, val);
                needcomma = ",";
            }
            io.Write("]");
        }

        private static void SerializeData(TextWriter io, IValue val)
        {
            var t = val.GetType();

            if(t == typeof(Map))
            {
                SerializeStruct(io, (Map)val);
            }
            else if(t == typeof(URI) || t == typeof(UUID) || t == typeof(AString))
            {
                io.Write("\"" + SerializeString(val.ToString()) + "\"");
            }
            else if(t == typeof(Date))
            {
                DateTime dt = (Date)val;
                io.Write("\"" + dt.ToUniversalTime().ToString() + "\"");
            }
            else if(t == typeof(ABoolean))
            {
                var boolean = (ABoolean)val;
                io.Write(boolean ? "true" : "false");
            }
            else if(t == typeof(Real))
            {
                string s = val.ToString();
                if (!s.Contains("E") && !s.Contains("e"))
                {
                    int pos = s.IndexOf('.');
                    if (pos < 0)
                    {
                        s += ".0";
                    }
                }

                io.Write(s);
            }
            else if (t == typeof(Integer))
            {
                io.Write(val.ToString());
            }
            else if (t == typeof(Undef))
            {
                io.Write("null");
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
            using(var tw = output.UTF8StreamWriterLeaveOpen())
            {
                SerializeData(tw, val);
            }
        }

        public static string Serialize(IValue val)
        {
            using(var m = new MemoryStream())
            {
                Serialize(val, m);
                return Encoding.UTF8.GetString(m.ToArray());
            }
        }
    }
}
