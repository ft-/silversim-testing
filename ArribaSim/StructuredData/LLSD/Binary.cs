/*

ArribaSim is distributed under the terms of the
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

using ArribaSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ArribaSim.StructuredData.LLSD
{
    public static class LLSD_Binary
    {
        class InvalidLLSDBinarySerialization : Exception { }

        #region Main LLSD+Binary Deserialization
        private static Int32 ReadInt32(Stream input)
        {
            byte[] data = new byte[4];
            if (4 != input.Read(data, 0, 4))
            {
                throw new InvalidLLSDBinarySerialization();
            }

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }
            return BitConverter.ToInt32(data, 0);
        }

        private static IValue DeserializeInternal(Stream input)
        {
            byte[] data;
            Int32 datalen;
            Int32 elemCount;

            switch(input.ReadByte())
            {
                case '!':
                    return new Undef();

                case '1':
                    return new ABoolean(true);

                case '0':
                    return new ABoolean(false);

                case 'i':
                    return new Integer(ReadInt32(input));
                   
                case 'r':
                    data = new byte[8];
                    if (8 != input.Read(data, 0, 8))
                    {
                        throw new InvalidLLSDBinarySerialization();
                    }

                    if(!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(data);
                    }

                    return new Real(BitConverter.ToDouble(data, 0));

                case 'u':
                    data = new byte[16];
                    if(16 != input.Read(data, 0, 16))
                    {
                        throw new InvalidLLSDBinarySerialization();
                    }

                    return new UUID(data, 0);

                case 'b':
                    datalen = ReadInt32(input);

                    data = new byte[datalen];

                    if (datalen != input.Read(data, 0, datalen))
                    {
                        throw new InvalidLLSDBinarySerialization();
                    }

                    return new BinaryData(data);

                case 's':
                    datalen = ReadInt32(input);

                    data = new byte[datalen];

                    if (datalen != input.Read(data, 0, datalen))
                    {
                        throw new InvalidLLSDBinarySerialization();
                    }

                    return new AString(Encoding.UTF8.GetString(data));

                case 'l':
                    datalen = ReadInt32(input);

                    data = new byte[datalen];

                    if (datalen != input.Read(data, 0, datalen))
                    {
                        throw new InvalidLLSDBinarySerialization();
                    }

                    return new URI(Encoding.UTF8.GetString(data));

                case 'd':
                    data = new byte[8];
                    if(8 != input.Read(data, 0, 8))
                    {
                        throw new InvalidLLSDBinarySerialization();
                    }

                    return new Date(data, 0);

                case '[':
                    AnArray newArray = new AnArray();
                    elemCount = ReadInt32(input);
                    while(elemCount-- > 0)
                    {
                        newArray.Add(DeserializeInternal(input));
                    }
                    if(']' != input.ReadByte())
                    {
                        throw new InvalidLLSDBinarySerialization();
                    }
                    return newArray;

                case '{':
                    Map newMap = new Map();
                    elemCount = ReadInt32(input);
                    while(elemCount-- > 0)
                    {
                        string key;
                        datalen = ReadInt32(input);

                        data = new byte[datalen];

                        if (datalen != input.Read(data, 0, datalen))
                        {
                            throw new InvalidLLSDBinarySerialization();
                        }

                        key = Encoding.UTF8.GetString(data);

                        newMap[key] = DeserializeInternal(input);

                    }
                    if('}' != input.ReadByte())
                    {
                        throw new InvalidLLSDBinarySerialization();
                    }
                    return newMap;

                default:
                    throw new InvalidLLSDBinarySerialization();
            }
        }
        #endregion Main LLSD+Binary Deserialization

        public static IValue Deserialize(Stream input)
        {
            string a = string.Empty;
            int b;
            while(0xa != (b = input.ReadByte()))
            {
                a += (char)b;
            }
            if(a != "<? LLSD/Binary ?>")
            {
                throw new InvalidLLSDBinarySerialization();
            }
            return DeserializeInternal(input);
        }

        #region Main LLSD+Binary Serialization
        private static void SerializeInternal(IValue input, Stream output)
        {
            if (input is ArribaSim.Types.Map)
            {
                Map i = (Map)input;
                output.WriteByte((byte)'{');
                byte[] cnt = BitConverter.GetBytes(i.Count);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(cnt);
                }
                output.Write(cnt, 0, cnt.Length);

                foreach (KeyValuePair<string, IValue> kvp in (Map)input)
                {
                    output.WriteByte((byte)'s');
                    byte[] str = Encoding.UTF8.GetBytes(kvp.Key);
                    cnt = BitConverter.GetBytes(str.Length);
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(cnt);
                    }
                    output.Write(cnt, 0, cnt.Length);
                    output.Write(str, 0, str.Length);

                    SerializeInternal(kvp.Value, output);
                }
                output.WriteByte((byte)'}');
            }
            else if (input is AnArray)
            {
                AnArray i = (AnArray) input;
                output.WriteByte((byte)'[');
                byte[] cnt = BitConverter.GetBytes(i.Count);
                if(!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(cnt);
                }
                output.Write(cnt, 0, 4);

                foreach(IValue v in i)
                {
                    SerializeInternal(v, output);
                }

                output.WriteByte((byte)']');
            }
            else if(input is ABoolean)
            {
                if((ABoolean)input)
                {
                    output.WriteByte((byte)'1');
                }
                else
                {
                    output.WriteByte((byte)'0');
                }
            }
            else if(input is Date)
            {
                output.WriteByte((byte)'d');
                byte[] db = new byte[8];
                ((Date)input).ToBytes(db, 0);
                output.Write(db, 0, 8);
            }
            else if(input is Integer)
            {
                output.WriteByte((byte)'i');
                byte[] db = new byte[4];
                ((Integer)input).ToBytes(db, 0);
                output.Write(db, 0, 4);
            }
            else if(input is Quaternion)
            {
                Quaternion i = (Quaternion) input;
                byte[] db = new byte[8];
                db[0] = (byte)'[';
                db[1] = 0; db[2] = 0; db[3] = 0; db[4] = 4;
                db[5] = (byte)'r';
                output.Write(db, 0, 6);

                db = BitConverter.GetBytes(i.X);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(db);
                output.Write(db, 0, 8);

                output.WriteByte((byte)'r');
                db = BitConverter.GetBytes(i.Y);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(db);
                output.Write(db, 0, 8);

                output.WriteByte((byte)'r');
                db = BitConverter.GetBytes(i.Z);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(db);
                output.Write(db, 0, 8);

                output.WriteByte((byte)'r');
                db = BitConverter.GetBytes(i.W);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(db);
                output.Write(db, 0, 8);

                output.WriteByte((byte)']');
            }
            else if(input is Real)
            {
                output.WriteByte((byte)'r');
                byte[] db = new byte[8];
                ((Real)input).ToBytes(db, 0);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(db);
                output.Write(db, 0, 8);
            }
            else if(input is AString)
            {
                AString i = (AString)input;
                output.WriteByte((byte)'s');
                byte[] str = Encoding.UTF8.GetBytes(i.ToString());
                byte[] cnt = BitConverter.GetBytes(str.Length);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(cnt);
                }
                output.Write(cnt, 0, cnt.Length);
                output.Write(str, 0, str.Length);
            }
            else if(input is Undef)
            {
                output.WriteByte((byte)'!');
            }
            else if(input is URI)
            {
                URI i = (URI)input;
                output.WriteByte((byte)'l');
                byte[] str = Encoding.UTF8.GetBytes(i.ToString());
                byte[] cnt = BitConverter.GetBytes(str.Length);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(cnt);
                }
                output.Write(cnt, 0, cnt.Length);
                output.Write(str, 0, str.Length);
            }
            else if(input is UUID)
            {
                output.WriteByte((byte)'u');
                byte[] db = new byte[16];
                ((UUID)input).ToBytes(db, 0);
                output.Write(db, 0, 16);
            }
            else if (input is Vector3)
            {
                Vector3 i = (Vector3)input;
                byte[] db = new byte[6];
                db[0] = (byte)'[';
                db[1] = 0; db[2] = 0; db[3] = 0; db[4] = 3;
                db[5] = (byte)'r';
                output.Write(db, 0, db.Length);

                db = BitConverter.GetBytes(i.X);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(db);
                output.Write(db, 0, db.Length);

                output.WriteByte((byte)'r');
                db = BitConverter.GetBytes(i.Y);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(db);
                output.Write(db, 0, db.Length);

                output.WriteByte((byte)'r');
                db = BitConverter.GetBytes(i.Z);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(db);
                output.Write(db, 0, db.Length);

                output.WriteByte((byte)']');
            }
            else if(input is BinaryData)
            {
                byte[] data = ((BinaryData)input);
                output.WriteByte((byte)'u');
                byte[] cnt = BitConverter.GetBytes(data.Length);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(cnt);
                }
                output.Write(cnt, 0, cnt.Length);
                output.Write(data, 0, data.Length);
            }
            else
            {
                throw new ArgumentException("Failed to serialize LLSD+Binary");
            }
        }
        #endregion Main LLSD+Binary Serialization

        public static void Serialize(IValue value, Stream output)
        {
            output.Write(m_BinaryHeader, 0, m_BinaryHeader.Length);
            SerializeInternal(value, output);
        }

        private static readonly byte[] m_BinaryHeader = new byte[]{
                (byte)'<',
                (byte)'?',
                (byte)' ',
                (byte)'L',
                (byte)'L',
                (byte)'S',
                (byte)'D',
                (byte)'/',
                (byte)'B',
                (byte)'i',
                (byte)'n',
                (byte)'a',
                (byte)'r',
                (byte)'y',
                (byte)' ',
                (byte)'?',
                (byte)'>',
                0x0A
        };
    }
}
