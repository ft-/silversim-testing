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

using System.Collections.Generic;
using System.IO;

namespace SilverSim.Types.MuteList
{
    public static class MuteListExtensionMethods
    {
        public static byte[] ToBinaryData(this List<MuteListEntry> list)
        {
            using (var ms = new MemoryStream())
            {
                using (StreamWriter writer = ms.UTF8StreamWriter())
                {
                    foreach (MuteListEntry entry in list)
                    {
                        writer.Write("{0} {1} {2}|{3}\n", (int)entry.Type, entry.MuteID.ToString(), entry.MuteName, (uint)entry.Flags);
                    }
                }
                return ms.ToArray();
            }
        }

        public static List<MuteListEntry> ToMuteList(this byte[] data)
        {
            var res = new List<MuteListEntry>();
            string datastring = data.FromUTF8Bytes();
            foreach(string elem in datastring.Split('\n'))
            {
                string[] outer = elem.Split('|');
                if(outer.Length != 2)
                {
                    continue;
                }

                string[] inner = outer[0].Split(new char[] { ' ' }, 3);
                if(inner.Length < 3)
                {
                    continue;
                }
                res.Add(new MuteListEntry
                {
                    Flags = (MuteFlags)uint.Parse(outer[1]),
                    MuteName = inner[2],
                    MuteID = UUID.Parse(inner[1]),
                    Type = (MuteType)int.Parse(inner[0])
                });
            }
            return res;
        }
    }
}
