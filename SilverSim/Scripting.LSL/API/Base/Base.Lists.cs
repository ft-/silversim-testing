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
        [LSLTooltip("Returns a list that is a copy of src but with the slice from start to end removed.")]
        public AnArray llDeleteSubList(ScriptInstance Instance,
            [LSLTooltip("source")]
            AnArray src,
            [LSLTooltip("start index")]
            int start,
            [LSLTooltip("end index")]
            int end)
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
        [LSLTooltip("Returns a list of all the entries in the strided list whose index is a multiple of stride in the range start to end.")]
        public AnArray llList2ListStrided(ScriptInstance instance,
            AnArray src,
            [LSLTooltip("start index")]
            int start,
            [LSLTooltip("end index")]
            int end,
            [LSLTooltip("number of entries per stride, if less than 1 it is assumed to be 1")]
            int stride)
        {

            AnArray result = new AnArray();
            int[] si = new int[2];
            int[] ei = new int[2];
            bool twopass = false;

            /*
             * First step is always to deal with negative indices
             */

            if (start < 0)
            {
                start = src.Count + start;
            }
            if (end < 0)
            {
                end = src.Count + end;
            }

            /*
             * Out of bounds indices are OK, just trim them accordingly
             */

            if (start > src.Count)
            {
                start = src.Count;
            }

            if (end > src.Count)
            {
                end = src.Count;
            }

            if (stride == 0)
            {
                stride = 1;
            }

            /*
             * There may be one or two ranges to be considered
             */

            if (start != end)
            {

                if (start <= end)
                {
                    si[0] = start;
                    ei[0] = end;
                }
                else
                {
                    si[1] = start;
                    ei[1] = src.Count;
                    si[0] = 0;
                    ei[0] = end;
                    twopass = true;
                }

                /*
                 * The scan always starts from the beginning of the
                 * source list, but members are only selected if they
                 * fall within the specified sub-range. The specified
                 * range values are inclusive.
                 * A negative stride reverses the direction of the
                 * scan producing an inverted list as a result.
                 */

                if (stride > 0)
                {
                    for (int i = 0; i < src.Count; i += stride)
                    {
                        if (i <= ei[0] && i >= si[0])
                        {
                            result.Add(src[i]);
                        }
                        if (twopass && i >= si[1] && i <= ei[1])
                        {
                            result.Add(src[i]);
                        }
                    }
                }
                else if (stride < 0)
                {
                    for (int i = src.Count - 1; i >= 0; i += stride)
                    {
                        if (i <= ei[0] && i >= si[0])
                        {
                            result.Add(src[i]);
                        }
                        if (twopass && i >= si[1] && i <= ei[1])
                        {
                            result.Add(src[i]);
                        }
                    }
                }
            }
            else
            {
                if (start % stride == 0)
                {
                    result.Add(src[start]);
                }
            }

            return result;
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
        [LSLTooltip("Returns the integer index of the first instance of test in src.")]
        public int llListFindList(ScriptInstance Instance,
            [LSLTooltip("what to search in (haystack)")]
            AnArray src,
            [LSLTooltip("what to search for (needle)")]
            AnArray test)
        {
            int index = -1;
            int length = src.Count - test.Count + 1;

            /* If either list is empty, do not match */
            if (src.Count != 0 && test.Count != 0)
            {
                for (int i = 0; i < length; i++)
                {
                    if (src[i].Equals(test[0]) || test[0].Equals(src[i]))
                    {
                        int j;
                        for (j = 1; j < test.Count; j++)
                            if (!(src[i + j].Equals(test[j]) || test[j].Equals(src[i + j])))
                                break;

                        if (j == test.Count)
                        {
                            index = i;
                            break;
                        }
                    }
                }
            }

            return index;
        }

        [APILevel(APIFlags.LSL)]
        [LSLTooltip("Returns an integer that is at index in src")]
        public int llList2Integer(ScriptInstance Instance,
            [LSLTooltip("List containing the element of interest")]
            AnArray src,
            [LSLTooltip("Index of the element of interest.")]
            int index)
        {
            if (index < 0)
            {
                index = src.Count - index;
            }

            if (index < 0 || index >= src.Count)
            {
                return 0;
            }

            if(src[index] is Real)
            {
                return LSLCompiler.ConvToInt((Real)src[index]);
            }
            else if (src[index] is AString)
            {
                return LSLCompiler.ConvToInt(src[index].ToString());
            }
            else
            {
                return src[index].AsInteger;
            }
        }

        [APILevel(APIFlags.LSL)]
        [LSLTooltip("Returns a key that is at index in src")]
        public LSLKey llList2Key(ScriptInstance Instance,
            [LSLTooltip("List containing the element of interest")]
            AnArray src,
            [LSLTooltip("Index of the element of interest.")]
            int index)
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
        [LSLTooltip("Returns a rotation that is at index in src")]
        public Quaternion llList2Rot(ScriptInstance Instance,
            [LSLTooltip("List containing the element of interest")]
            AnArray src,
            [LSLTooltip("Index of the element of interest.")]
            int index)
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
        [LSLTooltip("Returns a string that is at index in src")]
        public string llList2String(ScriptInstance Instance,
            [LSLTooltip("List containing the element of interest")]
            AnArray src,
            [LSLTooltip("Index of the element of interest.")]
            int index)
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
        [LSLTooltip("Returns a vector that is at index in src")]
        public Vector3 llList2Vector(ScriptInstance Instance,
            [LSLTooltip("List containing the element of interest")]
            AnArray src,
            [LSLTooltip("Index of the element of interest.")]
            int index)
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
        [LSLTooltip("Returns a string that is the list src converted to a string with separator between the entries.")]
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
        [LSLTooltip("Returns a string of comma separated values taken in order from src.")]
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
        [LSLTooltip("Returns the type (an integer) of the entry at index in src.")]
        public int llGetListEntryType(ScriptInstance Instance,
            [LSLTooltip("List containing the element of interest")]
            AnArray src,
            [LSLTooltip("Index of the element of interest")]
            int index)
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
        [LSLTooltip("Returns an integer that is the number of elements in the list src")]
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
        [LSLTooltip("Returns a list that is src broken into a list of strings, discarding separators, keeping spacers, discards any null (empty string) values generated.")]
        public AnArray llParseString2List(ScriptInstance Instance,
            [LSLTooltip("source string")]
            string src,
            [LSLTooltip("separators to be discarded")]
            AnArray separators,
            [LSLTooltip("spacers to be kept")]
            AnArray spacers)
        {
            return ParseString2List(Instance, src, separators, spacers, false);
        }

        [APILevel(APIFlags.LSL)]
        [LSLTooltip("Returns a list that is src broken into a list, discarding separators, keeping spacers, keeping any null values generated.")]
        public AnArray llParseStringKeepNulls(ScriptInstance Instance,
            [LSLTooltip("source string")]
            string src,
            [LSLTooltip("separators to be discarded")]
            AnArray separators,
            [LSLTooltip("spacers to be kept")]
            AnArray spacers)
        {
            return ParseString2List(Instance, src, separators, spacers, true);
        }

        [APILevel(APIFlags.LSL)]
        [LSLTooltip("This function takes a string of values separated by commas, and turns it into a list.")]
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
