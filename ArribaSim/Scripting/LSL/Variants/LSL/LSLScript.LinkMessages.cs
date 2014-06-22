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

using ArribaSim.Scene.Types.Object;
using ArribaSim.Scene.Types.Script;
using ArribaSim.Scene.Types.Script.Events;
using ArribaSim.Types;
using ArribaSim.Types.Asset;

namespace ArribaSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        private void enqueue_to_scripts(ObjectPart part, LinkMessageEvent ev)
        {
            foreach(ObjectPartInventoryItem item in part.Inventory.Values)
            {
                if(item.AssetType == AssetType.LSLText || item.AssetType == AssetType.LSLBytecode)
                {
                    IScriptInstance si = item.ScriptInstance;

                    if(si != null)
                    {
                        si.PostEvent(ev);
                    }
                }
            }
        }

        public void llMessageLinked(Integer link, Integer num, AString str, UUID id)
        {
            LinkMessageEvent ev = new LinkMessageEvent();
            ev.SenderNumber = Part.LinkNumber;
            ev.TargetNumber = link;
            ev.Number = num;
            ev.Data = str.ToString();
            ev.Id = id;

            if(link == LINK_THIS)
            { 
                enqueue_to_scripts(Part, ev);
            }
            else if(link == LINK_ROOT)
            {
                enqueue_to_scripts(Part.Group.RootPart, ev);
            }
            else if(link == LINK_SET)
            {
                foreach(ObjectPart part in Part.Group.Values)
                {
                    enqueue_to_scripts(part, ev);
                }
            }
            else if(link == LINK_ALL_OTHERS)
            {
                foreach(ObjectPart part in Part.Group.Values)
                {
                    if(part != Part)
                    {
                        enqueue_to_scripts(part, ev);
                    }
                }
            }
            else if(link == LINK_ROOT)
            {
                foreach(ObjectPart part in Part.Group.Values)
                {
                    if(part != Part.Group.RootPart)
                    {
                        enqueue_to_scripts(part, ev);
                    }
                }
            }
            else
            {
                enqueue_to_scripts(Part.Group[link.AsInt], ev);
            }
        }
    }
}
