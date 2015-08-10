// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using System;
using System.Text;

namespace SilverSim.Scripting.LSL.API.Base
{
    public partial class Base_API
    {
        [APILevel(APIFlags.LSL)]
        public string llIntegerToBase64(ScriptInstance Instance, int number)
        {
            byte[] b = BitConverter.GetBytes(number);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            return System.Convert.ToBase64String(b);
        }

        [APILevel(APIFlags.LSL)]
        public int llBase64ToInteger(ScriptInstance Instance, string s)
        {
            if (s.Length > 8)
            {
                return 0;
            }
            string i = s.PadRight(8, '=');
            byte[] b = System.Convert.FromBase64String(i);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b, 0, 4);
            }
            return BitConverter.ToInt32(b, 0);
        }

        [APILevel(APIFlags.LSL)]
        public string llStringToBase64(ScriptInstance Instance, string str)
        {
            byte[] b = Encoding.UTF8.GetBytes(str);
            return System.Convert.ToBase64String(b);
        }

        [APILevel(APIFlags.LSL)]
        public string llBase64ToString(ScriptInstance Instance, string str)
        {
            byte[] b = System.Convert.FromBase64String(str);
            return Encoding.UTF8.GetString(b);
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(0.3)]
        public string llXorBase64(ScriptInstance Instance, string str1, string str2)
        {
            byte[] a = System.Convert.FromBase64String(str1);
            byte[] b = System.Convert.FromBase64String(str2);
            byte[] o = new byte[a.Length];

            for (int i = 0; i < a.Length; ++i)
            {
                o[i] = (byte)(a[i] ^ b[i % b.Length]);
            }
            return System.Convert.ToBase64String(o);
        }
    }
}
