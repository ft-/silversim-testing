﻿/*

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

namespace SilverSim.Scripting.LSL.API.Base
{
    public partial class Base_API
    {
        [APILevel(APIFlags.LSL)]
        public string llDeleteSubString(ScriptInstance Instance, string src, int start, int end)
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

        [APILevel(APIFlags.LSL)]
        public string llToLower(ScriptInstance Instance, string s)
        {
            return s.ToLower();
        }

        [APILevel(APIFlags.LSL)]
        public string llToUpper(ScriptInstance Instance, string s)
        {
            return s.ToUpper();
        }

        [APILevel(APIFlags.LSL)]
        public string llUnescapeURL(ScriptInstance Instance, string url)
        {
            return Uri.UnescapeDataString(url);
        }

        [APILevel(APIFlags.LSL)]
        public string llEscapeURL(ScriptInstance Instance, string url)
        {
            return Uri.EscapeDataString(url);
        }

        [APILevel(APIFlags.LSL)]
        public const int STRING_TRIM_HEAD = 0x1;
        [APILevel(APIFlags.LSL)]
        public const int STRING_TRIM_TAIL = 0x2;
        [APILevel(APIFlags.LSL)]
        public const int STRING_TRIM = 0x3;

        private static readonly char[] trimchars = new char[] { ' ', '\t', '\r', '\n' };

        [APILevel(APIFlags.LSL)]
        public string llStringTrim(ScriptInstance Instance, string src, int type)
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

        [APILevel(APIFlags.LSL)]
        public int llStringLength(ScriptInstance Instance, string src)
        {
            return src.Length;
        }

        [APILevel(APIFlags.LSL)]
        public int llSubStringIndex(ScriptInstance Instance, string source, string pattern)
        {
            return source.IndexOf(pattern);
        }

        [APILevel(APIFlags.LSL)]
        public string llGetSubstring(ScriptInstance Instance, string src, int start, int end)
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
