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

using SilverSim.Types;
using SilverSim.Scene.Types.Script;

namespace SilverSim.Scripting.LSL.API.Base
{
    public partial class Base_API
    {
        [APILevel(APIFlags.LSL)]
        public AnArray llDeleteSubList(ScriptInstance Instance, AnArray src, int start, int end)
        {
            if (start < 0)
            {
                start = src.Count - start;
            }
            if (end < 0)
            {
                end = src.Count - end;
            }

            if (start < 0)
            {
                start = 0;
            }
            else if (start > src.Count)
            {
                start = src.Count;
            }

            if (end < 0)
            {
                end = 0;
            }
            else if (end > src.Count)
            {
                end = src.Count;
            }

            if (start > end)
            {
                AnArray res = new AnArray();
                for (int i = start; i <= end; ++i)
                {
                    res.Add(src[i]);
                }

                return res;
            }
            else
            {
                AnArray res = new AnArray();

                for (int i = 0; i < start + 1; ++i)
                {
                    res.Add(src[i]);
                }

                for (int i = end; i < src.Count; ++i)
                {
                    res.Add(src[i]);
                }

                return res;
            }
        }

        [APILevel(APIFlags.LSL)]
        public AnArray llList2List(ScriptInstance Instance, AnArray src, int start, int end)
        {
            if (start < 0)
            {
                start = src.Count - start;
            }
            if (end < 0)
            {
                end = src.Count - end;
            }

            if (start < 0)
            {
                start = 0;
            }
            else if (start > src.Count)
            {
                start = src.Count;
            }

            if (end < 0)
            {
                end = 0;
            }
            else if (end > src.Count)
            {
                end = src.Count;
            }

            if (start <= end)
            {
                AnArray res = new AnArray();
                for (int i = start; i <= end; ++i )
                {
                    res.Add(src[i]);
                }

                return res;
            }
            else
            {
                AnArray res = new AnArray();

                for (int i = 0; i < end + 1; ++i)
                {
                    res.Add(src[i]);
                }

                for (int i = start; i < src.Count; ++i)
                {
                    res.Add(src[i]);
                }

                return res;
            }
        }

        [APILevel(APIFlags.LSL)]
        public double llList2Float(ScriptInstance Instance, AnArray src, int index)
        {
            if(index < 0)
            {
                index = src.Count - index;
            }

            if(index < 0 ||index >=src.Count)
            {
                return 0;
            }

            return src[index].AsReal;
        }

        [APILevel(APIFlags.LSL)]
        public int llList2Integer(ScriptInstance Instance, AnArray src, int index)
        {
            if (index < 0)
            {
                index = src.Count - index;
            }

            if (index < 0 || index >= src.Count)
            {
                return 0;
            }

            return src[index].AsInteger;
        }

        [APILevel(APIFlags.LSL)]
        public LSLKey llList2Key(ScriptInstance Instance, AnArray src, int index)
        {
            if (index < 0)
            {
                index = src.Count - index;
            }

            if (index < 0 || index >= src.Count)
            {
                return UUID.Zero;
            }

            return src[index].ToString();
        }

        [APILevel(APIFlags.LSL)]
        public Quaternion llList2Rot(ScriptInstance Instance, AnArray src, int index)
        {
            if (index < 0)
            {
                index = src.Count - index;
            }

            if (index < 0 || index >= src.Count)
            {
                return Quaternion.Identity;
            }

            return src[index].AsQuaternion;
        }

        [APILevel(APIFlags.LSL)]
        public string llList2String(ScriptInstance Instance, AnArray src, int index)
        {
            if (index < 0)
            {
                index = src.Count - index;
            }

            if (index < 0 || index >= src.Count)
            {
                return "";
            }

            return src[index].AsString.ToString();
        }

        [APILevel(APIFlags.LSL)]
        public Vector3 llList2Vector(ScriptInstance Instance, AnArray src, int index)
        {
            if (index < 0)
            {
                index = src.Count - index;
            }

            if (index < 0 || index >= src.Count)
            {
                return Vector3.Zero;
            }

            return src[index].AsVector3;
        }

        [APILevel(APIFlags.LSL)]
        public string llDumpList2String(ScriptInstance Instance, AnArray src, string separator)
        {
            string s = string.Empty;

            foreach(IValue val in src)
            {
                if(!string.IsNullOrEmpty(s))
                {
                    s += separator;
                }
                s += val.ToString();
            }
            return s;
        }

        [APILevel(APIFlags.LSL)]
        public string llList2CSV(ScriptInstance Instance, AnArray src)
        {
            return llDumpList2String(Instance, src, ", ");
        }

        [APILevel(APIFlags.LSL)]
        public const int TYPE_INTEGER = 1;
        [APILevel(APIFlags.LSL)]
        public const int TYPE_FLOAT = 2;
        [APILevel(APIFlags.LSL)]
        public const int TYPE_STRING = 3;
        [APILevel(APIFlags.LSL)]
        public const int TYPE_KEY = 4;
        [APILevel(APIFlags.LSL)]
        public const int TYPE_VECTOR = 5;
        [APILevel(APIFlags.LSL)]
        public const int TYPE_ROTATION = 6;
        [APILevel(APIFlags.LSL)]
        public const int TYPE_INVALID = 0;

        [APILevel(APIFlags.LSL)]
        public int llGetListEntryType(ScriptInstance Instance, AnArray src, int index)
        {
            if (index < 0)
            {
                index = src.Count - index;
            }

            if (index < 0 || index >= src.Count)
            {
                return TYPE_INVALID;
            }

            return (int)src[index].LSL_Type;
        }

        [APILevel(APIFlags.LSL)]
        public int llGetListLength(ScriptInstance Instance, AnArray src)
        {
            return src.Count;
        }

        private AnArray ParseString2List(ScriptInstance Instance, string src, AnArray separators, AnArray spacers, bool keepNulls)
        {
            AnArray res = new AnArray();
            string value = null;
            
            while(src.Length != 0)
            {
                IValue foundSpacer = null;
                foreach(IValue spacer in spacers)
                {
                    if(spacer.LSL_Type != LSLValueType.String)
                    {
                        continue;
                    }
                    if(src.StartsWith(spacer.ToString()))
                    {
                        foundSpacer = spacer;
                        break;
                    }
                }

                if (foundSpacer != null)
                {
                    src = src.Substring(foundSpacer.ToString().Length);
                    continue;
                }

                IValue foundSeparator = null;
                foreach(IValue separator in separators)
                {
                    if(separator.LSL_Type != LSLValueType.String)
                    {
                        continue;
                    }

                    if(src.StartsWith(separator.ToString()))
                    {
                        foundSeparator = separator;
                        break;
                    }
                }

                if(foundSeparator != null)
                {
                    if(value == null && keepNulls)
                    {
                        res.Add(value);
                    }
                    else if(value != null)
                    {
                        res.Add(value);
                    }
                    value = null;
                    src = src.Substring(foundSeparator.ToString().Length);
                    if(src.Length == 0)
                    {
                        /* special case we consumed all entries but a separator at end */
                        if(keepNulls)
                        {
                            res.Add(string.Empty);
                        }
                    }
                }

                int minIndex = src.Length;

                foreach(IValue spacer in spacers)
                {
                    if (spacer.LSL_Type != LSLValueType.String)
                    {
                        continue;
                    }
                    int resIndex = src.IndexOf(spacer.ToString());
                    if(resIndex < 0)
                    {
                        continue;
                    }
                    else if(resIndex < minIndex)
                    {
                        minIndex = resIndex;
                    }
                }
                foreach(IValue separator in separators)
                {
                    if(spacers.LSL_Type != LSLValueType.String)
                    {
                        continue;
                    }
                    int resIndex = src.IndexOf(separator.ToString());
                    if (resIndex < 0)
                    {
                        continue;
                    }
                    else if (resIndex < minIndex)
                    {
                        minIndex = resIndex;
                    }
                }

                value = src.Substring(0, minIndex);
                src = src.Substring(minIndex);
            }

            if (value != null)
            {
                res.Add(value);
            }

            return res;
        }

        [APILevel(APIFlags.LSL)]
        public AnArray llParseString2List(ScriptInstance Instance, string src, AnArray separators, AnArray spacers)
        {
            return ParseString2List(Instance, src, separators, spacers, false);
        }

        [APILevel(APIFlags.LSL)]
        public AnArray llParseStringKeepNulls(ScriptInstance Instance, string src, AnArray separators, AnArray spacers)
        {
            return ParseString2List(Instance, src, separators, spacers, true);
        }

        [APILevel(APIFlags.LSL)]
        public AnArray llCSV2List(ScriptInstance Instance, string src)
        {
            bool wsconsume = true;
            bool inbracket = false;
            string value = string.Empty;
            AnArray ret = new AnArray();

            foreach(char c in src)
            {
                switch(c)
                {
                    case ' ': case '\t':
                        if(wsconsume)
                        {
                            break;
                        }
                        value += c;
                        break;

                    case '<':
                        inbracket = true;
                        value += c;
                        break;

                    case '>':
                        inbracket = false;
                        value += c;
                        break;

                    case ',':
                        if(inbracket)
                        {
                            value += c;
                            break;
                        }

                        ret.Add(value);
                        wsconsume = true;
                        break;

                    default:
                        wsconsume = false;
                        value += c;
                        break;
                }
            }

            ret.Add(string.Empty);
            return ret;
        }
    }
}
