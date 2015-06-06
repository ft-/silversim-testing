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

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;

namespace SilverSim.Scripting.LSL.API.Animation
{
    [ScriptApiName("Animation")]
    [LSLImplementation]
    public partial class Animation_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        public Animation_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL)]
        [LSLTooltip("permission to start or stop animations on agent")]
        public const int PERMISSION_TRIGGER_ANIMATION = 0x10;

        [APILevel(APIFlags.LSL)]
        public void llStartAnimation(ScriptInstance instance, string anim)
        {
            lock (instance)
            {
                IAgent agent;
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                if ((grantinfo.PermsMask & ScriptPermissions.TriggerAnimation) == 0 ||
                    grantinfo.PermsGranter == UUI.Unknown)
                {
                    return;
                }
                try
                {
                    agent = instance.Part.ObjectGroup.Scene.Agents[grantinfo.PermsGranter.ID];
                }
                catch
                {
                    instance.ShoutError("llStartAnimation: permission granter not in region");
                    return;
                }

                agent.PlayAnimation(anim, instance.Part.ID);
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llStopAnimation(ScriptInstance instance, string anim)
        {
            lock (instance)
            {
                IAgent agent;
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                if ((grantinfo.PermsMask & ScriptPermissions.TriggerAnimation) == 0 ||
                    grantinfo.PermsGranter == UUI.Unknown)
                {
                    return;
                }
                try
                {
                    agent = instance.Part.ObjectGroup.Scene.Agents[grantinfo.PermsGranter.ID];
                }
                catch
                {
                    instance.ShoutError("llStopAnimation: permission granter not in region");
                    return;
                }

                agent.StopAnimation(anim, instance.Part.ID);
            }
        }

        [APILevel(APIFlags.LSL)]
        public string llGetAnimation(ScriptInstance Instance, LSLKey agent)
        {
#warning Implement llGetAnimation
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public string llGetAnimationList(ScriptInstance Instance, LSLKey agent)
        {
#warning Implement llGetAnimation
            throw new NotImplementedException();
        }
    }
}
