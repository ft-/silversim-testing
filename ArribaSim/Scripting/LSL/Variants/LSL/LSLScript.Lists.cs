/*

ArribaSim is distributed under the terms of the
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
