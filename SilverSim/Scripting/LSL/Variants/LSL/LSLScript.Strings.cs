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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;

namespace SilverSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public string llDeleteSubString(string src, int start, int end)
        {
            if (start < 0)
            {
                start = src.Length - start;
            }
            if (end < 0)
            {
                end = src.Length - end;
            }

            if (start < 0)
            {
                start = 0;
            }
            else if (start > src.Length)
            {
                start = src.Length;
            }

            if (end < 0)
            {
                end = 0;
            }
            else if (end > src.Length)
            {
                end = src.Length;
            }

            if (start > end)
            {
                return src.Substring(start, end - start + 1);
            }
            else
            {
                return src.Substring(0, start + 1) + src.Substring(end);
            }
        }

        public string llToLower(string s)
        {
            return s.ToLower();
        }

        public string llToUpper(string s)
        {
            return s.ToUpper();
        }

        public string llUnescapeURL(string url)
        {
            return Uri.UnescapeDataString(url);
        }

        public string llEscapeURL(string url)
        {
            return Uri.EscapeDataString(url);
        }

        public const int STRING_TRIM_HEAD = 0x1;
        public const int STRING_TRIM_TAIL = 0x2;
        public const int STRING_TRIM = 0x3;

        private readonly char[] trimchars = new char[] { ' ', '\t', '\r', '\n' };

        public string llStringTrim(string src, int type)
        {
            switch(type & STRING_TRIM)
            {
                case STRING_TRIM_HEAD:
                    src = src.TrimStart(trimchars);
                    break;
                case STRING_TRIM_TAIL:
                    src = src.TrimEnd(trimchars);
                    break;

                case STRING_TRIM:
                    src = src.Trim(trimchars);
                    break;
            }

            return src;
        }

        public int llStringLength(string src)
        {
            return src.Length;
        }

        public int llSubStringIndex(string source, string pattern)
        {
            return source.IndexOf(pattern);
        }

        public string llGetSubstring(string src, int start, int end)
        {
            if(start < 0)
            {
                start = src.Length - start;
            }
            if (end < 0)
            {
                end = src.Length - end;
            }

            if(start < 0)
            {
                start = 0;
            }
            else if(start > src.Length)
            {
                start = src.Length;
            }

            if (end < 0)
            {
                end = 0;
            }
            else if(end > src.Length)
            {
                end = src.Length;
            }

            if(start <= end)
            {
                return src.Substring(start, end - start + 1);
            }
            else
            {
                string a = src.Substring(start);
                string b = src.Substring(0, end + 1);
                return b + a;
            }
        }
    }
}
