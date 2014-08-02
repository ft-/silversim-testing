using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public string llIntegerToBase64(int number)
        {
            byte[] b = BitConverter.GetBytes(number);
            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            return System.Convert.ToBase64String(b);
        }

        public int llBase64ToInteger(string s)
        {
            if(s.Length > 8)
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

        public string llStringToBase64(string str)
        {
            byte[] b = Encoding.UTF8.GetBytes(str);
            return System.Convert.ToBase64String(b);
        }

        public string llBase64ToString(string str)
        {
            byte[] b = System.Convert.FromBase64String(str);
            return Encoding.UTF8.GetString(b);
        }

        public string llXorBase64(string str1, string str2)
        {
            byte[] a = System.Convert.FromBase64String(str1);
            byte[] b = System.Convert.FromBase64String(str2);
            byte[] o = new byte[a.Length];

            for(int i = 0; i < a.Length; ++i)
            {
                o[i] = (byte)(a[i] ^ b[i % b.Length]);
            }
            return System.Convert.ToBase64String(o);
        }
    }
}
