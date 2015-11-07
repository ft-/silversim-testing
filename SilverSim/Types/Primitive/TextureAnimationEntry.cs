// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types.Primitive
{
    public class TextureAnimationEntry
    {
        [Flags]
        [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
        public enum TextureAnimMode : byte
        {
            ANIM_OFF = 0x00,
            ANIM_ON = 0x01,
            LOOP = 0x02,
            REVERSE = 0x04,
            PING_PONG = 0x08,
            SMOOTH = 0x10,
            ROTATE = 0x20,
            SCALE = 0x40
        }

        public TextureAnimMode Flags;
        public sbyte Face;
        public byte SizeX;
        public byte SizeY;
        public float Start;
        public float Length;
        public float Rate;

        public TextureAnimationEntry()
        {
        }

        private static float BytesToFloat(byte[] bytes, int pos)
        {
            if (!BitConverter.IsLittleEndian)
            {
                byte[] newBytes = new byte[4];
                Buffer.BlockCopy(bytes, pos, newBytes, 0, 4);
                Array.Reverse(newBytes, 0, 4);
                return BitConverter.ToSingle(newBytes, 0);
            }
            else
            {
                return BitConverter.ToSingle(bytes, pos);
            }
        }

        private static byte[] FloatToBytes(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }


        public TextureAnimationEntry(byte[] data, int pos)
        {
            if (data.Length - pos >= 16)
            {
                Flags = (TextureAnimMode)data[pos++];
                Face = (sbyte)data[pos++];
                SizeX = data[pos++];
                SizeY = data[pos++];
                Start = BytesToFloat(data, pos);
                Length = BytesToFloat(data, pos + 4);
                Rate = BytesToFloat(data, pos + 8);
            }
        }

        public byte[] GetBytes()
        {
            byte[] data = new byte[16];

            data[0] = (byte)Flags;
            data[1] = (byte)Face;
            data[2] = SizeX;
            data[3] = SizeY;
            FloatToBytes(Start).CopyTo(data, 4);
            FloatToBytes(Length).CopyTo(data, 8);
            FloatToBytes(Rate).CopyTo(data, 12);

            return data;
        }
    }
}
