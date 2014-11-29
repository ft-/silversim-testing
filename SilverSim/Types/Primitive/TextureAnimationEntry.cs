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

namespace SilverSim.Types.Primitive
{
    public class TextureAnimationEntry
    {
        [Flags]
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

        public TextureAnimMode Flags = TextureAnimMode.ANIM_OFF;
        public sbyte Face = 0;
        public byte SizeX = 0;
        public byte SizeY = 0;
        public float Start = 0;
        public float Length = 0;
        public float Rate = 0;

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
                SizeX = (byte)data[pos++];
                SizeY = (byte)data[pos++];
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
