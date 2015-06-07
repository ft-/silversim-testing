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

        public UUID getAnimationAssetID(ScriptInstance Instance, string item)
        {
            UUID assetID;
            if (!UUID.TryParse(item, out assetID))
            {
#warning Implement viewer built-in animations
                /* must be an inventory item */
                lock (Instance)
                {
                    ObjectPartInventoryItem i = Instance.Part.Inventory[item];
                    if (i.InventoryType != Types.Inventory.InventoryType.Animation)
                    {
                        throw new InvalidOperationException(string.Format("Inventory item {0} is not an animation", item));
                    }
                    assetID = i.AssetID;
                }
            }
            return assetID;
        }

        [APILevel(APIFlags.LSL)]
        public void llStartAnimation(
            ScriptInstance instance,
            [LSLTooltip("animation to be played")]
            string anim)
        {
            lock (instance)
            {
                UUID animID = getAnimationAssetID(instance, anim);
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

                agent.PlayAnimation(animID, instance.Part.ID);
            }
        }

        [APILevel(APIFlags.OSSL)]
        [LSLTooltip("causes an animation to be played on the specified avatar.")]
        public void osAvatarPlayAnimation(
            ScriptInstance instance, 
            [LSLTooltip("UUID of the agent")]
            LSLKey avatar, 
            [LSLTooltip("animation to be played")]
            string animation)
        {
            lock (instance)
            {
                instance.CheckThreatLevel("osAvatarPlayAnimation", ScriptInstance.ThreatLevelType.VeryHigh);
                UUID animID = getAnimationAssetID(instance, animation);
                IAgent agent;
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                try
                {
                    agent = instance.Part.ObjectGroup.Scene.Agents[avatar];
                }
                catch
                {
                    instance.ShoutError("osAvatarPlayAnimation: agent not in region");
                    return;
                }

                agent.PlayAnimation(animID, instance.Part.ID);
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llStopAnimation(
            ScriptInstance instance, 
            [LSLTooltip("animation to be stopped")]
            string anim)
        {
            lock (instance)
            {
                UUID animID = getAnimationAssetID(instance, anim);
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

                agent.StopAnimation(animID, instance.Part.ID);
            }
        }

        [APILevel(APIFlags.OSSL)]
        [LSLTooltip("stops the specified animation if it is playing on the avatar given.")]
        public void osAvatarStopAnimation(
            ScriptInstance instance,
            [LSLTooltip("UUID of the agent")]
            LSLKey avatar,
            [LSLTooltip("animation to be stopped")]
            string animation)
        {
            lock (instance)
            {
                instance.CheckThreatLevel("osAvatarStopAnimation", ScriptInstance.ThreatLevelType.VeryHigh);
                UUID animID = getAnimationAssetID(instance, animation);
                IAgent agent;
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                try
                {
                    agent = instance.Part.ObjectGroup.Scene.Agents[avatar];
                }
                catch
                {
                    instance.ShoutError("osAvatarStopAnimation: agent not in region");
                    return;
                }

                agent.PlayAnimation(animID, instance.Part.ID);
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
