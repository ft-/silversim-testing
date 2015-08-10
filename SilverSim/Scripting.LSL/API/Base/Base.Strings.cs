// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
