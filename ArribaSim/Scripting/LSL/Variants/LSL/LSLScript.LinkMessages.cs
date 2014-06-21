using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;
using ArribaSim.Types.Asset;
using ArribaSim.Scene.Types.Object;
using ArribaSim.Scene.Types.Script;
using ArribaSim.Scene.Types.Script.Events;

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
