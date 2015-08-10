// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;
using SilverSim.Scene.Types.Object;

namespace SilverSim.Scripting.LSL.API.AnimationOverride
{
    [ScriptApiName("AnimationOverride")]
    [LSLImplementation]
    public partial class AnimationOverride_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        public AnimationOverride_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL)]
        public void llSetAnimationOverride(ScriptInstance instance, string anim_state, string anim)
        {
            lock (instance)
            {
                IAgent agent;
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;

                if ((grantinfo.PermsMask & ScriptPermissions.OverrideAnimations) == 0 ||
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
                    instance.ShoutError("llSetAnimationOverride: permission granter not in region");
                    return;
                }

                try
                {
                    agent.SetAnimationOverride(anim_state, anim);
                }
                catch (Exception e)
                {
                    instance.ShoutError(e.Message);
                }
            }
        }

        [APILevel(APIFlags.LSL)]
        public string llGetAnimationOverride(ScriptInstance instance, string anim_state)
        {
            lock (instance)
            {
                IAgent agent;
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                if (((grantinfo.PermsMask & ScriptPermissions.OverrideAnimations) == 0 &&
                    (grantinfo.PermsMask & ScriptPermissions.TriggerAnimation) == 0) ||
                    grantinfo.PermsGranter == UUI.Unknown)
                {
                    return string.Empty;
                }
                try
                {
                    agent = instance.Part.ObjectGroup.Scene.Agents[grantinfo.PermsGranter.ID];
                }
                catch
                {
                    instance.ShoutError("llSetAnimationOverride: permission granter not in region");
                    return string.Empty;
                }

                agent.ResetAnimationOverride(anim_state);
                return anim_state;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llResetAnimationOverride(ScriptInstance instance, string anim_state)
        {
            lock (instance)
            {
                IAgent agent;
                ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
                if ((grantinfo.PermsMask & ScriptPermissions.OverrideAnimations) == 0 ||
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
                    instance.ShoutError("llSetAnimationOverride: permission granter not in region");
                    return;
                }

                agent.ResetAnimationOverride(anim_state);
            }
        }
    }
}
