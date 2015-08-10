// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Asset;

namespace SilverSim.Scripting.LSL.API.Primitive
{
    public partial class Primitive_API
    {
        private void enqueue_to_scripts(ObjectPart part, LinkMessageEvent ev)
        {
            foreach(ObjectPartInventoryItem item in part.Inventory.Values)
            {
                if(item.AssetType == AssetType.LSLText || item.AssetType == AssetType.LSLBytecode)
                {
                    ScriptInstance si = item.ScriptInstance;

                    if(si != null)
                    {
                        si.PostEvent(ev);
                    }
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llMessageLinked(ScriptInstance Instance, int link, int num, string str, LSLKey id)
        {
            lock (Instance)
            {
                LinkMessageEvent ev = new LinkMessageEvent();
                ev.SenderNumber = Instance.Part.LinkNumber;
                ev.TargetNumber = link;
                ev.Number = num;
                ev.Data = str;
                ev.Id = id.ToString();

                foreach (ObjectPart part in GetLinkTargets(Instance, link))
                {
                    enqueue_to_scripts(part, ev);
                }
            }
        }
    }
}
