using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;

namespace ArribaSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public AString llDeleteSubString(AString src, Integer start, Integer end)
        {
            if (start < 0)
            {
                start = src.Length - start;
            }
            if (end < 0)
            {
                end = src.Length - end;
            }

            if (start < 0 || start >= src.Length || end < 0 || end >= src.Length)
            {
                return src;
            }

            AString res = new AString();
            if (start > end)
            {
                res += src.Substring(0, end + 1);
                res += src.Substring(start);
            }
            else
            {
                res += src.Substring(0, start + 1);
                res += src.Substring(end);
            }
            return res;
        }
    }
}
