using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Scene.Types.Script.Events;
using ArribaSim.Types;

namespace ArribaSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public AnArray llDeleteSubList(AnArray src, Integer start, Integer end)
        {
            if(start < 0)
            {
                start = new Integer(src.Count) - start;
            }
            if(end < 0)
            {
                end = new Integer(src.Count) - end;
            }

            if(start < 0 || start >= src.Count || end < 0 || end >= src.Count)
            {
                return new AnArray(src);
            }

            AnArray res = new AnArray();
            if(start > end)
            {
                for(int i = 0; i <= end; ++i)
                {
                    res.Add(src[i]);
                }
                for(int i = start; i < src.Count; ++i)
                {
                    res.Add(src[i]);
                }
            }
            else
            {
                for(int i = 0; i < start; ++i)
                {
                    res.Add(src[i]);
                }
                for(int i = end + 1; i < src.Count; ++i)
                {
                    res.Add(src[i]);
                }
            }
            return res;
        }
    }
}
