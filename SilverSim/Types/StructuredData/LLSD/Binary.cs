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
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace SilverSim.Types.StructuredData.Llsd
{
    public static class LlsdBinary
    {
        [Serializable]
        public class InvalidLlsdBinarySerializationException : Exception 
        {
            public InvalidLlsdBinarySerializationException()
            {

            }

            public InvalidLlsdBinarySerializationException(string message)
                : base(message)
            {

            }

            protected InvalidLlsdBinarySerializationException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public InvalidLlsdBinarySerializationException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }

        #region Main LLSD+Binary Deserialization
        private static Int32 ReadInt32(Stream input)
        {
            byte[] data = new byte[4];
            if (4 != input.Read(data, 0, 4))
            {
                throw new InvalidLlsdBinarySerializationException();
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }
            return BitConverter.ToInt32(data, 0);
        }

        private static IValue DeserializeInternal(Stream input, bool mayHaveHeader = true)
        {
            byte[] data;
            Int32 datalen;
            Int32 elemCount;

            int typeByte = input.ReadByte();

            if(typeByte == '<' && mayHaveHeader)
            {
                StringBuilder a = new StringBuilder();
                int b;
                while (0xa != (b = input.ReadByte()))
                {
                    a.Append((char)b);
                }
                if (a.ToString() != "<? LLSD/Binary ?>")
                {
                    throw new InvalidLlsdBinarySerializationException();
                }

                typeByte = input.ReadByte();
            }

            switch (typeByte)
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
                        throw new InvalidLlsdBinarySerializationException();
                    }

                    if(BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(data);
                    }

                    return new Real(BitConverter.ToDouble(data, 0));

                case 'u':
                    data = new byte[16];
                    if(16 != input.Read(data, 0, 16))
                    {
                        throw new InvalidLlsdBinarySerializationException();
                    }

                    return new UUID(data, 0);

                case 'b':
                    datalen = ReadInt32(input);

                    data = new byte[datalen];

                    if (datalen != input.Read(data, 0, datalen))
                    {
                        throw new InvalidLlsdBinarySerializationException();
                    }

                    return new BinaryData(data);

                case 's':
                    datalen = ReadInt32(input);

                    data = new byte[datalen];

                    if (datalen != input.Read(data, 0, datalen))
                    {
                        throw new InvalidLlsdBinarySerializationException();
                    }

                    return new AString(Encoding.UTF8.GetString(data));

                case 'l':
                    datalen = ReadInt32(input);

                    data = new byte[datalen];

                    if (datalen != input.Read(data, 0, datalen))
                    {
                        throw new InvalidLlsdBinarySerializationException();
                    }

                    return new URI(Encoding.UTF8.GetString(data));

                case 'd':
                    data = new byte[8];
                    if(8 != input.Read(data, 0, 8))
                    {
                        throw new InvalidLlsdBinarySerializationException();
                    }

                    if(BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(data);
                    }

                    return new Date(data, 0);

                case '[':
                    AnArray newArray = new AnArray();
                    elemCount = ReadInt32(input);
                    while(elemCount-- > 0)
                    {
                        newArray.Add(DeserializeInternal(input, false));
                    }
                    if(']' != input.ReadByte())
                    {
                        throw new InvalidLlsdBinarySerializationException();
                    }
                    return newArray;

                case '{':
                    Map newMap = new Map();
                    elemCount = ReadInt32(input);
                    while(elemCount-- > 0)
                    {
                        string key;
                        if(input.ReadByte() != (byte)'k')
                        {
                            throw new InvalidLlsdBinarySerializationException();
                        }
                        datalen = ReadInt32(input);

                        data = new byte[datalen];

                        if (datalen != input.Read(data, 0, datalen))
                        {
                            throw new InvalidLlsdBinarySerializationException();
                        }

                        key = Encoding.UTF8.GetString(data);

                        newMap[key] = DeserializeInternal(input, false);

                    }
                    if('}' != input.ReadByte())
                    {
                        throw new InvalidLlsdBinarySerializationException();
                    }
                    return newMap;

                default:
                    throw new InvalidLlsdBinarySerializationException();
            }
        }
        #endregion Main LLSD+Binary Deserialization

        public static IValue Deserialize(Stream input)
        {
            return DeserializeInternal(input);
        }

        #region Main LLSD+Binary Serialization
        private static void SerializeInternal(IValue input, Stream output)
        {
            Type t = input.GetType();

            if (t == typeof(Map))
            {
                Map i_m = (Map)input;
                output.WriteByte((byte)'{');
                byte[] cnt = BitConverter.GetBytes(i_m.Count);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(cnt);
                }
                output.Write(cnt, 0, cnt.Length);

                foreach (KeyValuePair<string, IValue> kvp in i_m)
                {
                    output.WriteByte((byte)'k');
                    byte[] str = Encoding.UTF8.GetBytes(kvp.Key);
                    cnt = BitConverter.GetBytes(str.Length);
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(cnt);
                    }
                    output.Write(cnt, 0, cnt.Length);
                    output.Write(str, 0, str.Length);

                    SerializeInternal(kvp.Value, output);
                }
                output.WriteByte((byte)'}');
            }
            else if (t == typeof(AnArray))
            {
                AnArray i_a = (AnArray)input;
                output.WriteByte((byte)'[');
                byte[] cnt = BitConverter.GetBytes(i_a.Count);
                if(BitConverter.IsLittleEndian)
                {
                    Array.Reverse(cnt);
                }
                output.Write(cnt, 0, 4);

                foreach(IValue v in i_a)
                {
                    SerializeInternal(v, output);
                }

                output.WriteByte((byte)']');
            }
            else if(t == typeof(ABoolean))
            {
                ABoolean i_bool = (ABoolean)input;
                output.WriteByte(i_bool ? (byte)'1' : (byte)'0');
            }
            else if(t == typeof(Date))
            {
                Date i_d = (Date)input;
                output.WriteByte((byte)'d');
                byte[] db = new byte[8];
                i_d.ToBytes(db, 0);
                output.Write(db, 0, 8);
            }
            else if (t == typeof(Integer))
            {
                output.WriteByte((byte)'i');
                byte[] db = new byte[4];
                ((SilverSim.Types.Integer)input).ToBytes(db, 0);
                Array.Reverse(db);
                output.Write(db, 0, 4);
            }
            else if(t == typeof(Quaternion))
            {
                Quaternion i = (Quaternion) input;
                byte[] db = new byte[8];
                db[0] = (byte)'[';
                db[1] = 0;
                db[2] = 0;
                db[3] = 0;
                db[4] = 4;
                db[5] = (byte)'r';
                output.Write(db, 0, 6);

                db = BitConverter.GetBytes(i.X);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(db);
                }
                output.Write(db, 0, 8);

                output.WriteByte((byte)'r');
                db = BitConverter.GetBytes(i.Y);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(db);
                }
                output.Write(db, 0, 8);

                output.WriteByte((byte)'r');
                db = BitConverter.GetBytes(i.Z);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(db);
                }
                output.Write(db, 0, 8);

                output.WriteByte((byte)'r');
                db = BitConverter.GetBytes(i.W);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(db);
                }
                output.Write(db, 0, 8);

                output.WriteByte((byte)']');
            }
            else if(t == typeof(Real))
            {
                Real i_real = (Real)input;
                output.WriteByte((byte)'r');
                byte[] db = new byte[8];
                i_real.ToBytes(db, 0);
                Array.Reverse(db);
                output.Write(db, 0, 8);
            }
            else if(t == typeof(AString))
            {
                AString i_string = (AString)input;
                output.WriteByte((byte)'s');
                byte[] str = Encoding.UTF8.GetBytes(i_string.ToString());
                byte[] cnt = BitConverter.GetBytes(str.Length);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(cnt);
                }
                output.Write(cnt, 0, cnt.Length);
                output.Write(str, 0, str.Length);
            }
            else if(t == typeof(Undef))
            {
                output.WriteByte((byte)'!');
            }
            else if(t == typeof(URI))
            {
                URI i_uri = (URI)input;
                output.WriteByte((byte)'l');
                byte[] str = Encoding.UTF8.GetBytes(i_uri.ToString());
                byte[] cnt = BitConverter.GetBytes(str.Length);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(cnt);
                }
                output.Write(cnt, 0, cnt.Length);
                output.Write(str, 0, str.Length);
            }
            else if(t == typeof(UUID))
            {
                output.WriteByte((byte)'u');
                byte[] db = new byte[16];
                ((UUID)input).ToBytes(db, 0);
                output.Write(db, 0, 16);
            }
            else if (t == typeof(Vector3))
            {
                Vector3 i = (Vector3)input;
                byte[] db = new byte[6];
                db[0] = (byte)'[';
                db[1] = 0;
                db[2] = 0;
                db[3] = 0;
                db[4] = 3;
                db[5] = (byte)'r';
                output.Write(db, 0, db.Length);

                db = BitConverter.GetBytes(i.X);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(db);
                }
                output.Write(db, 0, db.Length);

                output.WriteByte((byte)'r');
                db = BitConverter.GetBytes(i.Y);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(db);
                }
                output.Write(db, 0, db.Length);

                output.WriteByte((byte)'r');
                db = BitConverter.GetBytes(i.Z);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(db);
                }
                output.Write(db, 0, db.Length);

                output.WriteByte((byte)']');
            }
            else if(t == typeof(BinaryData))
            {
                BinaryData i_bin = (BinaryData)input;
                byte[] data = i_bin;
                output.WriteByte((byte)'b');
                byte[] cnt = BitConverter.GetBytes(data.Length);
                if (BitConverter.IsLittleEndian)
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

        public static void Serialize(IValue value, Stream output, bool writeHeader = false)
        {
            if (writeHeader)
            {
                output.Write(m_BinaryHeader, 0, m_BinaryHeader.Length);
            }
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
