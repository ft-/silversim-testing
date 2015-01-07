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
